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
    /// R1 revision harness: LifeSim.exe --eval-revision [--seeds N] [--worlds N | --procedural N] [--base-seed S]
    ///   [--ngram L] [--policies a,b,c] [--no-ablations] [--hide-dest] [--world-filter A,B] --out DIR
    /// Same LOO protocol / constants / paired episode seeds as --eval-baseline. Writes a long-format raw CSV
    /// (one row per seed x held-out world x policy) for analysis/revision_stats.py.
    /// Existing --eval / --eval-baseline paths are untouched.
    /// </summary>
    public static class RevisionHarness
    {
        static readonly string[] AllPolicies = { "SuccessWeighted", "TabularNgramBC", "KnnFeel", "EpisodicControl", "TabularQ", "EmptyLTM", "SW_NoSuccessWeight", "SW_DestAnchor" };

        static string Arg(string[] args, string name, string def)
        {
            for (int i = 0; i < args.Length - 1; i++) if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return def;
        }
        static bool Flag(string[] args, string name) { return args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)); }

        public static int Run(string[] args)
        {
            string outDir = Arg(args, "--out", Path.Combine(Environment.CurrentDirectory, "EvalLogs", "revision"));
            int seedsN = int.Parse(Arg(args, "--seeds", "10"));
            int proc = int.Parse(Arg(args, "--procedural", "0"));
            int worldsN = int.Parse(Arg(args, "--worlds", "0"));
            int baseSeed = int.Parse(Arg(args, "--base-seed", "9001"));
            int ngram = int.Parse(Arg(args, "--ngram", "5"));
            bool hide = Flag(args, "--hide-dest");
            bool abl = !Flag(args, "--no-ablations");
            var policies = Arg(args, "--policies", string.Join(",", AllPolicies)).Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (!abl) policies.RemoveAll(p => p.StartsWith("SW_"));
            if (!policies.Contains("EmptyLTM")) policies.Add("EmptyLTM"); // gain reference always present
            string filter = Arg(args, "--world-filter", "");

            if (proc <= 0 && worldsN > WorldLibrary.Primary.Count) proc = worldsN - WorldLibrary.Primary.Count;
            var worlds = proc > 0 ? WorldLibrary.PrimaryPlusProcedural(proc, baseSeed).ToList() : WorldLibrary.Primary.ToList();
            var heldList = worlds;
            if (filter.Length > 0) { var f = new HashSet<string>(filter.Split(',')); heldList = worlds.Where(w => f.Contains(w.Name)).ToList(); }
            int[] seeds = seedsN == EvalHarness.LooSeedsN ? new[] { 101, 202, 303, 404, 505 } : EvalHarness.BuildLooSeedList(seedsN);

            Directory.CreateDirectory(outDir);
            var meta = new StringBuilder();
            meta.AppendLine("cmd=" + string.Join(" ", args));
            meta.AppendLine("seeds=" + string.Join(",", seeds));
            meta.AppendLine("worlds=" + string.Join(",", worlds.Select(w => w.Name)));
            meta.AppendLine("held_out=" + string.Join(",", heldList.Select(w => w.Name)));
            meta.AppendLine("policies=" + string.Join(",", policies));
            meta.AppendLine("ngram_L(new baselines)=" + ngram + " hide_dest=" + hide + " TeacherAttempts=" + EvalHarness.TeacherAttempts + " EvalRuns=" + EvalHarness.EvalRuns + " MaxSteps=" + EvalHarness.MaxSteps);
            meta.AppendLine("started=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (box-local IST)");
            File.WriteAllText(Path.Combine(outDir, "run_meta.txt"), meta.ToString());
            Console.WriteLine(meta);

            string rawPath = Path.Combine(outDir, "revision_raw.csv");
            File.WriteAllText(rawPath, "seed,held_out,policy,ngram_L,hide_dest,runs,goals,goal_rate,avg_energy,energy_score,keys,train_sec,eval_sec\n");
            var total = Stopwatch.StartNew();
            int done = 0, jobs = heldList.Count * seeds.Length;
            foreach (var held in heldList)
            {
                var train = worlds.Where(w => w.Name != held.Name).ToList();
                foreach (int seed in seeds)
                {
                    var sw = Stopwatch.StartNew();
                    var rng = new Random(seed);
                    var b = RevisionTrainer.Train(train, EvalHarness.TeacherAttempts, EvalHarness.MaxSteps, rng, ngram, abl, null);
                    b.Sw.StripWorldShard(held.Name);
                    if (b.SwNoBoost != null) b.SwNoBoost.StripWorldShard(held.Name);
                    if (b.SwDestAnchor != null) b.SwDestAnchor.StripWorldShard(held.Name);
                    double trainSec = sw.Elapsed.TotalSeconds;
                    int pairedBase = seed + 777;
                    var sb = new StringBuilder();
                    foreach (var p in policies)
                    {
                        var es = Stopwatch.StartNew();
                        int keys;
                        var t = Eval(held, p, b, EvalHarness.EvalRuns, EvalHarness.MaxSteps, pairedBase, hide, out keys);
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},{7:0.####},{8:0.####},{9:0.####},{10},{11:0.0},{12:0.0}",
                            seed, held.Name, p, ngram, hide ? 1 : 0, t.Runs, t.GoalsReached, t.GoalRate, t.AvgEnergy, t.EnergyScore, keys, trainSec, es.Elapsed.TotalSeconds));
                    }
                    File.AppendAllText(rawPath, sb.ToString());
                    done++;
                    double eta = total.Elapsed.TotalMinutes / done * (jobs - done);
                    Console.WriteLine(string.Format("  [{0}/{1}] held={2} seed={3} {4:0.0}s  ETA {5:0.0} min", done, jobs, held.Name, seed, sw.Elapsed.TotalSeconds, eta));
                }
            }
            File.AppendAllText(Path.Combine(outDir, "run_meta.txt"), "finished=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " elapsed_min=" + total.Elapsed.TotalMinutes.ToString("0.0", CultureInfo.InvariantCulture) + "\n");
            Console.WriteLine("DONE revision eval in " + total.Elapsed.TotalMinutes.ToString("0.0") + " min -> " + rawPath);
            return 0;
        }

        /// <summary>Mirror of Evaluator.EvalFrozenNamedPolicy (same paired episode seeds, same robot settings).</summary>
        static EvalRunTotals Eval(World world, string policy, RevisionTrainer.Bundle b, int runs, int maxSteps, int pairedBaseSeed, bool hide, out int keys)
        {
            keys = 0;
            var totals = new EvalRunTotals { Runs = runs, Label = policy + " on " + world.Name };
            for (int r = 1; r <= runs; r++)
            {
                int episodeSeed = Evaluator.MixEpisodeSeed(pairedBaseSeed, r);
                var robot = new Robot(world.StartCell, new Random(episodeSeed));
                robot.LearnEnabled = false; robot.RecordSenseTrace = false; robot.ClearBrainOnReset = false;
                robot.HideDestination = hide;
                robot.TabularBC = null; robot.FeelPolicy = null; robot.UseBrain = false;
                BrainMemory brain = null;
                switch (policy)
                {
                    case "SuccessWeighted": brain = b.Sw; break;
                    case "SW_NoSuccessWeight": brain = b.SwNoBoost; break;
                    case "SW_DestAnchor": brain = b.SwDestAnchor; break;
                    case "TabularNgramBC": robot.TabularBC = b.Bc; keys = b.Bc.KeyCount; break;
                    case "KnnFeel": robot.FeelPolicy = b.Knn; keys = b.Knn.KeyCount; break;
                    case "EpisodicControl": robot.FeelPolicy = b.Ec; keys = b.Ec.KeyCount; break;
                    case "TabularQ": robot.FeelPolicy = b.Q; keys = b.Q.KeyCount; break;
                    case "EmptyLTM": break;
                    default: throw new ArgumentException("unknown policy " + policy);
                }
                if (brain != null) { robot.UseBrain = true; robot.SetBrain(brain.CloneFrozenLtmOnly()); keys = brain.LtmCount; }
                else robot.SetBrain(new BrainMemory());

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
    }
}
