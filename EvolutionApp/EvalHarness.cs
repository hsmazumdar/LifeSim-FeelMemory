using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Evolution
{
    /// <summary>
    /// Headless LifeSim harness (LifeSim.exe --eval).
    /// TeacherSuccess in-sample / LOO / transfer / ablation + dest-hidden arms.
    /// Fix2 honesty. Temp mem only.
    /// </summary>
    public static class EvalHarness
    {
        public const int TeacherAttempts = 250;
        public const int EvalRuns = 30;
        public const int PrefSeedsN = 5;
        public const int LooSeedsN = 5;
        /// <summary>Phase B scaffold: deterministic LOO seed count (generate via BuildLooSeedList).</summary>
        public const int LooSeedsN25 = 25;
        public const int TransferSeedsN = 3;
        public const int AblationSeedsN = 5;
        public const int AblationRuns = 40;
        public const int MaxSteps = 2000;

        static readonly int[] PrefSeedList = { 11, 22, 33, 44, 55 };
        static readonly int[] LooSeedList = { 101, 202, 303, 404, 505 };
        static readonly int[] TransferSeedList = { 7, 17, 27 };
        static Dictionary<string, double> OptEnergyByWorld;

        static readonly int[] AblationSeedList = { 3, 13, 23, 43, 53 };

        /// <summary>Active LOO seed list for --eval-baseline (Phase A n=5 or Phase B via --seeds 25).</summary>
        static int[] BaselineSeedList = LooSeedList;
        static int BaselineSeedsN = LooSeedsN;
        static int BaselineSeedFailures = 0;
        static string BaselinePhaseLabel = "Phase A";
        static int BaselineProceduralCount = 0;
        static int BaselineWorldCount = 4;
        static string BaselineWorldNames = "Terrain-A..D";
        static int BaselineBaseSeed = 9001;

        public static int Run(string[] args)
        {
            if (args != null)
            {
                foreach (var a in args)
                {
                    if (string.Equals(a, "--smoke", StringComparison.OrdinalIgnoreCase))
                    {
                        string smokeOut = ResolveOutDir(args);
                        Directory.CreateDirectory(smokeOut);
                        return RunSmoke(smokeOut);
                    }
                    if (string.Equals(a, "--teacher-diag", StringComparison.OrdinalIgnoreCase))
                    {
                        string diagOut = ResolveOutDir(args);
                        Directory.CreateDirectory(diagOut);
                        return RunTeacherDiag(diagOut);
                    }
                }
            }

            // Baseline Phase A: three-policy baseline LOO GK (SuccessWeighted vs TabularNgramBC vs EmptyLTM)
            bool evalBaseline = false;
            foreach (var a in (args ?? new string[0]))
            {
                if (string.Equals(a, "--eval-baseline", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "--baseline", StringComparison.OrdinalIgnoreCase))
                    evalBaseline = true;
            }
            if (evalBaseline)
            {
                string bOut = ResolveOutDir(args);
                // Prefer explicit phaseA folder when --out not given
                bool hasOut = false;
                if (args != null)
                    for (int i = 0; i < args.Length - 1; i++)
                        if (args[i] == "--out" || args[i] == "-o") hasOut = true;
                if (!hasOut)
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    bOut = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "EvalLogs", "baseline_rerun"));
                }

                // Phase B: --seeds 25 / --loo-seeds 25 uses BuildLooSeedList(LooSeedsN25)
                int seedN = LooSeedsN;
                if (args != null)
                {
                    for (int i = 0; i < args.Length - 1; i++)
                    {
                        if (string.Equals(args[i], "--seeds", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(args[i], "--loo-seeds", StringComparison.OrdinalIgnoreCase))
                        {
                            int parsed;
                            if (int.TryParse(args[i + 1], out parsed) && parsed > 0)
                                seedN = parsed;
                        }
                    }
                }
                BaselineSeedsN = seedN;
                BaselineSeedList = (seedN == LooSeedsN) ? LooSeedList : BuildLooSeedList(seedN);
                BaselineSeedFailures = 0;

                // Phase C: --worlds N (total LOO pool) / --procedural N (A-D + N) / --base-seed
                int proceduralN = 0;
                int totalWorldsWanted = 0;
                int baseSeed = 9001;
                if (args != null)
                {
                    for (int i = 0; i < args.Length - 1; i++)
                    {
                        if (string.Equals(args[i], "--procedural", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(args[i], "--proc", StringComparison.OrdinalIgnoreCase))
                        {
                            int parsed;
                            if (int.TryParse(args[i + 1], out parsed) && parsed >= 0)
                                proceduralN = parsed;
                        }
                        if (string.Equals(args[i], "--worlds", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(args[i], "--world-count", StringComparison.OrdinalIgnoreCase))
                        {
                            int parsed;
                            if (int.TryParse(args[i + 1], out parsed) && parsed > 0)
                                totalWorldsWanted = parsed;
                        }
                        if (string.Equals(args[i], "--base-seed", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(args[i], "--proc-seed", StringComparison.OrdinalIgnoreCase))
                        {
                            int parsed;
                            if (int.TryParse(args[i + 1], out parsed))
                                baseSeed = parsed;
                        }
                    }
                }
                BaselineBaseSeed = baseSeed;
                if (proceduralN <= 0 && totalWorldsWanted > WorldLibrary.Primary.Count)
                    proceduralN = totalWorldsWanted - WorldLibrary.Primary.Count;
                // If --procedural alone: use that. If --worlds alone: pad Primary to N.
                // Generate at least max(proceduralN, 20) procedural previews when Phase C.
                int previewProc = proceduralN;
                if (proceduralN > 0 && previewProc < 20) previewProc = 20;

                List<World> worldsB;
                if (proceduralN > 0)
                {
                    worldsB = WorldLibrary.PrimaryPlusProcedural(proceduralN, baseSeed).ToList();
                    // Persist >=20 procedural previews (A-D kept; Terrain-01..)
                    try
                    {
                        string forkRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                        string procDir = Path.Combine(forkRoot, "worlds", "procedural_20");
                        Directory.CreateDirectory(procDir);
                        var previewList = WorldLibrary.GenerateProcedural(previewProc, baseSeed);
                        for (int pi = 0; pi < previewList.Count; pi++)
                        {
                            var pw = previewList[pi];
                            CanvasWorldGenerator.SaveColorPreview(pw, Path.Combine(procDir, pw.Name + ".png"));
                            CanvasWorldGenerator.SaveRoughnessCsv(pw, Path.Combine(procDir, pw.Name + ".csv"));
                        }
                        File.WriteAllText(Path.Combine(procDir, "manifest.txt"),
                            "baseSeed=" + baseSeed + " count=" + previewList.Count + " names=" +
                            string.Join(",", previewList.Select(w => w.Name)) + Environment.NewLine, Encoding.UTF8);
                        Console.WriteLine("  Procedural previews saved: " + procDir + " (n=" + previewList.Count + ")");
                    }
                    catch (Exception exPrev)
                    {
                        Console.WriteLine("  WARN procedural preview save: " + exPrev.Message);
                    }
                }
                else
                {
                    worldsB = WorldLibrary.Primary.ToList();
                }

                BaselineProceduralCount = proceduralN;
                BaselineWorldCount = worldsB.Count;
                BaselineWorldNames = string.Join(",", worldsB.Select(w => w.Name));
                if (proceduralN > 0)
                    BaselinePhaseLabel = "Phase C";
                else if (seedN >= LooSeedsN25)
                    BaselinePhaseLabel = "Phase B";
                else
                    BaselinePhaseLabel = "Phase A";

                Directory.CreateDirectory(bOut);
                string bMem = Path.Combine(bOut, "temp_mem");
                Directory.CreateDirectory(bMem);
                Console.WriteLine("EvalHarness --eval-baseline " + BaselinePhaseLabel + " n=" + BaselineSeedsN +
                    " worlds=" + BaselineWorldCount + " procedural=" + BaselineProceduralCount + ". out=" + bOut);
                Console.WriteLine("  Seeds: " + string.Join(",", BaselineSeedList));
                Console.WriteLine("  Worlds: " + BaselineWorldNames);
                var swB = Stopwatch.StartNew();
                WriteOptimalReference(worldsB, Path.Combine(bOut, "optimal_reference.csv"));
                OptEnergyByWorld = BuildOptMap(worldsB);
                RunBaselineLooArm(worldsB, bMem, bOut, false, "loo_baseline_goalknown");
                WriteBaselineSummaryMd(bOut, swB.Elapsed);
                WriteBaselineVerdictMd(bOut, swB.Elapsed);
                Console.WriteLine("DONE " + BaselinePhaseLabel + " baseline in " + swB.Elapsed.TotalMinutes.ToString("0.0") + " min. Results: " + bOut);
                return 0;
            }

            // ISO: GK LOO only (skip DH / transfer / ablation / teacher arms)
            bool gkLooOnly = false;
            foreach (var a in (args ?? new string[0]))
            {
                if (string.Equals(a, "--eval-gk-loo", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "--gk-loo", StringComparison.OrdinalIgnoreCase))
                    gkLooOnly = true;
            }
            if (gkLooOnly)
            {
                string gkOut = ResolveOutDir(args);
                Directory.CreateDirectory(gkOut);
                string gkMem = Path.Combine(gkOut, "temp_mem");
                Directory.CreateDirectory(gkMem);
                Console.WriteLine("EvalHarness GK-LOO-ONLY. out=" + gkOut);
                var swGk = Stopwatch.StartNew();
                var worldsGk = WorldLibrary.Primary.ToList();
                WriteOptimalReference(worldsGk, Path.Combine(gkOut, "optimal_reference.csv"));
                OptEnergyByWorld = BuildOptMap(worldsGk);
                RunLooArm(worldsGk, gkMem, gkOut, false, "loo_goalknown");
                var sbGk = new StringBuilder();
                sbGk.AppendLine("# GK LOO only");
                sbGk.AppendLine("ElapsedMin=" + swGk.Elapsed.TotalMinutes.ToString("0.00"));
                sbGk.AppendLine();
                string csvGk = Path.Combine(gkOut, "loo_goalknown_summary.csv");
                if (File.Exists(csvGk)) sbGk.AppendLine(File.ReadAllText(csvGk));
                File.WriteAllText(Path.Combine(gkOut, "SUMMARY.md"), sbGk.ToString());
                Console.WriteLine("DONE GK-LOO in " + swGk.Elapsed.TotalMinutes.ToString("0.0") + " min. Results: " + gkOut);
                return 0;
            }
            string outDir = ResolveOutDir(args);
            Directory.CreateDirectory(outDir);
            string memDir = Path.Combine(outDir, "temp_mem");
            Directory.CreateDirectory(memDir);

            Console.WriteLine("EvalHarness starting. out=" + outDir);
            var swAll = Stopwatch.StartNew();
            var worlds = WorldLibrary.Primary.ToList();
            Console.WriteLine("Primary: " + string.Join(", ", worlds.Select(w =>
                w.Name + " S=" + w.StartCell + " D=" + w.GoalCell)));

            WriteOptimalReference(worlds, Path.Combine(outDir, "optimal_reference.csv"));
            OptEnergyByWorld = BuildOptMap(worlds);

            RunTeacherArm(worlds, memDir, outDir, false, "teacher_insample", false);
            RunTeacherArm(worlds, memDir, outDir, true, "teacher_insample_desthidden", false);
            RunLooArm(worlds, memDir, outDir, false, "loo_goalknown");
            RunLooArm(worlds, memDir, outDir, true, "loo_desthidden");
            RunTransferTrainA(worlds, memDir, outDir);
            RunAblation(worlds, outDir);
            WriteSummaryMd(outDir, swAll.Elapsed);
            WriteVerdictMd(outDir, swAll.Elapsed);

            Console.WriteLine("DONE in " + swAll.Elapsed.TotalMinutes.ToString("0.0") + " min. Results: " + outDir);
            return 0;
        }


        static int RunSmoke(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var worlds = WorldLibrary.Primary.ToList();
            World w = null;
            foreach (var x in worlds)
            {
                if (x.Name != null && x.Name.IndexOf("Terrain-A", StringComparison.OrdinalIgnoreCase) >= 0) { w = x; break; }
            }
            if (w == null) w = worlds[0];
            Console.WriteLine("SMOKE world=" + w.Name);

            var log = new StringBuilder();
            var rng = new Random(20260925);
            string memPath = Path.Combine(outDir, "smoke_brain.mem");
            var teach = Evaluator.RunTeacherSuccessHeadless(
                new List<World> { w }, 40, 1, 800, rng, log, memPath, null, false);
            BrainMemory brain = teach.Brain;
            Console.WriteLine("After teach: LtmCount=" + brain.LtmCount + " PromoteCount=" + brain.PromoteCount);
            Console.WriteLine(brain.StatusLine());

            EvalRunTotals withB, noB;
            Evaluator.EvalFrozenPaired(w, brain, 8, 800, 4242, out withB, out noB, false);
            Console.WriteLine("SMOKE with_brain goals=" + withB.GoalsReached + "/" + withB.Runs +
                " energy_score=" + withB.EnergyScore.ToString("0.0"));
            Console.WriteLine("SMOKE no_brain goals=" + noB.GoalsReached + "/" + noB.Runs +
                " energy_score=" + noB.EnergyScore.ToString("0.0"));
            double eGain = 0;
            if (Math.Abs(noB.EnergyScore) > 1e-6)
                eGain = 100.0 * (noB.EnergyScore - withB.EnergyScore) / Math.Abs(noB.EnergyScore);
            // Note: EnergyScore lower-is-better typically; print raw delta too
            Console.WriteLine("SMOKE energy_score_delta(no-with)=" + (noB.EnergyScore - withB.EnergyScore).ToString("0.00") +
                " relative_pct_vs_no=" + eGain.ToString("0.00"));

            var probe = brain.CloneFrozenLtmOnly();
            probe.ResetMatchCounters();
            var robot = new Robot(w.StartCell, new Random(99));
            robot.UseBrain = true;
            robot.LearnEnabled = false;
            robot.ClearBrainOnReset = false;
            robot.HideDestination = false;
            robot.SetBrain(probe);
            int ticks = 0, safety = 16000;
            while (!robot.IsOnGoal && robot.DecisionSteps < 800 && ticks < safety)
            {
                robot.Tick(w);
                ticks++;
            }
            double hitRate = probe.MatchAttempts > 0 ? (double)probe.MatchHits / probe.MatchAttempts : 0.0;
            Console.WriteLine("SMOKE match attempts=" + probe.MatchAttempts +
                " hits=" + probe.MatchHits + " steer=" + probe.MatchSteerApplied +
                " hit_rate=" + hitRate.ToString("0.###") +
                " steps=" + robot.DecisionSteps + " goal=" + robot.IsOnGoal + " stm=" + probe.StmCount);
            
            // V16 Fix A round-trip: SaveToFile must persist feel-atlas fields; Load clears STM.
            brain.SaveToFile(memPath);
            var reloaded = BrainMemory.LoadFromFile(memPath);
            reloaded.ClearStm();
            reloaded.ActiveWorldId = w.Name;
            int feelLines = 0;
            foreach (var ln in File.ReadAllLines(memPath))
                if (ln.StartsWith("FEEL,", StringComparison.Ordinal)) feelLines++;
            Console.WriteLine("SMOKE save/load FEEL lines=" + feelLines +
                " reloaded.LtmCount=" + reloaded.LtmCount +
                " reloaded.StmCount=" + reloaded.StmCount + " (STM expect 0)");
            if (feelLines < 1)
                Console.WriteLine("SMOKE WARN: no FEEL rules persisted — check teacher populate / Fix A");
            else
                Console.WriteLine("SMOKE OK: feel-atlas persistence round-trip");

File.WriteAllText(Path.Combine(outDir, "smoke_report.txt"),
                log.ToString() + "\nLTM=" + brain.LtmCount +
                "\nenergy_delta=" + (noB.EnergyScore - withB.EnergyScore) +
                "\nmatch_attempts=" + probe.MatchAttempts +
                "\nmatch_hits=" + probe.MatchHits +
                "\nmatch_steer=" + probe.MatchSteerApplied + "\n", Encoding.UTF8);
            Console.WriteLine("SMOKE done -> " + outDir);
            return 0;
        }
        /// <summary>
        /// One-off: path-length distribution for Dijkstra vs noisy TryReachGoal on Terrain-A..D.
        /// Prints Chebyshev/Manhattan minima and success Path.Count stats (moves, not cells).
        /// </summary>
        static int RunTeacherDiag(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var worlds = WorldLibrary.Primary.ToList();
            var sb = new StringBuilder();
            sb.AppendLine("world,manhattan,chebyshev,dijkstra_steps,dijkstra_energy,noisy_n,fail,min,mean,max,p50,p90,mean_over_cheby");
            Console.WriteLine("=== TEACHER PATH-LENGTH DIAG ===");
            Console.WriteLine("Path.Count = number of MOVES (departure cells); excludes goal cell.");
            Console.WriteLine("Movement is 8-connected => step-minimum = Chebyshev, not Manhattan.");

            int attempts = 250;
            int maxSteps = 2000;
            var rng = new Random(11);

            foreach (var w in worlds)
            {
                int manh = Math.Abs(w.GoalCell.X - w.StartCell.X) + Math.Abs(w.GoalCell.Y - w.StartCell.Y);
                int cheb = Math.Max(Math.Abs(w.GoalCell.X - w.StartCell.X), Math.Abs(w.GoalCell.Y - w.StartCell.Y));

                var opt = TeacherPrimitive.DijkstraSuccess(w);
                int dSteps = opt.ReachedGoal ? opt.Path.Count : -1;
                double dE = opt.Energy;

                var lens = new List<int>();
                int fails = 0;
                for (int a = 0; a < attempts; a++)
                {
                    double temp = 0.25 + rng.NextDouble() * 0.55;
                    var att = TeacherPrimitive.TryReachGoal(w, rng, maxSteps, temp);
                    if (att.ReachedGoal && att.Path != null && att.Path.Count > 0)
                        lens.Add(att.Path.Count);
                    else
                        fails++;
                }
                lens.Sort();
                double mean = lens.Count > 0 ? lens.Average() : 0;
                int min = lens.Count > 0 ? lens[0] : -1;
                int max = lens.Count > 0 ? lens[lens.Count - 1] : -1;
                int p50 = lens.Count > 0 ? lens[(lens.Count - 1) / 2] : -1;
                int p90 = lens.Count > 0 ? lens[(int)((lens.Count - 1) * 0.9)] : -1;
                double over = cheb > 0 && lens.Count > 0 ? mean / cheb : 0;

                Console.WriteLine();
                Console.WriteLine(w.Name + " S=" + w.StartCell + " G=" + w.GoalCell +
                    " manh=" + manh + " cheb=" + cheb);
                Console.WriteLine("  DijkstraSuccess: reached=" + opt.ReachedGoal +
                    " steps=" + dSteps + " energy=" + dE.ToString("0.####"));
                Console.WriteLine("  Noisy TryReachGoal x" + attempts + ": success=" + lens.Count +
                    " fail=" + fails);
                Console.WriteLine("  Path.Count min/mean/max/p50/p90 = " +
                    min + "/" + mean.ToString("0.##") + "/" + max + "/" + p50 + "/" + p90 +
                    "  mean/cheb=" + over.ToString("0.###"));

                sb.AppendLine(string.Format("{0},{1},{2},{3},{4:0.####},{5},{6},{7},{8:0.##},{9},{10},{11},{12:0.###}",
                    w.Name, manh, cheb, dSteps, dE, attempts, fails, min, mean, max, p50, p90, over));
            }

            // Small populate smoke on Terrain-A only
            World wa = worlds.FirstOrDefault(x => x.Name == "Terrain-A") ?? worlds[0];
            Console.WriteLine();
            Console.WriteLine("=== SMALL PopulateBrainFromSuccesses Terrain-A (40 attempts) ===");
            var brain = new BrainMemory();
            brain.ConfigureForWorld(wa);
            var stats = TeacherPrimitive.PopulateBrainFromSuccesses(
                brain, new List<World> { wa }, 40, maxSteps, new Random(42),
                msg => Console.WriteLine(msg));
            Console.WriteLine("success_paths=" + stats.SuccessPaths +
                " failed=" + stats.FailedAttempts +
                " success_steps=" + stats.SuccessSteps +
                " avg_steps=" + (stats.SuccessPaths > 0 ? (stats.SuccessSteps / (double)stats.SuccessPaths).ToString("0.##") : "n/a") +
                " ltm=" + stats.LtmCount);

            string csvPath = Path.Combine(outDir, "teacher_path_diag.csv");
            File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
            Console.WriteLine();
            Console.WriteLine("Wrote " + csvPath);
            Console.WriteLine("TEACHER-DIAG done.");
            return 0;
        }


        static string ResolveOutDir(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--out" || args[i] == "-o")
                    return Path.GetFullPath(args[i + 1]);
            string stamp = DateTime.Now.ToString("yyyyMMdd");
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "EvalLogs", "review_" + stamp));
            try { Directory.CreateDirectory(candidate); return candidate; } catch { }
            return Path.GetFullPath(Path.Combine(baseDir, "EvalLogs", "review_" + stamp));
        }

        static Dictionary<string, double> BuildOptMap(List<World> worlds)
        {
            var map = new Dictionary<string, double>();
            foreach (var w in worlds)
            {
                double e; int s;
                DijkstraMinEnergy(w, w.StartCell, w.GoalCell, out e, out s);
                map[w.Name] = e;
            }
            return map;
        }

        static void WriteOptimalReference(List<World> worlds, string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("world,start_x,start_y,goal_x,goal_y,opt_energy,opt_steps,greedy_energy,greedy_steps,greedy_over_opt_pct");
            foreach (var w in worlds)
            {
                double optE; int optS;
                DijkstraMinEnergy(w, w.StartCell, w.GoalCell, out optE, out optS);
                double grE; int grS;
                GreedyDiagonalEnergy(w, w.StartCell, w.GoalCell, out grE, out grS);
                double over = optE > 1e-9 ? 100.0 * (grE - optE) / optE : 0;
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5:0.####},{6},{7:0.####},{8},{9:0.##}",
                    w.Name, w.StartCell.X, w.StartCell.Y, w.GoalCell.X, w.GoalCell.Y,
                    optE, optS, grE, grS, over));
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Console.WriteLine("Wrote " + path);
        }

        /// <summary>
        /// Phase A/B: LOO comparing SuccessWeighted vs TabularNgramBC vs EmptyLTM on Primary worlds.
        /// Uses EvalHarness.LooSeedList (n=5 Phase A) or BuildLooSeedList(LooSeedsN25) for Phase B.
        /// </summary>
        static void RunBaselineLooArm(List<World> worlds, string memDir, string outDir, bool hideDest, string tag)
        {
            Console.WriteLine("=== " + tag + " (SuccessWeighted | TabularNgramBC | EmptyLTM) ===");
            var rows = new StringBuilder();
            rows.AppendLine("seed,held_out,train_worlds,policy,hide_dest,runs,goals,goal_rate,avg_energy,energy_score,gap_to_opt,ltm_or_keys,votes_or_pref,rules_or_votes");

            // Per-world energy scores for GainPct vs EmptyLTM
            var swScores = new Dictionary<string, List<double>>();
            var bcScores = new Dictionary<string, List<double>>();
            var emptyScores = new Dictionary<string, List<double>>();
            var swGoal = new Dictionary<string, List<double>>();
            var bcGoal = new Dictionary<string, List<double>>();
            var emptyGoal = new Dictionary<string, List<double>>();
            foreach (var w in worlds)
            {
                swScores[w.Name] = new List<double>();
                bcScores[w.Name] = new List<double>();
                emptyScores[w.Name] = new List<double>();
                swGoal[w.Name] = new List<double>();
                bcGoal[w.Name] = new List<double>();
                emptyGoal[w.Name] = new List<double>();
            }

            for (int hi = 0; hi < worlds.Count; hi++)
            {
                var held = worlds[hi];
                var train = worlds.Where((w, i) => i != hi).ToList();
                string trainNames = string.Join("+", train.Select(t => t.Name));
                for (int si = 0; si < BaselineSeedsN; si++)
                {
                    int seed = BaselineSeedList[si];
                    Console.WriteLine("  BASELINE LOO held=" + held.Name + " seed=" + seed + " (" + (si + 1) + "/" + BaselineSeedsN + ")");
                    var log = new StringBuilder();
                    try
                    {
                        var rng = new Random(seed);
                        string memPath = Path.Combine(memDir, tag + "_" + held.Name + "_" + seed + ".mem");

                        BrainMemory swBrain;
                        TabularNgramBC bc;
                        TeacherPopulateStats swStats, bcStats;
                        Evaluator.TrainSwAndBcFromTeacher(
                            train, TeacherAttempts, MaxSteps, rng,
                            out swBrain, out bc, out swStats, out bcStats,
                            msg => { Console.WriteLine("    " + msg); log.AppendLine(msg); });

                        if (swBrain != null) swBrain.StripWorldShard(held.Name);
                        if (!string.IsNullOrEmpty(memPath) && swBrain != null)
                            swBrain.SaveToFile(memPath);

                        log.AppendLine("SW " + (swBrain != null ? swBrain.StatusLine() : "null"));
                        log.AppendLine("BC " + bc.StatusLine());
                        log.AppendLine("NOTES: BC = flat count/majority from SAME TeacherSuccess trajectories; no success-weight LTM promotion.");

                        int pairedBase = seed + 777;
                        var tSw = Evaluator.EvalFrozenNamedPolicy(held, "SuccessWeighted", swBrain, bc, EvalRuns, MaxSteps, pairedBase, hideDest);
                        var tBc = Evaluator.EvalFrozenNamedPolicy(held, "TabularNgramBC", swBrain, bc, EvalRuns, MaxSteps, pairedBase, hideDest);
                        var tEm = Evaluator.EvalFrozenNamedPolicy(held, "EmptyLTM", swBrain, bc, EvalRuns, MaxSteps, pairedBase, hideDest);

                        AppendPolicyRow(rows, seed, held.Name, trainNames, "SuccessWeighted", hideDest, tSw,
                            swBrain != null ? swBrain.LtmCount : 0, swStats.PreferredSignatures, swStats.RulesWritten);
                        AppendPolicyRow(rows, seed, held.Name, trainNames, "TabularNgramBC", hideDest, tBc,
                            bc.KeyCount, bc.TotalVotes, bc.KeyCount);
                        AppendPolicyRow(rows, seed, held.Name, trainNames, "EmptyLTM", hideDest, tEm, 0, 0, 0);

                        swScores[held.Name].Add(tSw.EnergyScore);
                        bcScores[held.Name].Add(tBc.EnergyScore);
                        emptyScores[held.Name].Add(tEm.EnergyScore);
                        swGoal[held.Name].Add(tSw.GoalRate);
                        bcGoal[held.Name].Add(tBc.GoalRate);
                        emptyGoal[held.Name].Add(tEm.GoalRate);

                        File.WriteAllText(Path.Combine(outDir, tag + "_raw_" + held.Name + "_" + seed + ".log"), log.ToString(), Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        BaselineSeedFailures++;
                        log.AppendLine("SEED_FAILURE: " + ex.Message);
                        Console.WriteLine("    SEED_FAILURE held=" + held.Name + " seed=" + seed + ": " + ex.Message);
                        File.WriteAllText(Path.Combine(outDir, tag + "_raw_" + held.Name + "_" + seed + "_FAIL.log"), log.ToString() + "\n" + ex, Encoding.UTF8);
                    }
                }
            }
            File.WriteAllText(Path.Combine(outDir, tag + "_raw.csv"), rows.ToString(), Encoding.UTF8);
            WriteBaselineGainSummary(Path.Combine(outDir, tag + "_summary.csv"), worlds,
                swScores, bcScores, emptyScores, swGoal, bcGoal, emptyGoal);
        }

        static void AppendPolicyRow(StringBuilder rows, int seed, string held, string trainNames, string policy,
            bool hideDest, EvalRunTotals t, int ltm, int pref, int written)
        {
            rows.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7:0.####},{8:0.####},{9:0.####},{10:0.####},{11},{12},{13}",
                seed, held, trainNames, policy, hideDest ? 1 : 0, t.Runs, t.GoalsReached, t.GoalRate, t.AvgEnergy, t.EnergyScore,
                GapToOpt(t.AvgEnergy, OptEnergyByWorld[held]), ltm, pref, written));
        }

        static void WriteBaselineGainSummary(string path, List<World> worlds,
            Dictionary<string, List<double>> swE, Dictionary<string, List<double>> bcE, Dictionary<string, List<double>> emE,
            Dictionary<string, List<double>> swG, Dictionary<string, List<double>> bcG, Dictionary<string, List<double>> emG)
        {
            var sb = new StringBuilder();
            sb.AppendLine("world,n,SW_energy_mean,SW_energy_std,BC_energy_mean,BC_energy_std,Empty_energy_mean,Empty_energy_std,SW_gain_pct_vs_Empty_mean,SW_gain_pct_vs_Empty_std,BC_gain_pct_vs_Empty_mean,BC_gain_pct_vs_Empty_std,SW_minus_BC_gain_pp_mean,SW_goal_mean,BC_goal_mean,Empty_goal_mean");
            foreach (var w in worlds)
            {
                var swGains = new List<double>();
                var bcGains = new List<double>();
                var diff = new List<double>();
                int n = swE[w.Name].Count;
                for (int i = 0; i < n; i++)
                {
                    double gSw = GainPct(emE[w.Name][i], swE[w.Name][i]);
                    double gBc = GainPct(emE[w.Name][i], bcE[w.Name][i]);
                    swGains.Add(gSw);
                    bcGains.Add(gBc);
                    diff.Add(gSw - gBc);
                }
                sb.AppendLine(string.Format("{0},{1},{2:0.####},{3:0.####},{4:0.####},{5:0.####},{6:0.####},{7:0.####},{8:0.####},{9:0.####},{10:0.####},{11:0.####},{12:0.####},{13:0.####},{14:0.####},{15:0.####}",
                    w.Name, n,
                    Mean(swE[w.Name]), Std(swE[w.Name]),
                    Mean(bcE[w.Name]), Std(bcE[w.Name]),
                    Mean(emE[w.Name]), Std(emE[w.Name]),
                    Mean(swGains), Std(swGains),
                    Mean(bcGains), Std(bcGains),
                    Mean(diff),
                    Mean(swG[w.Name]), Mean(bcG[w.Name]), Mean(emG[w.Name])));
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Console.WriteLine("Wrote " + path);
        }

        static void WriteBaselineSummaryMd(string outDir, TimeSpan elapsed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# " + BaselinePhaseLabel + " - Baseline LOO (SuccessWeighted vs TabularNgramBC vs EmptyLTM)");
            sb.AppendLine();
            sb.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " IST");
            sb.AppendLine("Wall clock: " + elapsed.TotalMinutes.ToString("0.0") + " min");
            sb.AppendLine();
            sb.AppendLine("## Protocol");
            sb.AppendLine("- LOO goal-known on " + BaselineWorldCount + " worlds (procedural=" + BaselineProceduralCount + ", baseSeed=" + BaselineBaseSeed + "): " + BaselineWorldNames);
            sb.AppendLine("- Seeds: BuildLooSeedList/LooSeedList n=" + BaselineSeedsN + " = {" + string.Join(",", BaselineSeedList) + "}");
            if (BaselineProceduralCount > 0)
                sb.AppendLine("- Phase C note: n=25 full LOO is overnight follow-up; this run uses n=" + BaselineSeedsN + ".");
            sb.AppendLine("- TeacherAttempts=" + TeacherAttempts + ", EvalRuns=" + EvalRuns + ", MaxSteps=" + MaxSteps);
            sb.AppendLine("- Policies: **SuccessWeighted** (feel-atlas LTM), **TabularNgramBC** (flat majority n-gram→action), **EmptyLTM** (reactive / no-memory)");
            sb.AppendLine("- BC trained from the **same** TeacherSuccess trajectories as SW (joint train); BC uses flat counts, no success-weight promotion.");
            sb.AppendLine("- Feel encoding: source cell; Smooth≤0.30, Rough≥0.65; n-gram len 3..5; action = relative move at window end.");
            sb.AppendLine("- BC test: exact key lookup (prefer longest); miss → EmptyLTM reactive.");
            sb.AppendLine("- GainPct = 100*(EmptyEnergyScore - PolicyEnergyScore)/EmptyEnergyScore (lower energy_score is better).");
            sb.AppendLine();
            sb.AppendLine("## NOTES");
            sb.AppendLine("- Mechanism names in logs: SuccessWeighted, TabularNgramBC, EmptyLTM.");
            sb.AppendLine("- No PPO/DQN. Simple learned baseline only.");
            sb.AppendLine("- Reviewer mirror: LifeSim.exe --eval-baseline");
            sb.AppendLine();
            AppendCsv(sb, "Optimal reference", Path.Combine(outDir, "optimal_reference.csv"));
            AppendCsv(sb, "LOO baseline summary (GainPct vs EmptyLTM)", Path.Combine(outDir, "loo_baseline_goalknown_summary.csv"));
            File.WriteAllText(Path.Combine(outDir, "SUMMARY.md"), sb.ToString(), Encoding.UTF8);
        }

        static void WriteBaselineVerdictMd(string outDir, TimeSpan elapsed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# " + BaselinePhaseLabel + " - VERDICT");
            sb.AppendLine();
            sb.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " IST");
            sb.AppendLine("Wall clock: " + elapsed.TotalMinutes.ToString("0.0") + " min");
            sb.AppendLine();
            string sumPath = Path.Combine(outDir, "loo_baseline_goalknown_summary.csv");
            bool swBeatsBc = false;
            int worldsSwWins = 0, worldsTotal = 0;
            var allSwGains = new List<double>();
            var allBcGains = new List<double>();
            if (File.Exists(sumPath))
            {
                var lines = File.ReadAllLines(sumPath);
                sb.AppendLine("## Mean±std GainPct vs EmptyLTM (energy)");
                sb.AppendLine();
                sb.AppendLine("| World | SuccessWeighted | TabularNgramBC | SW−BC (pp) | SW>BC? |");
                sb.AppendLine("|-------|-----------------|----------------|------------|--------|");
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var c = lines[i].Split(',');
                    if (c.Length < 13) continue;
                    worldsTotal++;
                    string world = c[0];
                    string swMean = c[8], swStd = c[9], bcMean = c[10], bcStd = c[11], diff = c[12];
                    double d, swG, bcG;
                    bool win = double.TryParse(diff, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out d) && d > 0;
                    if (win) worldsSwWins++;
                    if (double.TryParse(swMean, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out swG)) allSwGains.Add(swG);
                    if (double.TryParse(bcMean, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out bcG)) allBcGains.Add(bcG);
                    sb.AppendLine(string.Format("| {0} | {1} ± {2} | {3} ± {4} | {5} | {6} |",
                        world, swMean, swStd, bcMean, bcStd, diff, win ? "YES" : "NO"));
                }
                swBeatsBc = worldsTotal > 0 && worldsSwWins >= (worldsTotal + 1) / 2;
            }
            else
            {
                sb.AppendLine("(summary CSV missing)");
            }
            sb.AppendLine();
                                    sb.AppendLine("## Verdict");
            sb.AppendLine("- Worlds where SuccessWeighted GainPct > TabularNgramBC: **" + worldsSwWins + "/" + worldsTotal + "**");
            if (allSwGains.Count > 0)
            {
                sb.AppendLine("- Mean across worlds SW GainPct: **" + Mean(allSwGains).ToString("0.00") + " +/- " + Std(allSwGains).ToString("0.00") + "**");
                sb.AppendLine("- Mean across worlds BC GainPct: **" + Mean(allBcGains).ToString("0.00") + " +/- " + Std(allBcGains).ToString("0.00") + "**");
            }
            sb.AppendLine("- Does SW still beat tabular BC overall? **" + (swBeatsBc ? "YES" : "NO / MIXED") + "**");
            sb.AppendLine("- EmptyLTM is the reactive baseline; GainPct is vs EmptyLTM.");
            sb.AppendLine("- World count: **" + BaselineWorldCount + "** (procedural=" + BaselineProceduralCount + ", baseSeed=" + BaselineBaseSeed + ")");
            sb.AppendLine("- Seed count n=" + BaselineSeedsN + "; seed failures: **" + BaselineSeedFailures + "**");
            if (BaselineProceduralCount > 0)
                sb.AppendLine("- Note: n=25 full LOO on 20 worlds is overnight follow-up; this run is n=" + BaselineSeedsN + ".");
            sb.AppendLine();
            sb.AppendLine("## Build");
            sb.AppendLine("- Fork only: `--worlds` / `--procedural` / `--base-seed` / `--seeds` wired in EvalHarness.");
            sb.AppendLine("- `LifeSim.sln` -> `EvolutionApp\\bin\\Debug\\LifeSim.exe --eval-baseline --seeds " + BaselineSeedsN +
                (BaselineProceduralCount > 0
                    ? (" --worlds " + BaselineWorldCount + " --procedural " + BaselineProceduralCount)
                    : "") + "`");
            File.WriteAllText(Path.Combine(outDir, "VERDICT.md"), sb.ToString(), Encoding.UTF8);
        }

        /// <summary>Deterministic seed list for Phase B (LooSeedsN25).</summary>
        public static int[] BuildLooSeedList(int n)
        {
            if (n <= 0) n = LooSeedsN25;
            var list = new int[n];
            // First 5 match Phase A for continuity; rest are 100*k+1 style.
            int[] base5 = { 101, 202, 303, 404, 505 };
            for (int i = 0; i < n; i++)
            {
                if (i < base5.Length) list[i] = base5[i];
                else list[i] = 100 * (i + 1) + 1; // 601,701,... then 1001,...
            }
            // Fix the formula for i>=5: want 606,707,... actually use 101*(i+1)
            for (int i = 5; i < n; i++)
                list[i] = 101 * (i + 1);
            return list;
        }

        static void RunTeacherArm(List<World> worlds, string memDir, string outDir, bool hideDest, string tag, bool unused)
        {
            Console.WriteLine("=== " + tag + " ===");
            var rows = new StringBuilder();
            rows.AppendLine("seed,world,mode,hide_dest,runs,goals,goal_rate,avg_energy,energy_score,gap_to_opt,ltm_count,pref_sigs,rules_written");
            var gains = NewGains(worlds);

            for (int si = 0; si < PrefSeedsN; si++)
            {
                int seed = PrefSeedList[si];
                Console.WriteLine("  seed " + seed);
                var log = new StringBuilder();
                var rng = new Random(seed);
                string memPath = Path.Combine(memDir, tag + "_seed_" + seed + ".mem");
                var result = Evaluator.RunTeacherSuccessHeadless(
                    worlds, TeacherAttempts, EvalRuns, MaxSteps, rng, log, memPath,
                    msg => Console.WriteLine("    " + msg), hideDest);
                File.WriteAllText(Path.Combine(outDir, tag + "_raw_seed_" + seed + ".log"), log.ToString(), Encoding.UTF8);

                int ltm = result.Brain != null ? result.Brain.LtmCount : 0;
                int pref = result.PreferredSignatures;
                int written = result.RulesWritten;
                foreach (var w in worlds)
                {
                    var wb = result.Transfer.WithBrain[w.Name];
                    var nb = result.Transfer.NoBrain[w.Name];
                    rows.AppendLine(FormatRow(seed, w.Name, "with_brain", hideDest, wb, ltm, pref, written));
                    rows.AppendLine(FormatRow(seed, w.Name, "no_brain", hideDest, nb, ltm, pref, written));
                    RecordGain(gains, w.Name, wb, nb);
                }
            }
            File.WriteAllText(Path.Combine(outDir, tag + "_raw.csv"), rows.ToString(), Encoding.UTF8);
            WriteGainSummary(Path.Combine(outDir, tag + "_summary.csv"), gains, worlds);
        }

        static void RunLooArm(List<World> worlds, string memDir, string outDir, bool hideDest, string tag)
        {
            Console.WriteLine("=== " + tag + " ===");
            var rows = new StringBuilder();
            rows.AppendLine("seed,held_out,train_worlds,mode,hide_dest,runs,goals,goal_rate,avg_energy,energy_score,gap_to_opt,ltm_count,pref_sigs,rules_written");
            var gains = NewGains(worlds);

            for (int hi = 0; hi < worlds.Count; hi++)
            {
                var held = worlds[hi];
                var train = worlds.Where((w, i) => i != hi).ToList();
                string trainNames = string.Join("+", train.Select(t => t.Name));
                for (int si = 0; si < LooSeedsN; si++)
                {
                    int seed = LooSeedList[si];
                    Console.WriteLine("  LOO held=" + held.Name + " seed=" + seed);
                    var log = new StringBuilder();
                    var rng = new Random(seed);
                    string memPath = Path.Combine(memDir, tag + "_" + held.Name + "_" + seed + ".mem");
                    // Train without evaluating on train set (evalHide=false but we ignore Transfer; re-eval held)
                    var result = Evaluator.RunTeacherSuccessHeadless(
                        train, TeacherAttempts, 1, MaxSteps, rng, log, memPath, null, false);
                    // Honest LOO: strip held-out WorldId shard if any leaked into atlas
                    if (result.Brain != null) result.Brain.StripWorldShard(held.Name);

                    EvalRunTotals wb, nb;
                    Evaluator.EvalFrozenPaired(held, result.Brain, EvalRuns, MaxSteps, seed + 777,
                        out wb, out nb, hideDest);

                    int ltm = result.Brain.LtmCount;
                    int pref = result.PreferredSignatures;
                    int written = result.RulesWritten;
                    rows.AppendLine(string.Format("{0},{1},{2},with_brain,{3},{4},{5},{6:0.####},{7:0.####},{8:0.####},{9:0.####},{10},{11},{12}",
                        seed, held.Name, trainNames, hideDest ? 1 : 0, wb.Runs, wb.GoalsReached, wb.GoalRate, wb.AvgEnergy, wb.EnergyScore,
                        GapToOpt(wb.AvgEnergy, OptEnergyByWorld[held.Name]), ltm, pref, written));
                    rows.AppendLine(string.Format("{0},{1},{2},no_brain,{3},{4},{5},{6:0.####},{7:0.####},{8:0.####},{9:0.####},{10},{11},{12}",
                        seed, held.Name, trainNames, hideDest ? 1 : 0, nb.Runs, nb.GoalsReached, nb.GoalRate, nb.AvgEnergy, nb.EnergyScore,
                        GapToOpt(nb.AvgEnergy, OptEnergyByWorld[held.Name]), ltm, pref, written));
                    RecordGain(gains, held.Name, wb, nb);
                    File.WriteAllText(Path.Combine(outDir, tag + "_raw_" + held.Name + "_" + seed + ".log"), log.ToString(), Encoding.UTF8);
                }
            }
            File.WriteAllText(Path.Combine(outDir, tag + "_raw.csv"), rows.ToString(), Encoding.UTF8);
            WriteGainSummary(Path.Combine(outDir, tag + "_summary.csv"), gains, worlds);
        }

        static void RunTransferTrainA(List<World> worlds, string memDir, string outDir)
        {
            Console.WriteLine("=== Transfer train Primary[0] ===");
            var rows = new StringBuilder();
            rows.AppendLine("seed,train_world,test_world,mode,runs,goals,goal_rate,avg_energy,energy_score,gap_to_opt,same_world");
            var gains = NewGains(worlds);
            var trainWorld = worlds[0];
            for (int si = 0; si < TransferSeedsN; si++)
            {
                int seed = TransferSeedList[si];
                Console.WriteLine("  Transfer seed " + seed);
                var log = new StringBuilder();
                var rng = new Random(seed);
                string memPath = Path.Combine(memDir, "transfer_A_" + seed + ".mem");
                var trainList = new List<World> { trainWorld };
                var result = Evaluator.RunTeacherSuccessHeadless(
                    trainList, TeacherAttempts, 1, MaxSteps, rng, log, memPath,
                    msg => Console.WriteLine("    " + msg), false);
                foreach (var w in worlds)
                {
                    EvalRunTotals wb, nb;
                    Evaluator.EvalFrozenPaired(w, result.Brain, EvalRuns, MaxSteps, seed + 42,
                        out wb, out nb, false);
                    bool same = w.Name == trainWorld.Name;
                    rows.AppendLine(string.Format("{0},{1},{2},with_brain,{3},{4},{5:0.####},{6:0.####},{7:0.####},{8:0.####},{9}",
                        seed, trainWorld.Name, w.Name, wb.Runs, wb.GoalsReached, wb.GoalRate, wb.AvgEnergy, wb.EnergyScore,
                        GapToOpt(wb.AvgEnergy, OptEnergyByWorld[w.Name]), same ? 1 : 0));
                    rows.AppendLine(string.Format("{0},{1},{2},no_brain,{3},{4},{5:0.####},{6:0.####},{7:0.####},{8:0.####},{9}",
                        seed, trainWorld.Name, w.Name, nb.Runs, nb.GoalsReached, nb.GoalRate, nb.AvgEnergy, nb.EnergyScore,
                        GapToOpt(nb.AvgEnergy, OptEnergyByWorld[w.Name]), same ? 1 : 0));
                    RecordGain(gains, w.Name, wb, nb);
                }
                File.WriteAllText(Path.Combine(outDir, "transfer_raw_seed_" + seed + ".log"), log.ToString(), Encoding.UTF8);
            }
            File.WriteAllText(Path.Combine(outDir, "transfer_baseline_raw.csv"), rows.ToString(), Encoding.UTF8);
            WriteGainSummary(Path.Combine(outDir, "transfer_baseline_summary.csv"), gains, worlds);
        }

        static void RunAblation(List<World> worlds, string outDir)
        {
            Console.WriteLine("=== Ablation empty LTM ===");
            var rows = new StringBuilder();
            rows.AppendLine("seed,world,mode,runs,goals,goal_rate,avg_energy,energy_score,gap_to_opt");
            var gains = NewGains(worlds);
            for (int si = 0; si < AblationSeedsN; si++)
            {
                int seed = AblationSeedList[si];
                var empty = new BrainMemory();
                foreach (var w in worlds)
                {
                    EvalRunTotals wb, nb;
                    Evaluator.EvalFrozenPaired(w, empty, AblationRuns, MaxSteps, seed, out wb, out nb, false);
                    rows.AppendLine(string.Format("{0},{1},empty_brain,{2},{3},{4:0.####},{5:0.####},{6:0.####},{7:0.####}",
                        seed, w.Name, wb.Runs, wb.GoalsReached, wb.GoalRate, wb.AvgEnergy, wb.EnergyScore,
                        GapToOpt(wb.AvgEnergy, OptEnergyByWorld[w.Name])));
                    rows.AppendLine(string.Format("{0},{1},no_brain,{2},{3},{4:0.####},{5:0.####},{6:0.####},{7:0.####}",
                        seed, w.Name, nb.Runs, nb.GoalsReached, nb.GoalRate, nb.AvgEnergy, nb.EnergyScore,
                        GapToOpt(nb.AvgEnergy, OptEnergyByWorld[w.Name])));
                    RecordGain(gains, w.Name, wb, nb);
                }
            }
            File.WriteAllText(Path.Combine(outDir, "ablation_empty_ltm_raw.csv"), rows.ToString(), Encoding.UTF8);
            WriteGainSummary(Path.Combine(outDir, "ablation_empty_ltm_summary.csv"), gains, worlds);
        }

        class GainMaps
        {
            public Dictionary<string, List<double>> Energy = new Dictionary<string, List<double>>();
            public Dictionary<string, List<double>> Goal = new Dictionary<string, List<double>>();
            public Dictionary<string, List<double>> BrainGap = new Dictionary<string, List<double>>();
            public Dictionary<string, List<double>> NoBrainGap = new Dictionary<string, List<double>>();
        }

        static GainMaps NewGains(List<World> worlds)
        {
            var g = new GainMaps();
            foreach (var w in worlds)
            {
                g.Energy[w.Name] = new List<double>();
                g.Goal[w.Name] = new List<double>();
                g.BrainGap[w.Name] = new List<double>();
                g.NoBrainGap[w.Name] = new List<double>();
            }
            return g;
        }

        static void RecordGain(GainMaps g, string name, EvalRunTotals wb, EvalRunTotals nb)
        {
            g.Energy[name].Add(GainPct(nb.EnergyScore, wb.EnergyScore));
            g.Goal[name].Add(100.0 * (wb.GoalRate - nb.GoalRate));
            double opt = OptEnergyByWorld.ContainsKey(name) ? OptEnergyByWorld[name] : double.NaN;
            g.BrainGap[name].Add(GapToOpt(wb.AvgEnergy, opt));
            g.NoBrainGap[name].Add(GapToOpt(nb.AvgEnergy, opt));
        }

        static string FormatRow(int seed, string world, string mode, bool hide, EvalRunTotals t, int ltm, int pref, int written)
        {
            double gap = OptEnergyByWorld.ContainsKey(world) ? GapToOpt(t.AvgEnergy, OptEnergyByWorld[world]) : 0;
            return string.Format("{0},{1},{2},{3},{4},{5},{6:0.####},{7:0.####},{8:0.####},{9:0.####},{10},{11},{12}",
                seed, world, mode, hide ? 1 : 0, t.Runs, t.GoalsReached, t.GoalRate, t.AvgEnergy, t.EnergyScore, gap, ltm, pref, written);
        }

        static double GainPct(double noBrainScore, double withBrainScore)
        {
            if (Math.Abs(noBrainScore) < 1e-9) return 0;
            return 100.0 * (noBrainScore - withBrainScore) / noBrainScore;
        }

        static double GapToOpt(double avgEnergy, double opt)
        {
            if (opt < 1e-9 || double.IsNaN(avgEnergy) || double.IsNaN(opt)) return double.NaN;
            return avgEnergy / opt;
        }

        static void WriteGainSummary(string path, GainMaps gains, List<World> worlds)
        {
            var sb = new StringBuilder();
            sb.AppendLine("world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std");
            foreach (var w in worlds)
            {
                var eg = gains.Energy[w.Name];
                var gg = gains.Goal[w.Name];
                sb.AppendLine(string.Format("{0},{1},{2:0.####},{3:0.####},{4:0.####},{5:0.####},{6:0.####},{7:0.####},{8:0.####},{9:0.####}",
                    w.Name, eg.Count, Mean(eg), Std(eg), Mean(gg), Std(gg),
                    Mean(gains.BrainGap[w.Name]), Std(gains.BrainGap[w.Name]),
                    Mean(gains.NoBrainGap[w.Name]), Std(gains.NoBrainGap[w.Name])));
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Console.WriteLine("Wrote " + path);
        }

        static double Mean(List<double> xs)
        {
            if (xs == null || xs.Count == 0) return 0;
            double s = 0; int n = 0;
            foreach (var x in xs) if (!double.IsNaN(x)) { s += x; n++; }
            return n > 0 ? s / n : 0;
        }

        static double Std(List<double> xs)
        {
            if (xs == null || xs.Count < 2) return 0;
            var clean = xs.Where(x => !double.IsNaN(x)).ToList();
            if (clean.Count < 2) return 0;
            double m = Mean(clean);
            double v = 0; foreach (var x in clean) v += (x - m) * (x - m);
            return Math.Sqrt(v / (clean.Count - 1));
        }

        static void DijkstraMinEnergy(World w, System.Drawing.Point start, System.Drawing.Point goal, out double energy, out int steps)
        {
            int cols = w.Cols, rows = w.Rows;
            var dist = new double[cols, rows];
            var prevX = new int[cols, rows];
            var prevY = new int[cols, rows];
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    dist[x, y] = double.MaxValue;
            dist[start.X, start.Y] = 0;
            var open = new List<System.Drawing.Point>();
            open.Add(start);
            int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
            int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };
            while (open.Count > 0)
            {
                int bi = 0;
                for (int i = 1; i < open.Count; i++)
                    if (dist[open[i].X, open[i].Y] < dist[open[bi].X, open[bi].Y]) bi = i;
                var cur = open[bi];
                open.RemoveAt(bi);
                if (cur == goal) break;
                for (int d = 0; d < 8; d++)
                {
                    int nx = cur.X + dx[d], ny = cur.Y + dy[d];
                    if (nx < 0 || ny < 0 || nx >= cols || ny >= rows) continue;
                    if (w.CellHasObstacle(nx, ny)) continue;
                    double nd = dist[cur.X, cur.Y] + w.CellEnergyCost(nx, ny);
                    if (nd < dist[nx, ny])
                    {
                        bool wasInf = dist[nx, ny] >= double.MaxValue / 2;
                        dist[nx, ny] = nd;
                        prevX[nx, ny] = cur.X;
                        prevY[nx, ny] = cur.Y;
                        if (wasInf) open.Add(new System.Drawing.Point(nx, ny));
                    }
                }
            }
            energy = dist[goal.X, goal.Y];
            steps = 0;
            if (energy >= double.MaxValue / 2) { energy = double.NaN; steps = -1; return; }
            int cx = goal.X, cy = goal.Y;
            while (!(cx == start.X && cy == start.Y))
            {
                int px = prevX[cx, cy], py = prevY[cx, cy];
                cx = px; cy = py; steps++;
                if (steps > cols * rows + 5) break;
            }
        }

        static void GreedyDiagonalEnergy(World w, System.Drawing.Point start, System.Drawing.Point goal, out double energy, out int steps)
        {
            int x = start.X, y = start.Y;
            energy = 0; steps = 0;
            int guard = 0;
            while (!(x == goal.X && y == goal.Y) && guard++ < 500)
            {
                int sx = x == goal.X ? 0 : (goal.X > x ? 1 : -1);
                int sy = y == goal.Y ? 0 : (goal.Y > y ? 1 : -1);
                x += sx; y += sy;
                energy += w.CellEnergyCost(x, y);
                steps++;
            }
        }

        static void WriteSummaryMd(string outDir, TimeSpan elapsed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# LifeSim Review Evaluation");
            sb.AppendLine();
            sb.AppendLine("Wall clock: " + elapsed.TotalMinutes.ToString("0.0") + " min");
            sb.AppendLine();
            sb.AppendLine("## Protocol");
            sb.AppendLine("- TeacherSuccessâ†’LTM (goal-known teacher; failures discarded).");
            sb.AppendLine("- Fix2 honesty: ClearStm / CloneFrozenLtmOnly; EvalFrozenPaired; temp mem.");
            sb.AppendLine("- Arms: in-sample + LOO Ã— {goal-known, dest-hidden}; transfer A; empty-LTM ablation.");
            sb.AppendLine("- Worlds: WorldLibrary.Primary = Terrain-A..D. Sense: local 8-neighbor.");
            sb.AppendLine();
            sb.AppendLine("## Files");
            foreach (var f in Directory.GetFiles(outDir).OrderBy(x => x))
                sb.AppendLine("- " + Path.GetFileName(f));
            sb.AppendLine();
            AppendCsv(sb, "Optimal reference", Path.Combine(outDir, "optimal_reference.csv"));
            AppendCsv(sb, "Teacher in-sample (goal-known)", Path.Combine(outDir, "teacher_insample_summary.csv"));
            AppendCsv(sb, "Teacher in-sample (dest-hidden)", Path.Combine(outDir, "teacher_insample_desthidden_summary.csv"));
            AppendCsv(sb, "LOO goal-known", Path.Combine(outDir, "loo_goalknown_summary.csv"));
            AppendCsv(sb, "LOO dest-hidden", Path.Combine(outDir, "loo_desthidden_summary.csv"));
            AppendCsv(sb, "Transfer train A", Path.Combine(outDir, "transfer_baseline_summary.csv"));
            AppendCsv(sb, "Ablation empty LTM", Path.Combine(outDir, "ablation_empty_ltm_summary.csv"));
            File.WriteAllText(Path.Combine(outDir, "SUMMARY.md"), sb.ToString(), Encoding.UTF8);
        }

        static void WriteVerdictMd(string outDir, TimeSpan elapsed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# LifeSim â€” Verdict");
            sb.AppendLine();
            sb.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " IST");
            sb.AppendLine("Wall clock: " + elapsed.TotalMinutes.ToString("0.0") + " min");
            sb.AppendLine();
            sb.AppendLine("## Design");
            sb.AppendLine("- TeacherSuccessâ†’LTM; policy `brain-feel-atlas`; feel n-gram atlas (no 8-neigh primary).");
            sb.AppendLine("- Fix2 honesty preserved. Terrain Primary; Canvas retained in All.");
            sb.AppendLine();
            sb.AppendLine("## Fix2 LOO bar (Prefâ†’LTM, goal-known)");
            sb.AppendLine("| World | Fix2 LOO % | â‰¥25%? |");
            sb.AppendLine("|-------|------------|-------|");
            sb.AppendLine("| A | 27.1 Â± 12.1 | YES |");
            sb.AppendLine("| B | 4.0 Â± 4.8 | NO |");
            sb.AppendLine("| C | 30.1 Â± 16.8 | YES |");
            sb.AppendLine("| D | 13.5 Â± 6.1 | NO |");
            sb.AppendLine("Bar (â‰¥25% on â‰¥2/4): **MET on Fix2** (A+C).");
            sb.AppendLine();
            sb.AppendLine("## LOO goal-known");
            AppendCsv(sb, null, Path.Combine(outDir, "loo_goalknown_summary.csv"));
            sb.AppendLine("## LOO dest-hidden");
            AppendCsv(sb, null, Path.Combine(outDir, "loo_desthidden_summary.csv"));
            sb.AppendLine("## Ablation");
            AppendCsv(sb, null, Path.Combine(outDir, "ablation_empty_ltm_summary.csv"));
            sb.AppendLine();
            sb.AppendLine("## Honest verdict");
            sb.AppendLine("Compare LOO means above to Fix2 bar. Dest-hidden tests whether success-path features alone guide without goal coordinates.");
            sb.AppendLine();
            sb.AppendLine("## Build");
            sb.AppendLine("- `LifeSim.sln` â†’ `EvolutionApp\\bin\\Debug\\LifeSim.exe`");
            File.WriteAllText(Path.Combine(outDir, "VERDICT.md"), sb.ToString(), Encoding.UTF8);
        }

        static void AppendCsv(StringBuilder sb, string title, string path)
        {
            if (!string.IsNullOrEmpty(title)) sb.AppendLine("### " + title);
            if (!File.Exists(path)) { sb.AppendLine("(missing: " + Path.GetFileName(path) + ")"); sb.AppendLine(); return; }
            sb.AppendLine("```");
            sb.AppendLine(File.ReadAllText(path).TrimEnd());
            sb.AppendLine("```");
            sb.AppendLine();
        }
    }
}



