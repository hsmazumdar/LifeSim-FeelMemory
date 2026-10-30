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
    /// R1 positive-gain experiments (pre-specified in 05_PositiveGain_Report.md):
    ///   LifeSim.exe --eval-posgain --seed-list 101 --world-filter Terrain-A [--lowdata 2,5,10] --out DIR
    /// Same LOO protocol, training RNG (new Random(seed)) and paired test episode seeds (seed+777) as Stage B.
    /// GC baselines use fixed modal Stage B picks (BC_GC boost 8; kNN_GC boost 8,K 3,Wg 1; feel-only BC boost 8, kNN boost 8,K 3).
    /// Hybrid threshold tau is chosen per fold on 3 inner training worlds only (seed+555 episodes), never on the held-out world.
    /// E1 hybrid (GK, DH); E2 low-data (k training worlds, retrained); E3 goal-signal noise/drop and generator shift.
    /// </summary>
    public static class PosGainHarness
    {
        static string Arg(string[] a, string n, string d) { for (int i = 0; i < a.Length - 1; i++) if (string.Equals(a[i], n, StringComparison.OrdinalIgnoreCase)) return a[i + 1]; return d; }
        static readonly double[] TauGrid = { 0.4, 0.6, 0.8 };
        const double MissTau = 1e-6;

        /// <summary>Generator-shifted twin set: different seeds, 5x5 blocks (vs 4x4), 40 smoothing iters (vs 80), gamma 0.9 (vs 1.35).</summary>
        public static List<World> ShiftWorlds(int n)
        {
            var l = new List<World>();
            for (int i = 0; i < n; i++) l.Add(CanvasWorldGenerator.GenerateTerrain(string.Format("Shift-{0:00}", i + 1), unchecked(7001 + 97 * i), i % 4, 20, 5, 4, 40, 8, 0.9f));
            return l;
        }

        static EvalRunTotals Eval(World world, Func<Robot, int> setup, int runs, int pairedBaseSeed, bool hide, double noiseP, double dropP, out double hybFrac)
        {
            var totals = new EvalRunTotals { Runs = runs, Label = world.Name };
            long hyb = 0, dec = 0;
            for (int r = 1; r <= runs; r++)
            {
                int episodeSeed = Evaluator.MixEpisodeSeed(pairedBaseSeed, r);
                var robot = new Robot(world.StartCell, new Random(episodeSeed));
                robot.LearnEnabled = false; robot.RecordSenseTrace = false; robot.ClearBrainOnReset = false;
                robot.HideDestination = hide;
                robot.TabularBC = null; robot.FeelPolicy = null; robot.UseBrain = false;
                robot.SetBrain(new BrainMemory());
                if (setup != null) setup(robot);
                if (noiseP > 0 || dropP > 0) { robot.NoiseRng = new Random(unchecked(episodeSeed * 31 + 7)); robot.GoalNoiseP = noiseP; robot.GoalDropP = dropP; }
                int ticks = 0, safety = EvalHarness.MaxSteps * 20;
                while (!robot.IsOnGoal && robot.DecisionSteps < EvalHarness.MaxSteps && ticks < safety) { robot.Tick(world); ticks++; }
                if (robot.IsOnGoal) { totals.GoalsReached++; totals.EnergyToGoalSum += robot.TotalEnergy; }
                totals.TotalEnergy += robot.TotalEnergy; totals.DecisionSteps += robot.DecisionSteps;
                hyb += robot.HybridBrainSteps; dec += robot.DecisionSteps;
            }
            hybFrac = dec > 0 ? (double)hyb / dec : 0;
            return totals;
        }

        static Func<Robot, int> Arm(StageBSet s, string arm, StageBHarness.Cfg c) { return r => StageBHarness.SetupArm(r, s, arm, c); }
        static Func<Robot, int> Sw(BrainMemory b, World w, bool hide) { return r => StageBHarness.SetupBrain(r, b, w, hide, false, false); }
        static Func<Robot, int> Hyb(StageBSet s, string arm, StageBHarness.Cfg c, BrainMemory b, World w, bool hide, double tau)
        {
            return r => { int k = StageBHarness.SetupArm(r, s, arm, c); StageBHarness.SetupBrain(r, b, w, hide, false, false); r.HybridBrain = true; r.HybridTau = tau; return k; };
        }

        public static int Run(string[] args)
        {
            string outDir = Arg(args, "--out", "posgain");
            int[] seeds = Arg(args, "--seed-list", "101").Split(',').Select(int.Parse).ToArray();
            var f = new HashSet<string>(Arg(args, "--world-filter", "Terrain-A").Split(','));
            int[] lows = Arg(args, "--lowdata", "2,5,10").Split(',').Where(x => x.Length > 0).Select(int.Parse).ToArray();
            int ngram = 5, tuneRuns = 8, runs = EvalHarness.EvalRuns;
            var worlds = WorldLibrary.PrimaryPlusProcedural(16, 9001).ToList();
            var shift = ShiftWorlds(worlds.Count);
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "run_meta.txt"), "cmd=" + string.Join(" ", args) + "\nstarted=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (local IST)\n");
            string raw = Path.Combine(outDir, "posgain_raw.csv"), tuneP = Path.Combine(outDir, "posgain_tuning.csv");
            File.WriteAllText(raw, "seed,held_out,exp,cond,eval_world,policy,runs,goals,goal_rate,avg_energy,energy_score,hyb_frac,cfg\n");
            File.WriteAllText(tuneP, "seed,held_out,arm,tau,val_worlds,mean_ratio_to_empty,chosen\n");
            var bc = new StageBHarness.Cfg { Boost = 8 };
            var kn = new StageBHarness.Cfg { Boost = 8, K = 3, Wg = 1.0 };
            var total = Stopwatch.StartNew();
            foreach (var held in worlds.Where(w => f.Contains(w.Name)))
            {
                var train = worlds.Where(w => w.Name != held.Name).ToList();
                var sw0 = shift[worlds.IndexOf(held)];
                foreach (int seed in seeds)
                {
                    var sb = new StringBuilder();
                    Action<string, string, World, string, Func<Robot, int>, bool, double, double, string> ev = (exp, cond, w, pol, setup, hide, nP, dP, cfg) =>
                    {
                        double hf; var t = Eval(w, setup, runs, seed + 777, hide, nP, dP, out hf);
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},{7},{8:0.####},{9:0.####},{10:0.####},{11:0.####},{12}", seed, held.Name, exp, cond, w.Name, pol, t.Runs, t.GoalsReached, t.GoalRate, t.AvgEnergy, t.EnergyScore, hf, cfg));
                    };
                    var b = RevisionTrainer.Train(train, EvalHarness.TeacherAttempts, EvalHarness.MaxSteps, new Random(seed), ngram, true, null);
                    b.Sw.StripWorldShard(held.Name); b.SwNoBoost.StripWorldShard(held.Name);
                    var main = StageBSet.FromBundle(b, ngram);
                    // ---- tau tuning on inner training worlds only ----
                    var vr = new Random(seed * 1009 + worlds.IndexOf(held));
                    var val = train.OrderBy(w => vr.Next()).Take(3).ToList();
                    var inner = val.Select(v => StageBSet.Build(b.Recs, v.Name, ngram)).ToList();
                    var innerSw = val.Select(v => b.Sw.CloneWithoutWorld(v.Name)).ToList();
                    double hfx;
                    var emptyV = val.Select(v => Math.Max(1e-9, Eval(v, null, tuneRuns, seed + 555, false, 0, 0, out hfx).EnergyScore)).ToArray();
                    var tau = new Dictionary<string, double>();
                    foreach (var arm in new[] { "TabularNgramBC_GC", "KnnFeel_GC" })
                    {
                        var c = arm == "KnnFeel_GC" ? kn : bc; double best = double.PositiveInfinity, bt = TauGrid[0];
                        var lines = new List<string>();
                        foreach (var tv in TauGrid)
                        {
                            double acc = 0;
                            for (int i = 0; i < val.Count; i++) acc += Eval(val[i], Hyb(inner[i], arm, c, innerSw[i], val[i], false, tv), tuneRuns, seed + 555, false, 0, 0, out hfx).EnergyScore / emptyV[i];
                            acc /= val.Count; lines.Add(tv.ToString(CultureInfo.InvariantCulture) + "," + string.Join("|", val.Select(v => v.Name)) + "," + acc.ToString("0.#####", CultureInfo.InvariantCulture));
                            if (acc < best - 1e-12) { best = acc; bt = tv; }
                        }
                        tau[arm] = bt;
                        foreach (var l in lines) File.AppendAllText(tuneP, seed + "," + held.Name + "," + arm + "," + l + "," + (l.StartsWith(bt.ToString(CultureInfo.InvariantCulture) + ",") ? 1 : 0) + "\n");
                    }
                    double tb = tau["TabularNgramBC_GC"], tk = tau["KnnFeel_GC"];
                    string cb = "tau=" + tb.ToString(CultureInfo.InvariantCulture), ck = "tau=" + tk.ToString(CultureInfo.InvariantCulture);
                    // ---- E1 hybrid GK + E3 noise/drop + E3 shift ----
                    var conds = new[] { Tuple.Create("base", 0.0, 0.0), Tuple.Create("noise25", 0.25, 0.0), Tuple.Create("noise50", 0.5, 0.0), Tuple.Create("drop25", 0.0, 0.25), Tuple.Create("drop50", 0.0, 0.5) };
                    foreach (var cd in conds)
                    {
                        string exp = cd.Item1 == "base" ? "E1" : "E3";
                        double nP = cd.Item2, dP = cd.Item3;
                        ev(exp, cd.Item1, held, "EmptyLTM", null, false, nP, dP, "");
                        ev(exp, cd.Item1, held, "SuccessWeighted", Sw(b.Sw, held, false), false, nP, dP, "");
                        ev(exp, cd.Item1, held, "TabularNgramBC_GC", Arm(main, "TabularNgramBC_GC", bc), false, nP, dP, bc.ToString());
                        ev(exp, cd.Item1, held, "KnnFeel_GC", Arm(main, "KnnFeel_GC", kn), false, nP, dP, kn.ToString());
                        ev(exp, cd.Item1, held, "Hyb_BC_GC_tau", Hyb(main, "TabularNgramBC_GC", bc, b.Sw, held, false, tb), false, nP, dP, cb);
                        ev(exp, cd.Item1, held, "Hyb_kNN_GC_tau", Hyb(main, "KnnFeel_GC", kn, b.Sw, held, false, tk), false, nP, dP, ck);
                        ev(exp, cd.Item1, held, "Hyb_BC_GC_miss", Hyb(main, "TabularNgramBC_GC", bc, b.Sw, held, false, MissTau), false, nP, dP, "miss");
                        ev(exp, cd.Item1, held, "Hyb_kNN_GC_miss", Hyb(main, "KnnFeel_GC", kn, b.Sw, held, false, MissTau), false, nP, dP, "miss");
                        if (cd.Item1 == "base") ev(exp, cd.Item1, held, "SW_NoSuccessWeight", Sw(b.SwNoBoost, held, false), false, 0, 0, "");
                    }
                    // shift: test on the generator-shifted twin (model never saw any Shift world)
                    ev("E3", "shift", sw0, "EmptyLTM", null, false, 0, 0, "");
                    ev("E3", "shift", sw0, "SuccessWeighted", Sw(b.Sw, sw0, false), false, 0, 0, "");
                    ev("E3", "shift", sw0, "SW_NoSuccessWeight", Sw(b.SwNoBoost, sw0, false), false, 0, 0, "");
                    ev("E3", "shift", sw0, "TabularNgramBC_GC", Arm(main, "TabularNgramBC_GC", bc), false, 0, 0, bc.ToString());
                    ev("E3", "shift", sw0, "KnnFeel_GC", Arm(main, "KnnFeel_GC", kn), false, 0, 0, kn.ToString());
                    ev("E3", "shift", sw0, "Hyb_BC_GC_tau", Hyb(main, "TabularNgramBC_GC", bc, b.Sw, sw0, false, tb), false, 0, 0, cb);
                    ev("E3", "shift", sw0, "Hyb_kNN_GC_tau", Hyb(main, "KnnFeel_GC", kn, b.Sw, sw0, false, tk), false, 0, 0, ck);
                    ev("E3", "shift", sw0, "Hyb_BC_GC_miss", Hyb(main, "TabularNgramBC_GC", bc, b.Sw, sw0, false, MissTau), false, 0, 0, "miss");
                    ev("E3", "shift", sw0, "Hyb_kNN_GC_miss", Hyb(main, "KnnFeel_GC", kn, b.Sw, sw0, false, MissTau), false, 0, 0, "miss");
                    // ---- E1 DH: feel-only primary + SW on primary miss ----
                    var bcF = new StageBHarness.Cfg { Boost = 8 }; var knF = new StageBHarness.Cfg { Boost = 8, K = 3 };
                    ev("E1", "dh", held, "EmptyLTM", null, true, 0, 0, "");
                    ev("E1", "dh", held, "SuccessWeighted", Sw(b.Sw, held, true), true, 0, 0, "");
                    ev("E1", "dh", held, "TabularNgramBC", Arm(main, "TabularNgramBC", bcF), true, 0, 0, bcF.ToString());
                    ev("E1", "dh", held, "KnnFeel", Arm(main, "KnnFeel", knF), true, 0, 0, knF.ToString());
                    ev("E1", "dh", held, "HybDH_BC_miss", Hyb(main, "TabularNgramBC", bcF, b.Sw, held, true, MissTau), true, 0, 0, "miss");
                    ev("E1", "dh", held, "HybDH_kNN_miss", Hyb(main, "KnnFeel", knF, b.Sw, held, true, MissTau), true, 0, 0, "miss");
                    File.AppendAllText(raw, sb.ToString()); sb.Clear();
                    Console.WriteLine("held=" + held.Name + " seed=" + seed + " main done t=" + total.Elapsed.TotalMinutes.ToString("0.0"));
                    // ---- E2 low-data: retrain on k random training worlds ----
                    foreach (int k in lows)
                    {
                        var lr = new Random(seed * 7 + k * 1013 + worlds.IndexOf(held));
                        var sub = train.OrderBy(w => lr.Next()).Take(k).ToList();
                        var bk = RevisionTrainer.Train(sub, EvalHarness.TeacherAttempts, EvalHarness.MaxSteps, new Random(seed * 13 + k), ngram, true, null);
                        var sk = StageBSet.FromBundle(bk, ngram);
                        string cond = "k" + k, cs = "train=" + string.Join("|", sub.Select(w => w.Name));
                        ev("E2", cond, held, "EmptyLTM", null, false, 0, 0, cs);
                        ev("E2", cond, held, "SuccessWeighted", Sw(bk.Sw, held, false), false, 0, 0, cs);
                        ev("E2", cond, held, "SW_NoSuccessWeight", Sw(bk.SwNoBoost, held, false), false, 0, 0, cs);
                        ev("E2", cond, held, "TabularNgramBC_GC", Arm(sk, "TabularNgramBC_GC", bc), false, 0, 0, cs);
                        ev("E2", cond, held, "KnnFeel_GC", Arm(sk, "KnnFeel_GC", kn), false, 0, 0, cs);
                        ev("E2", cond, held, "Hyb_BC_GC_miss", Hyb(sk, "TabularNgramBC_GC", bc, bk.Sw, held, false, MissTau), false, 0, 0, cs);
                        ev("E2", cond, held, "Hyb_kNN_GC_miss", Hyb(sk, "KnnFeel_GC", kn, bk.Sw, held, false, MissTau), false, 0, 0, cs);
                        File.AppendAllText(raw, sb.ToString()); sb.Clear();
                        Console.WriteLine("held=" + held.Name + " seed=" + seed + " k=" + k + " t=" + total.Elapsed.TotalMinutes.ToString("0.0"));
                    }
                }
            }
            File.AppendAllText(Path.Combine(outDir, "run_meta.txt"), "finished=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " elapsed_min=" + total.Elapsed.TotalMinutes.ToString("0.0", CultureInfo.InvariantCulture) + "\n");
            return 0;
        }
    }
}
