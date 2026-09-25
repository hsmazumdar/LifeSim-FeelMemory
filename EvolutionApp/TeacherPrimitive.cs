using System;
using System.Collections.Generic;
using System.Drawing;

namespace Evolution
{
    /// <summary>
    /// V16 teacher / data-generating algorithm. Destination is KNOWN.
    /// Many noisy goal-seeking attempts; ONLY successful trajectories
    /// contribute feel-atlas n-gram units (TeacherSuccess→LTM). Failures discarded.
    /// </summary>
    public static class TeacherPrimitive
    {
        public struct Step
        {
            public Point Cell;
            public int MoveDir; // relative-to-facing 0..7
            public DirCellKind[] Sense; // relative-to-facing
            public int Facing;
        }

        public sealed class AttemptResult
        {
            public bool ReachedGoal;
            public double Energy;
            public int Steps;
            public List<Step> Path = new List<Step>(64);
        }

        public static AttemptResult TryReachGoal(World world, Random rng, int maxSteps, double noiseTemp = 0.35)
        {
            if (rng == null) rng = new Random();
            var result = new AttemptResult();
            var cell = world.StartCell;
            int facing = FaceIndex(cell, world.GoalCell);
            double energy = 0;
            var visited = new Dictionary<long, int>();

            for (int step = 0; step < maxSteps; step++)
            {
                if (cell == world.GoalCell)
                {
                    result.ReachedGoal = true;
                    result.Energy = energy;
                    result.Steps = step;
                    return result;
                }

                var senseAbs = BrainMemory.Sense8(world, cell);
                var sense = BrainMemory.RotateSense(senseAbs, facing);

                double[] w = new double[8];
                double sum = 0;
                int legal = 0;
                for (int d = 0; d < 8; d++)
                {
                    int nx = cell.X + BrainMemory.SenseDx[d];
                    int ny = cell.Y + BrainMemory.SenseDy[d];
                    if (nx < 0 || ny < 0 || nx >= world.Cols || ny >= world.Rows)
                    { w[d] = 0; continue; }
                    var np = new Point(nx, ny);
                    if (world.CellHasObstacle(np)) { w[d] = 0.001; continue; }
                    legal++;
                    float rough = world.CellRoughness(np);
                    double cost = 0.2 + 0.8 * rough;
                    int gdx = world.GoalCell.X - nx;
                    int gdy = world.GoalCell.Y - ny;
                    double dist = Math.Sqrt(gdx * gdx + gdy * gdy);
                    int curGdx = world.GoalCell.X - cell.X;
                    int curGdy = world.GoalCell.Y - cell.Y;
                    double curDist = Math.Sqrt(curGdx * curGdx + curGdy * curGdy);
                    double progress = curDist - dist;
                    double score = progress * 2.5 - cost * 1.8;
                    int rel = (d - facing + 8) % 8;
                    if (rel == 0) score += 0.25;
                    else if (rel == 4) score -= 0.35;
                    long key = ((long)nx << 32) ^ (uint)ny;
                    int seen;
                    if (visited.TryGetValue(key, out seen))
                        score -= 0.4 * Math.Min(seen, 5);
                    double ww = Math.Exp(score / Math.Max(0.05, noiseTemp));
                    if (ww < 1e-6) ww = 1e-6;
                    w[d] = ww;
                    sum += ww;
                }
                if (legal == 0 || sum <= 0) break;

                double roll = rng.NextDouble() * sum;
                double acc = 0;
                int chosen = -1;
                for (int d = 0; d < 8; d++)
                {
                    if (w[d] <= 0) continue;
                    acc += w[d];
                    if (roll <= acc) { chosen = d; break; }
                }
                if (chosen < 0)
                {
                    for (int d = 0; d < 8; d++) if (w[d] > 0) chosen = d;
                }
                if (chosen < 0) break;

                result.Path.Add(new Step
                {
                    Cell = cell,
                    MoveDir = BrainMemory.RelativeMoveFromAbsolute(facing, chosen),
                    Sense = sense,
                    Facing = facing
                });

                cell = new Point(cell.X + BrainMemory.SenseDx[chosen], cell.Y + BrainMemory.SenseDy[chosen]);
                energy += world.CellEnergyCost(cell);
                facing = chosen;
                long vk = ((long)cell.X << 32) ^ (uint)cell.Y;
                int vc;
                visited.TryGetValue(vk, out vc);
                visited[vk] = vc + 1;
            }

            result.ReachedGoal = cell == world.GoalCell;
            result.Energy = energy;
            result.Steps = result.Path.Count;
            return result;
        }

        public static AttemptResult DijkstraSuccess(World world)
        {
            var result = new AttemptResult();
            int cols = world.Cols, rows = world.Rows;
            var dist = new double[cols, rows];
            var prevDir = new int[cols, rows];
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                { dist[x, y] = double.MaxValue; prevDir[x, y] = -1; }

            var start = world.StartCell;
            var goal = world.GoalCell;
            dist[start.X, start.Y] = 0;
            var open = new List<Point> { start };
            int[] dx = BrainMemory.SenseDx;
            int[] dy = BrainMemory.SenseDy;

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
                    if (world.CellHasObstacle(nx, ny)) continue;
                    double nd = dist[cur.X, cur.Y] + world.CellEnergyCost(nx, ny);
                    if (nd < dist[nx, ny])
                    {
                        bool wasInf = dist[nx, ny] >= double.MaxValue / 2;
                        dist[nx, ny] = nd;
                        prevDir[nx, ny] = d;
                        if (wasInf) open.Add(new Point(nx, ny));
                    }
                }
            }

            if (dist[goal.X, goal.Y] >= double.MaxValue / 2)
                return result;

            var cells = new List<Point>();
            var enterDir = new List<int>();
            var c = goal;
            cells.Add(c);
            while (!(c.X == start.X && c.Y == start.Y))
            {
                int dEnter = prevDir[c.X, c.Y];
                if (dEnter < 0) break;
                enterDir.Add(dEnter);
                c = new Point(c.X - dx[dEnter], c.Y - dy[dEnter]);
                cells.Add(c);
                if (cells.Count > cols * rows + 2) break;
            }
            cells.Reverse();
            enterDir.Reverse();

            int facing = FaceIndex(start, goal);
            double energy = 0;
            for (int i = 0; i < cells.Count - 1; i++)
            {
                var cell = cells[i];
                int absMove = enterDir[i];
                var senseAbs = BrainMemory.Sense8(world, cell);
                var sense = BrainMemory.RotateSense(senseAbs, facing);
                result.Path.Add(new Step
                {
                    Cell = cell,
                    MoveDir = BrainMemory.RelativeMoveFromAbsolute(facing, absMove),
                    Sense = sense,
                    Facing = facing
                });
                facing = absMove;
                energy += world.CellEnergyCost(cells[i + 1]);
            }
            result.ReachedGoal = true;
            result.Energy = energy;
            result.Steps = result.Path.Count;
            return result;
        }

        public static TeacherPopulateStats PopulateBrainFromSuccesses(
            BrainMemory brain,
            IReadOnlyList<World> worlds,
            int attemptsPerWorld,
            int maxSteps,
            Random rng,
            Action<string> progress = null)
        {
            if (brain == null) throw new ArgumentNullException("brain");
            if (rng == null) rng = new Random();
            if (attemptsPerWorld < 1) attemptsPerWorld = 200;
            var stats = new TeacherPopulateStats();

            brain.ClearPreferredVotes();

            foreach (var world in worlds)
            {
                if (progress != null) progress("TeacherSuccess " + world.Name + "...");
                int successes = 0;
                int failures = 0;

                var opt = DijkstraSuccess(world);
                if (opt.ReachedGoal)
                {
                    IngestPath(brain, opt, world);
                    successes++;
                    stats.SuccessPaths++;
                    stats.SuccessSteps += opt.Path.Count;
                }

                for (int a = 0; a < attemptsPerWorld; a++)
                {
                    double temp = 0.25 + rng.NextDouble() * 0.55;
                    var att = TryReachGoal(world, rng, maxSteps, temp);
                    if (att.ReachedGoal && att.Path != null && att.Path.Count > 0)
                    {
                        IngestPath(brain, att, world);
                        successes++;
                        stats.SuccessPaths++;
                        stats.SuccessSteps += att.Path.Count;
                    }
                    else
                    {
                        failures++;
                        stats.FailedAttempts++;
                    }
                }
                stats.WorldSuccesses[world.Name] = successes;
                stats.WorldFailures[world.Name] = failures;
            }

            stats.RulesWritten = brain.PopulateLtmFromTeacherSuccess(0.5f, brain.LtmCapacity);
            stats.PreferredSignatures = brain.PreferredVoteSignatureCount;
            stats.LtmCount = brain.LtmCount;
            return stats;
        }

        static void IngestPath(BrainMemory brain, AttemptResult att, World world)
        {
            float boost = 1.0f;
            if (att.Steps > 0)
            {
                double avg = att.Energy / att.Steps;
                boost = (float)(0.7 + 0.6 * (1.0 / (1.0 + avg)));
            }
            if (att.Path == null || att.Path.Count == 0) return;
            if (world != null) brain.ConfigureForWorld(world);
            if (att.Path.Count > brain.MaxPathSteps) return;

            var senses = new System.Collections.Generic.List<DirCellKind[]>(att.Path.Count);
            var moves = new System.Collections.Generic.List<int>(att.Path.Count);
            var feels = new System.Collections.Generic.List<FeelCode>(att.Path.Count);
            var cells = new System.Collections.Generic.List<Point>(att.Path.Count);
            var facings = new System.Collections.Generic.List<byte>(att.Path.Count);
            // V16a ISO: REVERT Fix C — teacher feel/cell = SOURCE cell (pre-move / s.Cell), as V15.
            // Keep all other V16 fixes (persistence, progressBin=newest, success-only, observe+loose).
            for (int i = 0; i < att.Path.Count; i++)
            {
                var s = att.Path[i];
                senses.Add(s.Sense);
                moves.Add(s.MoveDir);
                cells.Add(s.Cell);
                facings.Add((byte)(((s.Facing % 8) + 8) % 8));
                FeelCode feel = FeelCode.Medium;
                if (world != null)
                {
                    if (world.CellHasObstacle(s.Cell)) feel = FeelCode.Obstacle;
                    else
                    {
                        float r = world.CellRoughness(s.Cell);
                        if (r <= MotorMemory.SmoothThreshold) feel = FeelCode.Smooth;
                        else if (r >= MotorMemory.RoughThreshold) feel = FeelCode.Rough;
                        else feel = FeelCode.Medium;
                    }
                }
                feels.Add(feel);
            }
            brain.IngestTeacherSuccessTour(senses, moves, feels, cells, facings,
                world != null ? world.StartCell : Point.Empty,
                world != null ? world.GoalCell : Point.Empty,
                boost);
        }

        static int FaceIndex(Point from, Point to)
        {
            int sx = Math.Sign(to.X - from.X);
            int sy = Math.Sign(to.Y - from.Y);
            for (int i = 0; i < 8; i++)
                if (BrainMemory.SenseDx[i] == sx && BrainMemory.SenseDy[i] == sy)
                    return i;
            return 0;
        }
    }

    public sealed class TeacherPopulateStats
    {
        public int SuccessPaths;
        public int SuccessSteps;
        public int FailedAttempts;
        public int RulesWritten;
        public int PreferredSignatures;
        public int LtmCount;
        public readonly Dictionary<string, int> WorldSuccesses = new Dictionary<string, int>();
        public readonly Dictionary<string, int> WorldFailures = new Dictionary<string, int>();
    }
}
