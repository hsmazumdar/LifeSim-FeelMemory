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
        public const int TransferSeedsN = 3;
        public const int AblationSeedsN = 5;
        public const int AblationRuns = 40;
        public const int MaxSteps = 2000;

        static readonly int[] PrefSeedList = { 11, 22, 33, 44, 55 };
        static readonly int[] LooSeedList = { 101, 202, 303, 404, 505 };
        static readonly int[] TransferSeedList = { 7, 17, 27 };
        static Dictionary<string, double> OptEnergyByWorld;

        static readonly int[] AblationSeedList = { 3, 13, 23, 43, 53 };
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
                Console.WriteLine("SMOKE WARN: no FEEL rules persisted â€” check teacher populate / Fix A");
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
            string candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "EvalLogs", "paper_results_" + stamp));
            try { Directory.CreateDirectory(candidate); return candidate; } catch { }
            // Portable fallback: repo-root EvalLogs (no machine-specific drive letters).
            string repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            return Path.GetFullPath(Path.Combine(repoRoot, "EvalLogs", "paper_results_" + stamp));
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
            sb.AppendLine("# LifeSim V16 Review Evaluation");
            sb.AppendLine();
            sb.AppendLine("Wall clock: " + elapsed.TotalMinutes.ToString("0.0") + " min");
            sb.AppendLine();
            sb.AppendLine("## Protocol");
            sb.AppendLine("- TeacherSuccessÃ¢â€ â€™LTM (goal-known teacher; failures discarded).");
            sb.AppendLine("- Fix2 honesty: ClearStm / CloneFrozenLtmOnly; EvalFrozenPaired; temp mem.");
            sb.AppendLine("- Arms: in-sample + LOO Ãƒâ€” {goal-known, dest-hidden}; transfer A; empty-LTM ablation.");
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
            sb.AppendLine("# LifeSim V16 Ã¢â‚¬â€ Verdict");
            sb.AppendLine();
            sb.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " IST");
            sb.AppendLine("Wall clock: " + elapsed.TotalMinutes.ToString("0.0") + " min");
            sb.AppendLine();
            sb.AppendLine("## Design");
            sb.AppendLine("- TeacherSuccessÃ¢â€ â€™LTM; policy `brain-feel-atlas`; feel n-gram atlas (no 8-neigh primary).");
            sb.AppendLine("- Fix2 honesty preserved. Terrain Primary; Canvas retained in All.");
            sb.AppendLine();
            sb.AppendLine("## Fix2 LOO bar (PrefÃ¢â€ â€™LTM, goal-known)");
            sb.AppendLine("| World | Fix2 LOO % | Ã¢â€°Â¥25%? |");
            sb.AppendLine("|-------|------------|-------|");
            sb.AppendLine("| A | 27.1 Ã‚Â± 12.1 | YES |");
            sb.AppendLine("| B | 4.0 Ã‚Â± 4.8 | NO |");
            sb.AppendLine("| C | 30.1 Ã‚Â± 16.8 | YES |");
            sb.AppendLine("| D | 13.5 Ã‚Â± 6.1 | NO |");
            sb.AppendLine("Bar (Ã¢â€°Â¥25% on Ã¢â€°Â¥2/4): **MET on Fix2** (A+C).");
            sb.AppendLine();
            sb.AppendLine("## V16 LOO goal-known");
            AppendCsv(sb, null, Path.Combine(outDir, "loo_goalknown_summary.csv"));
            sb.AppendLine("## V16 LOO dest-hidden");
            AppendCsv(sb, null, Path.Combine(outDir, "loo_desthidden_summary.csv"));
            sb.AppendLine("## Ablation");
            AppendCsv(sb, null, Path.Combine(outDir, "ablation_empty_ltm_summary.csv"));
            sb.AppendLine();
            sb.AppendLine("## Honest verdict");
            sb.AppendLine("Compare V16 LOO means above to Fix2 bar. Dest-hidden tests whether success-path features alone guide without goal coordinates.");
            sb.AppendLine();
            sb.AppendLine("## Build");
            sb.AppendLine("- `LifeSim.sln` Ã¢â€ â€™ `EvolutionApp\\bin\\Debug\\LifeSim.exe`");
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



