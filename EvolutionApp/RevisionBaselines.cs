using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Evolution
{
    /// <summary>Common lookup interface for feel-n-gram policies executed through the Robot's BC action wrapper.</summary>
    public interface IFeelPolicy
    {
        string Name { get; }
        int KeyCount { get; }
        bool TryLookup(IList<byte> recentFeelsOldestFirst, out int relativeMove, out int support, out string matchedKey);
        string StatusLine();
    }

    /// <summary>One teacher transition sequence (success or failure) in feel space, shared by all R1 baselines.</summary>
    public sealed class FeelTrajectory
    {
        public byte[] Feels;   // source-cell feel per step
        public int[] Moves;    // relative move 0..7 per step
        public double[] Costs; // energy cost of the cell entered by each step
        public bool Reached;
        public byte[] Gd;      // R1 Stage B: goal direction relative to facing at the source cell (same fn as SW backfill)
        public byte[] Pb;      // R1 Stage B: progress bin from distance-to-goal (same fn as SW backfill)
    }

    public static class FeelUtil
    {
        public static FeelTrajectory FromAttempt(TeacherPrimitive.AttemptResult att, World world)
        {
            if (att == null || att.Path == null || att.Path.Count == 0) return null;
            int n = att.Path.Count;
            var t = new FeelTrajectory { Feels = new byte[n], Moves = new int[n], Costs = new double[n], Reached = att.ReachedGoal, Gd = new byte[n], Pb = new byte[n] };
            for (int i = 0; i < n; i++)
            {
                var s = att.Path[i];
                t.Feels[i] = (byte)TabularNgramBC.FeelAtSource(world, s.Cell);
                t.Moves[i] = s.MoveDir;
                t.Gd[i] = BrainMemory.ComputeGoalDirection(s.Cell, world.GoalCell, ((s.Facing % 8) + 8) % 8);
                t.Pb[i] = BrainMemory.ProgressBinFromDistance(s.Cell, world.StartCell, world.GoalCell);
                int abs = (((s.Facing + s.MoveDir) % 8) + 8) % 8;
                var nc = new Point(s.Cell.X + BrainMemory.SenseDx[abs], s.Cell.Y + BrainMemory.SenseDy[abs]);
                if (i + 1 < n) nc = att.Path[i + 1].Cell; // exact when available
                t.Costs[i] = world.CellEnergyCost(nc);
            }
            return t;
        }

        /// <summary>Key of the window ending at index end (inclusive), length len.</summary>
        public static string Key(byte[] f, int end, int len)
        {
            var sb = new StringBuilder(len * 2 + 4);
            sb.Append('L').Append(len).Append(':');
            for (int k = end - len + 1; k <= end; k++) { if (k > end - len + 1) sb.Append('-'); sb.Append(f[k]); }
            return sb.ToString();
        }

        public static byte[] Tail(IList<byte> h, int len)
        {
            if (h == null || h.Count < len) return null;
            var a = new byte[len];
            for (int i = 0; i < len; i++) a[i] = h[h.Count - len + i];
            return a;
        }

        public static int Hamming(byte[] a, byte[] b)
        {
            int d = 0;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) d++;
            return d;
        }
    }

    /// <summary>
    /// R1 baseline: Hamming-distance kNN memory over stored feel windows (length L) from the SAME
    /// TeacherSuccess trajectories as SW/BC; copies the vote-weighted next action of the k nearest windows.
    /// Miss (no window within MaxHamming) -> EmptyLTM reactive (same wrapper as TabularNgramBC).
    /// </summary>
    public sealed class KnnFeelMemory : IFeelPolicy
    {
        public const string MechanismName = "KnnFeel";
        public int L = 5, K = 5, MaxHamming = 2;
        readonly Dictionary<string, int> _index = new Dictionary<string, int>();
        readonly List<byte[]> _keys = new List<byte[]>();
        readonly List<int[]> _votes = new List<int[]>();
        public string Name { get { return MechanismName; } }
        public int KeyCount { get { return _keys.Count; } }

        public void Ingest(FeelTrajectory t)
        {
            if (t == null || !t.Reached || t.Feels.Length < L) return;
            for (int e = L - 1; e < t.Feels.Length; e++)
            {
                int mv = t.Moves[e];
                if (mv < 0 || mv > 7) continue;
                string key = FeelUtil.Key(t.Feels, e, L);
                int idx;
                if (!_index.TryGetValue(key, out idx))
                {
                    idx = _keys.Count; _index[key] = idx;
                    var w = new byte[L]; Array.Copy(t.Feels, e - L + 1, w, 0, L);
                    _keys.Add(w); _votes.Add(new int[8]);
                }
                _votes[idx][mv]++;
            }
        }

        public bool TryLookup(IList<byte> hist, out int rel, out int support, out string matchedKey)
        {
            rel = -1; support = 0; matchedKey = null;
            var q = FeelUtil.Tail(hist, L);
            if (q == null || _keys.Count == 0) return false;
            // collect distances, keep K nearest (ties at the K-th distance included)
            var d = new int[_keys.Count];
            for (int i = 0; i < _keys.Count; i++) d[i] = FeelUtil.Hamming(q, _keys[i]);
            var order = Enumerable.Range(0, _keys.Count).OrderBy(i => d[i]).ToList();
            if (d[order[0]] > MaxHamming) return false;
            int cutoff = d[order[Math.Min(K, order.Count) - 1]];
            var score = new double[8];
            foreach (int i in order)
            {
                if (d[i] > cutoff || d[i] > MaxHamming) break;
                double w = 1.0 / (1.0 + d[i]);
                for (int a = 0; a < 8; a++) score[a] += w * _votes[i][a];
                support++;
            }
            int best = 0;
            for (int a = 1; a < 8; a++) if (score[a] > score[best]) best = a;
            if (score[best] <= 0) return false;
            rel = best; matchedKey = "knn:d" + d[order[0]];
            return true;
        }
        public string StatusLine() { return MechanismName + " L=" + L + " K=" + K + " keys=" + _keys.Count; }
    }

    /// <summary>
    /// R1 baseline: Episodic control (Blundell et al. 2016 style) in feel space.
    /// Table (feel window, action) -> best Monte-Carlo return seen (return = -remaining energy; failures get
    /// an extra -FailPenalty). Unseen (s,a): mean of K nearest stored windows (Hamming) for that action.
    /// Uses ALL teacher attempts (success + failure) from the same rollouts.
    /// </summary>
    public sealed class EpisodicControlFeel : IFeelPolicy
    {
        public const string MechanismName = "EpisodicControl";
        public int L = 5, K = 5, MaxHamming = 2;
        public double FailPenalty = 50.0;
        readonly Dictionary<string, int> _index = new Dictionary<string, int>();
        readonly List<byte[]> _keys = new List<byte[]>();
        readonly List<double[]> _q = new List<double[]>(); // NaN = unseen
        public string Name { get { return MechanismName; } }
        public int KeyCount { get { return _keys.Count; } }

        public void Ingest(FeelTrajectory t)
        {
            if (t == null || t.Feels.Length < L) return;
            int n = t.Feels.Length;
            var ret = new double[n];
            double acc = t.Reached ? 0 : -FailPenalty;
            for (int i = n - 1; i >= 0; i--) { acc -= t.Costs[i]; ret[i] = acc; }
            for (int e = L - 1; e < n; e++)
            {
                int mv = t.Moves[e];
                if (mv < 0 || mv > 7) continue;
                string key = FeelUtil.Key(t.Feels, e, L);
                int idx;
                if (!_index.TryGetValue(key, out idx))
                {
                    idx = _keys.Count; _index[key] = idx;
                    var w = new byte[L]; Array.Copy(t.Feels, e - L + 1, w, 0, L);
                    _keys.Add(w);
                    var qa = new double[8]; for (int a = 0; a < 8; a++) qa[a] = double.NaN;
                    _q.Add(qa);
                }
                var row = _q[idx];
                if (double.IsNaN(row[mv]) || ret[e] > row[mv]) row[mv] = ret[e];
            }
        }

        public bool TryLookup(IList<byte> hist, out int rel, out int support, out string matchedKey)
        {
            rel = -1; support = 0; matchedKey = null;
            var q = FeelUtil.Tail(hist, L);
            if (q == null || _keys.Count == 0) return false;
            string key = FeelUtil.Key(q, L - 1, L);
            int exact;
            bool hasExact = _index.TryGetValue(key, out exact);
            var d = new int[_keys.Count];
            for (int i = 0; i < _keys.Count; i++) d[i] = FeelUtil.Hamming(q, _keys[i]);
            var order = Enumerable.Range(0, _keys.Count).Where(i => d[i] <= MaxHamming).OrderBy(i => d[i]).ToList();
            double bestV = double.NegativeInfinity; int bestA = -1;
            for (int a = 0; a < 8; a++)
            {
                double v = double.NaN;
                if (hasExact && !double.IsNaN(_q[exact][a])) v = _q[exact][a];
                else
                {
                    double s = 0; int c = 0;
                    foreach (int i in order)
                    {
                        if (double.IsNaN(_q[i][a])) continue;
                        s += _q[i][a]; c++;
                        if (c >= K) break;
                    }
                    if (c > 0) v = s / c;
                }
                if (!double.IsNaN(v) && v > bestV) { bestV = v; bestA = a; }
            }
            if (bestA < 0) return false;
            rel = bestA; support = 1; matchedKey = hasExact ? "ec:exact" : "ec:knn";
            return true;
        }
        public string StatusLine() { return MechanismName + " L=" + L + " K=" + K + " keys=" + _keys.Count; }
    }

    /// <summary>
    /// R1 baseline: tabular Q-learning on feel n-gram states (state = last L source-cell feels, or shorter at
    /// episode start), reward = -energy of entered cell, terminal 0 at goal / -FailPenalty on timeout.
    /// Trained OFFLINE (batch, repeated sweeps) on ALL teacher transitions from the training worlds
    /// (same rollouts as SW/BC). Test: greedy over actions with a Q estimate; unseen state -> reactive.
    /// </summary>
    public sealed class TabularQFeel : IFeelPolicy
    {
        public const string MechanismName = "TabularQ";
        public int L = 5, Sweeps = 30;
        public double Alpha = 0.2, Gamma = 0.99, FailPenalty = 50.0;
        readonly Dictionary<string, double[]> _q = new Dictionary<string, double[]>();
        readonly List<FeelTrajectory> _data = new List<FeelTrajectory>();
        public string Name { get { return MechanismName; } }
        public int KeyCount { get { return _q.Count; } }

        string StateKey(byte[] f, int i) { int len = Math.Min(L, i + 1); return FeelUtil.Key(f, i, len); }

        double[] Row(string k)
        {
            double[] r;
            if (!_q.TryGetValue(k, out r)) { r = new double[8]; for (int a = 0; a < 8; a++) r[a] = double.NaN; _q[k] = r; }
            return r;
        }
        static double MaxQ(double[] r) { double m = double.NaN; for (int a = 0; a < 8; a++) if (!double.IsNaN(r[a]) && (double.IsNaN(m) || r[a] > m)) m = r[a]; return double.IsNaN(m) ? 0 : m; }

        public void Ingest(FeelTrajectory t) { if (t != null && t.Feels.Length > 0) _data.Add(t); }

        public void Train(Random rng)
        {
            for (int sweep = 0; sweep < Sweeps; sweep++)
            {
                // reverse-order sweeps propagate value faster (standard batch trick)
                for (int ti = 0; ti < _data.Count; ti++)
                {
                    var t = _data[ti];
                    int n = t.Feels.Length;
                    for (int i = n - 1; i >= 0; i--)
                    {
                        int a = t.Moves[i]; if (a < 0 || a > 7) continue;
                        var row = Row(StateKey(t.Feels, i));
                        double target;
                        if (i == n - 1) target = -t.Costs[i] + (t.Reached ? 0 : -FailPenalty);
                        else target = -t.Costs[i] + Gamma * MaxQ(Row(StateKey(t.Feels, i + 1)));
                        if (double.IsNaN(row[a])) row[a] = target; else row[a] += Alpha * (target - row[a]);
                    }
                }
            }
        }

        public bool TryLookup(IList<byte> hist, out int rel, out int support, out string matchedKey)
        {
            rel = -1; support = 0; matchedKey = null;
            if (hist == null || hist.Count == 0) return false;
            int len = Math.Min(L, hist.Count);
            var tail = FeelUtil.Tail(hist, len);
            string k = FeelUtil.Key(tail, len - 1, len);
            double[] r;
            if (!_q.TryGetValue(k, out r)) return false;
            int best = -1;
            for (int a = 0; a < 8; a++) if (!double.IsNaN(r[a]) && (best < 0 || r[a] > r[best])) best = a;
            if (best < 0) return false;
            rel = best; support = 1; matchedKey = k;
            return true;
        }
        public string StatusLine() { return MechanismName + " L=" + L + " states=" + _q.Count + " traj=" + _data.Count; }
    }

    /// <summary>
    /// R1 joint trainer. Consumes the RNG in EXACTLY the same order as Evaluator.TrainSwAndBcFromTeacher, so SW
    /// and BC tables are identical to the frozen paper code for a given seed; the new baselines see the same rollouts.
    /// </summary>
    public static class RevisionTrainer
    {
        public sealed class Bundle
        {
            public BrainMemory Sw;          // paper method
            public BrainMemory SwNoBoost;   // ablation: success-weight boost fixed to 1.0
            public BrainMemory SwDestAnchor;// ablation: feels anchored at destination cell
            public BrainMemory SwShuffled;  // control: SW keys with permuted motor actions
            public TabularNgramBC Bc;
            public KnnFeelMemory Knn;
            public EpisodicControlFeel Ec;
            public TabularQFeel Q;
            public int Successes, Failures;
            public List<StageBRec> Recs = new List<StageBRec>(); // R1 Stage B: all teacher rollouts (for GC baselines + inner-fold tuning)
        }

        public static Bundle Train(IReadOnlyList<World> worlds, int attemptsPerWorld, int maxSteps, Random rng,
            int ngramL, bool doAblations, Action<string> progress)
        {
            var b = new Bundle
            {
                Sw = new BrainMemory(), Bc = new TabularNgramBC(),
                Knn = new KnnFeelMemory { L = ngramL }, Ec = new EpisodicControlFeel { L = ngramL }, Q = new TabularQFeel { L = ngramL }
            };
            b.Sw.ClearPreferredVotes(); b.Bc.Clear();
            if (doAblations)
            {
                b.SwNoBoost = new BrainMemory(); b.SwNoBoost.ClearPreferredVotes();
                b.SwDestAnchor = new BrainMemory(); b.SwDestAnchor.ClearPreferredVotes();
            }
            foreach (var world in worlds)
            {
                if (progress != null) progress("R1 JointTeacher " + world.Name + "...");
                var opt = TeacherPrimitive.DijkstraSuccess(world);
                if (opt.ReachedGoal) Feed(b, opt, world, doAblations);
                for (int a = 0; a < attemptsPerWorld; a++)
                {
                    double temp = 0.25 + rng.NextDouble() * 0.55;
                    var att = TeacherPrimitive.TryReachGoal(world, rng, maxSteps, temp);
                    if (att.ReachedGoal && att.Path != null && att.Path.Count > 0) Feed(b, att, world, doAblations);
                    else
                    {
                        b.Failures++;
                        var ft = FeelUtil.FromAttempt(att, world);
                        b.Ec.Ingest(ft); b.Q.Ingest(ft); // failures only for value-based baselines
                        if (ft != null) b.Recs.Add(new StageBRec { World = world, Traj = ft, Att = null });
                    }
                }
            }
            b.Sw.PopulateLtmFromTeacherSuccess(0.5f, b.Sw.LtmCapacity);
            if (doAblations)
            {
                b.SwNoBoost.PopulateLtmFromTeacherSuccess(0.5f, b.SwNoBoost.LtmCapacity);
                b.SwDestAnchor.PopulateLtmFromTeacherSuccess(0.5f, b.SwDestAnchor.LtmCapacity);
            }
            b.Q.Train(rng == null ? new Random(1) : new Random(rng.Next()));
            return b;
        }

        static void Feed(Bundle b, TeacherPrimitive.AttemptResult att, World world, bool abl)
        {
            b.Successes++;
            IngestSw(b.Sw, att, world, true, false);
            b.Bc.IngestPath(att, world);
            var ft = FeelUtil.FromAttempt(att, world);
            b.Knn.Ingest(ft); b.Ec.Ingest(ft); b.Q.Ingest(ft);
            if (ft != null) b.Recs.Add(new StageBRec { World = world, Traj = ft, Att = att });
            if (abl)
            {
                IngestSw(b.SwNoBoost, att, world, false, false);
                IngestSw(b.SwDestAnchor, att, world, true, true);
            }
        }

        /// <summary>Copy of Evaluator.IngestSuccessIntoSw with two ablation switches.</summary>
        static void IngestSw(BrainMemory brain, TeacherPrimitive.AttemptResult att, World world, bool successWeight, bool destAnchor)
        {
            float boost = 1.0f;
            if (successWeight && att.Steps > 0)
            {
                double avg = att.Energy / att.Steps;
                boost = (float)(0.7 + 0.6 * (1.0 / (1.0 + avg)));
            }
            if (att.Path == null || att.Path.Count == 0) return;
            if (world != null) brain.ConfigureForWorld(world);
            if (att.Path.Count > brain.MaxPathSteps) return;
            var senses = new List<DirCellKind[]>(att.Path.Count);
            var moves = new List<int>(att.Path.Count);
            var feels = new List<FeelCode>(att.Path.Count);
            var cells = new List<Point>(att.Path.Count);
            var facings = new List<byte>(att.Path.Count);
            for (int i = 0; i < att.Path.Count; i++)
            {
                var s = att.Path[i];
                senses.Add(s.Sense); moves.Add(s.MoveDir); cells.Add(s.Cell);
                facings.Add((byte)(((s.Facing % 8) + 8) % 8));
                Point fc = s.Cell;
                if (destAnchor) fc = (i + 1 < att.Path.Count) ? att.Path[i + 1].Cell : world.GoalCell;
                feels.Add(TabularNgramBC.FeelAtSource(world, fc));
            }
            brain.IngestTeacherSuccessTour(senses, moves, feels, cells, facings,
                world != null ? world.StartCell : Point.Empty, world != null ? world.GoalCell : Point.Empty, boost);
        }
    }
}
