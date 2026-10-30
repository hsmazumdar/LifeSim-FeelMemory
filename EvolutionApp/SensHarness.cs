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
    /// R1 sensitivity/capacity sweep (GK mode only, same LOO protocol and paired test seeds seed+777 as Stage B):
    ///   LifeSim.exe --eval-sens --seed-list 101 --world-filter Terrain-A [--dirw 0,0.5,1,2.5,5] [--caps 64,128,256,512,1024] --out DIR
    /// Sweep "dirw": SW goal-direction weight (paper 2.5) at LTM cap 512.
    /// Sweep "cap": LTM capacity for SW (retrained per cap) and the same key budget for the GC BC / GC kNN baselines
    /// (first-come key cap, GoalCtxUtil.MaxKeys). GC configs fixed to the modal Stage B tuning picks
    /// (BC_GC boost=8; KnnFeel_GC boost=8,K=3,Wg=1), so no tuning here. Uncapped GC baselines also reported per cap row as reference.
    /// </summary>
    public static class SensHarness
    {
        static string Arg(string[] a, string n, string d) { for (int i = 0; i < a.Length - 1; i++) if (string.Equals(a[i], n, StringComparison.OrdinalIgnoreCase)) return a[i + 1]; return d; }

        public static int Run(string[] args)
        {
            string outDir = Arg(args, "--out", "sens");
            int[] seeds = Arg(args, "--seed-list", "101").Split(',').Select(int.Parse).ToArray();
            string filter = Arg(args, "--world-filter", "Terrain-A");
            double[] dirw = Arg(args, "--dirw", "0,0.5,1,2.5,5").Split(',').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
            int[] caps = Arg(args, "--caps", "64,128,256,512,1024").Split(',').Select(int.Parse).ToArray();
            int ngram = 5;
            var worlds = WorldLibrary.PrimaryPlusProcedural(16, 9001).ToList();
            var f = new HashSet<string>(filter.Split(','));
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "run_meta.txt"), "cmd=" + string.Join(" ", args) + "\nstarted=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (local IST)\n");
            string raw = Path.Combine(outDir, "sens_raw.csv");
            if (!File.Exists(raw)) File.WriteAllText(raw, "seed,held_out,sweep,value,policy,runs,goals,goal_rate,avg_energy,energy_score,keys\n");
            var bc = new StageBHarness.Cfg { Boost = 8 };
            var kn = new StageBHarness.Cfg { Boost = 8, K = 3, Wg = 1.0 };
            var total = Stopwatch.StartNew();
            foreach (var held in worlds.Where(w => f.Contains(w.Name)))
            {
                var train = worlds.Where(w => w.Name != held.Name).ToList();
                foreach (int seed in seeds)
                {
                    var sb = new StringBuilder();
                    int k;
                    Action<string, string, string, Func<Robot, int>> ev = (sweep, val, pol, setup) =>
                    {
                        var t = StageBHarness.Eval(held, setup, EvalHarness.EvalRuns, EvalHarness.MaxSteps, seed + 777, false, out k);
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},{7:0.####},{8:0.####},{9:0.####},{10}", seed, held.Name, sweep, val, pol, t.Runs, t.GoalsReached, t.GoalRate, t.AvgEnergy, t.EnergyScore, k));
                    };
                    ev("ref", "-", "EmptyLTM", null);
                    foreach (int cap in caps)
                    {
                        RevisionTrainer.LtmCap = cap;
                        BrainMemory.GoalDirWeight = 2.5;
                        GoalCtxUtil.MaxKeys = int.MaxValue;
                        var b = RevisionTrainer.Train(train, EvalHarness.TeacherAttempts, EvalHarness.MaxSteps, new Random(seed), ngram, true, null);
                        b.Sw.StripWorldShard(held.Name); b.SwNoBoost.StripWorldShard(held.Name);
                        string cv = cap.ToString(CultureInfo.InvariantCulture);
                        ev("cap", cv, "SuccessWeighted", r => StageBHarness.SetupBrain(r, b.Sw, held, false, false, false));
                        ev("cap", cv, "SW_NoSuccessWeight", r => StageBHarness.SetupBrain(r, b.SwNoBoost, held, false, false, false));
                        GoalCtxUtil.MaxKeys = cap;
                        var capped = StageBSet.FromBundle(b, ngram);
                        GoalCtxUtil.MaxKeys = int.MaxValue;
                        ev("cap", cv, "TabularNgramBC_GC_capped", r => StageBHarness.SetupArm(r, capped, "TabularNgramBC_GC", bc));
                        ev("cap", cv, "KnnFeel_GC_capped", r => StageBHarness.SetupArm(r, capped, "KnnFeel_GC", kn));
                        if (cap == 512)
                        {
                            var full = StageBSet.FromBundle(b, ngram);
                            ev("cap", cv, "TabularNgramBC_GC_uncapped", r => StageBHarness.SetupArm(r, full, "TabularNgramBC_GC", bc));
                            ev("cap", cv, "KnnFeel_GC_uncapped", r => StageBHarness.SetupArm(r, full, "KnnFeel_GC", kn));
                            foreach (var w in dirw)
                            {
                                BrainMemory.GoalDirWeight = w;
                                string wv = w.ToString(CultureInfo.InvariantCulture);
                                ev("dirw", wv, "SuccessWeighted", r => StageBHarness.SetupBrain(r, b.Sw, held, false, false, false));
                                ev("dirw", wv, "SW_NoSuccessWeight", r => StageBHarness.SetupBrain(r, b.SwNoBoost, held, false, false, false));
                            }
                            BrainMemory.GoalDirWeight = 2.5;
                        }
                        File.AppendAllText(raw, sb.ToString()); sb.Clear();
                        Console.WriteLine("held=" + held.Name + " seed=" + seed + " cap=" + cap + " t=" + total.Elapsed.TotalMinutes.ToString("0.0") + "min");
                    }
                }
            }
            File.AppendAllText(Path.Combine(outDir, "run_meta.txt"), "finished=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " elapsed_min=" + total.Elapsed.TotalMinutes.ToString("0.0", CultureInfo.InvariantCulture) + "\n");
            return 0;
        }
    }
}
