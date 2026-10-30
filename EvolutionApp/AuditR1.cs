using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Evolution
{
    /// <summary>R1 Stage A leakage audit: LifeSim.exe --audit-r1 [--seed S] --out DIR. Exit code 0 = all checks pass.</summary>
    public static class AuditR1
    {
        static int _fail;
        static StringBuilder _log = new StringBuilder();
        static void Check(bool ok, string what) { _log.AppendLine((ok ? "PASS " : "FAIL ") + what); if (!ok) _fail++; Console.WriteLine((ok ? "PASS " : "FAIL ") + what); }

        static World MoveGoal(World w, Point g)
        {
            var r = new float[w.Cols, w.Rows]; var obs = new List<Rectangle>();
            for (int x = 0; x < w.Cols; x++) for (int y = 0; y < w.Rows; y++)
            { var p = new Point(x, y); r[x, y] = w.CellRoughness(p); if (w.CellHasObstacle(p)) obs.Add(new Rectangle(x, y, 1, 1)); }
            return new World(w.Name, w.Cols, w.Rows, w.StartCell, g, r, obs);
        }

        static List<Point> Run(World w, BrainMemory brain, bool hide, int seed, int steps, out int stmAtStart, out int ltmBefore, out int ltmAfter, out float strBefore, out float strAfter)
        {
            var robot = new Robot(w.StartCell, new Random(seed));
            robot.LearnEnabled = false; robot.RecordSenseTrace = false; robot.ClearBrainOnReset = false; robot.HideDestination = hide;
            robot.UseBrain = true;
            var clone = brain.CloneFrozenLtmOnly();
            stmAtStart = clone.StmCount; ltmBefore = clone.LtmCount; strBefore = clone.LtmStrengthSum();
            robot.SetBrain(clone);
            var path = new List<Point>();
            int ticks = 0;
            while (!robot.IsOnGoal && robot.DecisionSteps < steps && ticks < steps * 20) { robot.Tick(w); ticks++; path.Add(robot.Cell); }
            robot.NotifyEpisodeEnd(robot.IsOnGoal, w); // must be a no-op when LearnEnabled=false
            ltmAfter = clone.LtmCount; strAfter = clone.LtmStrengthSum();
            return path;
        }

        public static int Run(string[] args)
        {
            string outDir = "audit_r1"; int seed = 101;
            for (int i = 0; i < args.Length - 1; i++) { if (args[i] == "--out") outDir = args[i + 1]; if (args[i] == "--seed") seed = int.Parse(args[i + 1]); }
            Directory.CreateDirectory(outDir);
            var worlds = WorldLibrary.Primary.ToList();
            foreach (var held in worlds)
            {
                var train = worlds.Where(w => w.Name != held.Name).ToList();
                Check(!train.Any(w => w.Name == held.Name), held.Name + ": held-out world not in training list");
                var b = RevisionTrainer.Train(train, EvalHarness.TeacherAttempts, EvalHarness.MaxSteps, new Random(seed), 5, false, null);
                int heldShard = b.Sw.LtmCountForWorld(held.Name);
                Check(heldShard == 0, held.Name + ": LTM entries tagged with held-out WorldId BEFORE strip = " + heldShard);
                var ids = b.Sw.LtmWorldIds();
                _log.AppendLine("  LTM world tags: " + string.Join(",", ids.Select(kv => kv.Key + "=" + kv.Value)));
                Check(!ids.ContainsKey(held.Name), held.Name + ": no held-out WorldId among LTM tags");

                int s0, lb, la; float sb, sa;
                var p1 = Run(held, b.Sw, false, seed + 777, 2000, out s0, out lb, out la, out sb, out sa);
                Check(s0 == 0, held.Name + ": STM empty at episode start (StmCount=" + s0 + ")");
                Check(lb == la && Math.Abs(sb - sa) < 1e-4, held.Name + ": LTM frozen during test (count " + lb + "->" + la + ", strength " + sb + "->" + sa + ")");
                // second episode on same frozen source brain must again start with empty STM and same LTM
                int s0b, lb2, la2; float sb2, sa2;
                Run(held, b.Sw, false, seed + 778, 2000, out s0b, out lb2, out la2, out sb2, out sa2);
                Check(s0b == 0 && lb2 == lb && Math.Abs(sb2 - sb) < 1e-4, held.Name + ": next episode starts clean (STM=" + s0b + ", LTM unchanged)");

                // dest-hidden: behaviour must not depend on goal location. Move goal to far corner, compare trajectories
                // up to the first time either run touches either goal cell.
                var g2 = new Point(held.Cols - 1 - held.GoalCell.X, held.Rows - 1 - held.GoalCell.Y);
                if (g2 == held.StartCell || held.CellHasObstacle(g2)) g2 = new Point(0, 0);
                var w2 = MoveGoal(held, g2);
                int x1, x2, x3, x4; float f1, f2;
                var h1 = Run(held, b.Sw, true, seed + 777, 300, out x1, out x2, out x3, out f1, out f2);
                var h2 = Run(w2, b.Sw, true, seed + 777, 300, out x1, out x2, out x3, out f1, out f2);
                int n = 0, cut = Math.Min(h1.Count, h2.Count);
                for (; n < cut; n++) { if (h1[n] != h2[n]) break; if (h1[n] == held.GoalCell || h1[n] == g2) { n = cut; break; } }
                Check(n == cut, held.Name + ": dest-hidden SW trajectory identical when goal is moved (" + n + "/" + cut + " steps match)");
                var k1 = Run(held, b.Sw, false, seed + 777, 300, out x1, out x2, out x3, out f1, out f2);
                var k2 = Run(w2, b.Sw, false, seed + 777, 300, out x1, out x2, out x3, out f1, out f2);
                int m = 0; for (; m < Math.Min(k1.Count, k2.Count); m++) if (k1[m] != k2[m]) break;
                _log.AppendLine("  control: goal-known trajectories diverge at step " + m + " (expected early divergence)");
            }
            _log.AppendLine(_fail == 0 ? "ALL CHECKS PASSED" : (_fail + " CHECK(S) FAILED"));
            File.WriteAllText(Path.Combine(outDir, "audit_r1.txt"), _log.ToString());
            Console.WriteLine(_fail == 0 ? "ALL CHECKS PASSED" : (_fail + " FAILED"));
            return _fail == 0 ? 0 : 1;
        }
    }
}
