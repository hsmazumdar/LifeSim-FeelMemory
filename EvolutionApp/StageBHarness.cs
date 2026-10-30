using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Evolution
{
    /// <summary>
    /// R1 Stage B harness: LifeSim.exe --eval-stageb [--seed-list 101,202] [--worlds 20 --procedural 16] [--base-seed 9001]
    ///   [--ngram 5] [--world-filter A,B] [--modes gk,dh] [--tune-runs 8] [--tune-folds 3] --out DIR
    /// Same LOO protocol / constants / paired test episode seeds (seed+777) as --eval-revision.
    /// Adds goal-conditioned (GC) baselines and a per-fold tuning sweep that only ever touches TRAINING worlds:
    /// for each outer fold, tuneFolds training worlds are used as inner validation worlds; for each, all baseline
    /// arms are rebuilt from the teacher rollouts of the remaining training worlds and scored on that world
    /// (tuning episode seeds = seed+555, never the test seeds). The held-out world is never used for selection.
    /// </summary>
    public static class StageBHarness
    {
        internal sealed class Cfg
        {
            public double Boost = 4.0; public int K = 5; public double Wg = 0.5;
            public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "boost={0};K={1};Wg={2}", Boost, K, Wg); }
        }

        static readonly string[] BaseArms = { "TabularNgramBC", "KnnFeel", "EpisodicControl", "TabularQ" };
        static readonly string[] GcArms = { "TabularNgramBC_GC", "KnnFeel_GC", "EpisodicControl_GC", "TabularQ_GC" };

        static string Arg(string[] args, string name, string def)
        {
            for (int i = 0; i < args.Length - 1; i++) if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return def;
        }

        static List<Cfg> Grid(string arm)
        {
            var g = new List<Cfg>();
            double[] boosts = { 2.0, 4.0, 8.0 };
            int[] ks = { 3, 5, 9 };
            double[] wgs = { 0.25, 1.0 };
            bool knnLike = arm.StartsWith("KnnFeel") || arm.StartsWith("EpisodicControl");
            bool gc = arm.EndsWith("_GC");
            foreach (var b in boosts)
            {
                if (!knnLike) { g.Add(new Cfg { Boost = b }); continue; }
                foreach (var k in ks)
                {
                    if (!gc) { g.Add(new Cfg { Boost = b, K = k }); continue; }
                    foreach (var w in wgs) g.Add(new Cfg { Boost = b, K = k, Wg = w });
                }
            }
            return g;
        }

        /// <summary>Configure the arm's objects for cfg and return the policy object the robot should use.</summary>
        static IFeelPolicy Arm(StageBSet s, string arm, Cfg c)
        {
            s.Knn.K = c.K; s.Ec.K = c.K;
            s.GKnn.K = c.K; s.GKnn.Wg = c.Wg; s.GKnn.Wp = c.Wg * 0.5;
            s.GEc.K = c.K; s.GEc.Wg = c.Wg; s.GEc.Wp = c.Wg * 0.5;
            switch (arm)
            {
                case "TabularNgramBC": return s.Bc;
                case "KnnFeel": return s.Knn;
                case "EpisodicControl": return s.Ec;
                case "TabularQ": return s.Q;
                case "TabularNgramBC_GC": return s.GBc;
                case "KnnFeel_GC": return s.GKnn;
                case "EpisodicControl_GC": return s.GEc;
                case "TabularQ_GC": return s.GQ;
            }
            throw new ArgumentException(arm);
        }

        internal static int SetupArm(Robot robot, StageBSet s, string arm, Cfg c)
        {
            var p = Arm(s, arm, c);
            if (p is TabularNgramBC) robot.TabularBC = (TabularNgramBC)p; else robot.FeelPolicy = p;
            robot.BcBoost = c.Boost;
            return p.KeyCount;
        }

        internal static int SetupBrain(Robot robot, BrainMemory brain, World world, bool hide, bool distProg, bool maskGoal)
        {
            var clone = brain.CloneFrozenLtmOnly();
            if (distProg)
            {
                clone.UseDistanceProgress = true;
                if (!hide) { clone.HasTestGoal = true; clone.TestStart = world.StartCell; clone.TestGoal = world.GoalCell; }
            }
            robot.UseBrain = true; robot.SetBrain(clone); robot.MaskBrainGoalContext = maskGoal;
            return brain.LtmCount;
        }

        internal static EvalRunTotals Eval(World world, Func<Robot, int> setup, int runs, int maxSteps, int pairedBaseSeed, bool hide, out int keys)
        {
            keys = 0;
            var totals = new EvalRunTotals { Runs = runs, Label = world.Name };
            for (int r = 1; r <= runs; r++)
            {
                int episodeSeed = Evaluator.MixEpisodeSeed(pairedBaseSeed, r);
                var robot = new Robot(world.StartCell, new Random(episodeSeed));
                robot.LearnEnabled = false; robot.RecordSenseTrace = false; robot.ClearBrainOnReset = false;
                robot.HideDestination = hide;
                robot.TabularBC = null; robot.FeelPolicy = null; robot.UseBrain = false;
                robot.SetBrain(new BrainMemory());
                keys = setup != null ? setup(robot) : 0;
                int ticks = 0, safety = maxSteps * 20;
                while (!robot.IsOnGoal && robot.DecisionSteps < maxSteps && ticks < safety) { robot.Tick(world); ticks++; }
                if (robot.IsOnGoal) { totals.GoalsReached++; totals.EnergyToGoalSum += robot.TotalEnergy; }
                totals.TotalEnergy += robot.TotalEnergy;
                totals.StepsSmooth += robot.StepsSmooth; totals.StepsMedium += robot.StepsMedium;
                totals.StepsRough += robot.StepsRough; totals.StepsObstacle += robot.StepsOnObstacle;
                totals.DecisionSteps += robot.DecisionSteps;
            }
            return totals;
        }

        public static int Run(string[] args)
        {
            string outDir = Arg(args, "--out", Path.Combine(Environment.CurrentDirectory, "EvalLogs", "stageB"));
            int proc = int.Parse(Arg(args, "--procedural", "16"));
            int baseSeed = int.Parse(Arg(args, "--base-seed", "9001"));
            int ngram = int.Parse(Arg(args, "--ngram", "5"));
            int tuneRuns = int.Parse(Arg(args, "--tune-runs", "8"));
            int tuneFolds = int.Parse(Arg(args, "--tune-folds", "3"));
            var modes = Arg(args, "--modes", "gk,dh").Split(',').Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0).ToList();
            int[] seeds = Arg(args, "--seed-list", "101,202,303").Split(',').Select(int.Parse).ToArray();
            string filter = Arg(args, "--world-filter", "");

            var worlds = WorldLibrary.PrimaryPlusProcedural(proc, baseSeed).ToList();
            var heldList = worlds;
            if (filter.Length > 0) { var f = new HashSet<string>(filter.Split(',')); heldList = worlds.Where(w => f.Contains(w.Name)).ToList(); }

            Directory.CreateDirectory(outDir);
            var meta = new StringBuilder();
            meta.AppendLine("cmd=" + string.Join(" ", args));
            meta.AppendLine("seeds=" + string.Join(",", seeds));
            meta.AppendLine("worlds=" + string.Join(",", worlds.Select(w => w.Name)));
            meta.AppendLine("held_out=" + string.Join(",", heldList.Select(w => w.Name)));
            meta.AppendLine("modes=" + string.Join(",", modes) + " tune_runs=" + tuneRuns + " tune_folds=" + tuneFolds + " ngram=" + ngram);
            meta.AppendLine("TeacherAttempts=" + EvalHarness.TeacherAttempts + " EvalRuns=" + EvalHarness.EvalRuns + " MaxSteps=" + EvalHarness.MaxSteps);
            meta.AppendLine("started=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (PC-local IST)");
            File.WriteAllText(Path.Combine(outDir, "run_meta.txt"), meta.ToString());
            Console.WriteLine(meta);

            string rawPath = Path.Combine(outDir, "stageb_raw.csv");
            string tunePath = Path.Combine(outDir, "stageb_tuning.csv");
            if (!File.Exists(rawPath)) File.WriteAllText(rawPath, "seed,held_out,mode,policy,runs,goals,goal_rate,avg_energy,energy_score,keys,cfg,train_sec,eval_sec\n");
            if (!File.Exists(tunePath)) File.WriteAllText(tunePath, "seed,held_out,mode,arm,cfg,val_worlds,mean_ratio_to_empty,chosen\n");
            var total = Stopwatch.StartNew();
            int done = 0, jobs = heldList.Count * seeds.Length;
            foreach (var held in heldList)
            {
                var train = worlds.Where(w => w.Name != held.Name).ToList();
                foreach (int seed in seeds)
                {
                    var sw = Stopwatch.StartNew();
                    var rng = new Random(seed);
                    var b = RevisionTrainer.Train(train, EvalHarness.TeacherAttempts, EvalHarness.MaxSteps, rng, ngram, true, null);
                    b.Sw.StripWorldShard(held.Name);
                    b.SwNoBoost.StripWorldShard(held.Name);
                    var main = StageBSet.FromBundle(b, ngram);
                    // inner validation worlds: training worlds only
                    var vr = new Random(seed * 1009 + worlds.IndexOf(held));
                    var valWorlds = train.OrderBy(w => vr.Next()).Take(tuneFolds).ToList();
                    var inner = valWorlds.Select(v => StageBSet.Build(b.Recs, v.Name, ngram)).ToList();
                    double trainSec = sw.Elapsed.TotalSeconds;
                    string valNames = string.Join("|", valWorlds.Select(v => v.Name));
                    var rows = new StringBuilder();
                    var tuneRows = new StringBuilder();
                    int k;
                    foreach (var mode in modes)
                    {
                        bool hide = mode == "dh";
                        // ---- tuning on inner validation worlds ----
                        var emptyV = valWorlds.Select(v => Math.Max(1e-9, Eval(v, null, tuneRuns, EvalHarness.MaxSteps, seed + 555, hide, out k).EnergyScore)).ToArray();
                        var tuned = new Dictionary<string, Cfg>();
                        var arms = hide ? BaseArms : BaseArms.Concat(GcArms).ToArray();
                        foreach (var arm in arms)
                        {
                            Cfg best = null; double bestScore = double.PositiveInfinity;
                            var scored = new List<Tuple<Cfg, double>>();
                            foreach (var c in Grid(arm))
                            {
                                double acc = 0;
                                for (int vi = 0; vi < valWorlds.Count; vi++)
                                {
                                    var s = inner[vi]; var cc = c;
                                    acc += Eval(valWorlds[vi], r => SetupArm(r, s, arm, cc), tuneRuns, EvalHarness.MaxSteps, seed + 555, hide, out k).EnergyScore / emptyV[vi];
                                }
                                acc /= valWorlds.Count;
                                scored.Add(Tuple.Create(c, acc));
                                if (acc < bestScore - 1e-12) { bestScore = acc; best = c; }
                            }
                            tuned[arm] = best;
                            foreach (var t in scored)
                                tuneRows.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6:0.#####},{7}", seed, held.Name, mode, arm, t.Item1, valNames, t.Item2, ReferenceEquals(t.Item1, best) ? 1 : 0));
                        }
                        // ---- test on held-out world (paired seeds seed+777) ----
                        var pols = new List<Tuple<string, Func<Robot, int>, string>>();
                        pols.Add(Tuple.Create<string, Func<Robot, int>, string>("SuccessWeighted", r => SetupBrain(r, b.Sw, held, hide, false, false), "prog=step"));
                        pols.Add(Tuple.Create<string, Func<Robot, int>, string>("SW_NoSuccessWeight", r => SetupBrain(r, b.SwNoBoost, held, hide, false, false), "prog=step"));
                        if (!hide)
                        {
                            pols.Add(Tuple.Create<string, Func<Robot, int>, string>("SW_ProgDist", r => SetupBrain(r, b.Sw, held, hide, true, false), "prog=dist"));
                            pols.Add(Tuple.Create<string, Func<Robot, int>, string>("SW_NoSuccessWeight_ProgDist", r => SetupBrain(r, b.SwNoBoost, held, hide, true, false), "prog=dist"));
                            pols.Add(Tuple.Create<string, Func<Robot, int>, string>("SW_NoGoalCtx", r => SetupBrain(r, b.Sw, held, hide, false, true), "prog=step;goal=masked"));
                        }
                        pols.Add(Tuple.Create<string, Func<Robot, int>, string>("EmptyLTM", null, ""));
                        var paper = new Cfg();
                        foreach (var arm in BaseArms) { var a = arm; pols.Add(Tuple.Create<string, Func<Robot, int>, string>(a, r => SetupArm(r, main, a, paper), paper.ToString())); }
                        foreach (var arm in BaseArms) { var a = arm; var c = tuned[a]; pols.Add(Tuple.Create<string, Func<Robot, int>, string>(a + "_Tuned", r => SetupArm(r, main, a, c), c.ToString())); }
                        if (!hide) foreach (var arm in GcArms) { var a = arm; var c = tuned[a]; pols.Add(Tuple.Create<string, Func<Robot, int>, string>(a, r => SetupArm(r, main, a, c), c.ToString())); }
                        foreach (var p in pols)
                        {
                            var es = Stopwatch.StartNew();
                            var t = Eval(held, p.Item2, EvalHarness.EvalRuns, EvalHarness.MaxSteps, seed + 777, hide, out k);
                            rows.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6:0.####},{7:0.####},{8:0.####},{9},{10},{11:0.0},{12:0.0}",
                                seed, held.Name, mode, p.Item1, t.Runs, t.GoalsReached, t.GoalRate, t.AvgEnergy, t.EnergyScore, k, p.Item3, trainSec, es.Elapsed.TotalSeconds));
                        }
                    }
                    File.AppendAllText(rawPath, rows.ToString());
                    File.AppendAllText(tunePath, tuneRows.ToString());
                    done++;
                    double eta = total.Elapsed.TotalMinutes / done * (jobs - done);
                    Console.WriteLine(string.Format("  [{0}/{1}] held={2} seed={3} train={4:0}s total={5:0.0}s ETA {6:0.0} min", done, jobs, held.Name, seed, trainSec, sw.Elapsed.TotalSeconds, eta));
                }
            }
            File.AppendAllText(Path.Combine(outDir, "run_meta.txt"), "finished=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " elapsed_min=" + total.Elapsed.TotalMinutes.ToString("0.0", CultureInfo.InvariantCulture) + "\n");
            Console.WriteLine("DONE stage B in " + total.Elapsed.TotalMinutes.ToString("0.0") + " min -> " + rawPath);
            return 0;
        }
    }
}
