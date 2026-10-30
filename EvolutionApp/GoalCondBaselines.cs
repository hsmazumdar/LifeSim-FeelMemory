using System;
using System.Collections.Generic;
using System.Linq;

namespace Evolution
{
    /// <summary>R1 Stage B: policies that also receive the goal context SW retrieval uses (goal direction relative to facing, progress bin).</summary>
    public interface IGoalCtxPolicy { void SetContext(int goalDir, int progBin); }
    /// <summary>R1 pos-gain: vote share of the chosen action on a goal-conditioned hit (0 when the GC part missed).</summary>
    public interface IConfPolicy { double LastConf { get; } }

    public sealed class StageBRec
    {
        public World World;
        public FeelTrajectory Traj;
        public TeacherPrimitive.AttemptResult Att; // null for failed rollouts
    }

    public static class GoalCtxUtil
    {
        public static int CircDir(int a, int b) { int d = Math.Abs(a - b) % 8; return d > 4 ? 8 - d : d; }
        public static string Ctx(int gd, int pb) { return "|g" + gd + "p" + pb; }
        /// <summary>R1 capacity sweep: max stored keys for GC BC / kNN / EC (first-come; default unlimited).</summary>
        public static int MaxKeys = int.MaxValue;
    }

    /// <summary>Goal-conditioned tabular n-gram BC: key = feel n-gram (L=5..3, longest first) + goal dir + progress bin.
    /// Context miss or unknown context (DH) -> feel-only TabularNgramBC fallback.</summary>
    public sealed class GcTabularBC : IFeelPolicy, IGoalCtxPolicy, IConfPolicy
    {
        public double LastConf { get; private set; }
        public const string MechanismName = "TabularNgramBC_GC";
        public IFeelPolicy Fallback;
        readonly Dictionary<string, int[]> _v = new Dictionary<string, int[]>();
        int _gd = -1, _pb = -1;
        public string Name { get { return MechanismName; } }
        public int KeyCount { get { return _v.Count; } }
        public void SetContext(int g, int p) { _gd = g; _pb = p; }
        public void Ingest(FeelTrajectory t)
        {
            if (t == null || !t.Reached) return;
            for (int e = 0; e < t.Feels.Length; e++)
            {
                int mv = t.Moves[e]; if (mv < 0 || mv > 7) continue;
                for (int len = TabularNgramBC.MinNgram; len <= TabularNgramBC.MaxNgram; len++)
                {
                    if (e - len + 1 < 0) break;
                    string k = FeelUtil.Key(t.Feels, e, len) + GoalCtxUtil.Ctx(t.Gd[e], t.Pb[e]);
                    int[] r; if (!_v.TryGetValue(k, out r)) { if (_v.Count >= GoalCtxUtil.MaxKeys) continue; r = new int[8]; _v[k] = r; }
                    r[mv]++;
                }
            }
        }
        public bool TryLookup(IList<byte> h, out int rel, out int support, out string mk)
        {
            rel = -1; support = 0; mk = null; LastConf = 0;
            if (_gd >= 0 && h != null)
            {
                for (int len = Math.Min(TabularNgramBC.MaxNgram, h.Count); len >= TabularNgramBC.MinNgram; len--)
                {
                    var tail = FeelUtil.Tail(h, len);
                    string k = FeelUtil.Key(tail, len - 1, len) + GoalCtxUtil.Ctx(_gd, _pb);
                    int[] r;
                    if (_v.TryGetValue(k, out r))
                    {
                        int best = 0; for (int a = 1; a < 8; a++) if (r[a] > r[best]) best = a;
                        if (r[best] > 0) { int tot = 0; for (int a = 0; a < 8; a++) tot += r[a]; LastConf = (double)r[best] / tot; rel = best; support = r[best]; mk = k; return true; }
                    }
                }
            }
            return Fallback != null && Fallback.TryLookup(h, out rel, out support, out mk);
        }
        public string StatusLine() { return MechanismName + " keys=" + _v.Count; }
    }

    /// <summary>Goal-conditioned kNN: items = (feel window L, goal dir, progress bin); distance = Hamming + Wg*circDirDist + Wp*|dProg|
    /// (Hamming still capped at MaxHamming). Unknown context -> feel-only kNN fallback.</summary>
    public sealed class GcKnn : IFeelPolicy, IGoalCtxPolicy, IConfPolicy
    {
        public double LastConf { get; private set; }
        public const string MechanismName = "KnnFeel_GC";
        public int L = 5, K = 5, MaxHamming = 2;
        public double Wg = 0.5, Wp = 0.25;
        public IFeelPolicy Fallback;
        readonly Dictionary<string, int> _index = new Dictionary<string, int>();
        readonly List<byte[]> _keys = new List<byte[]>();
        readonly List<int> _g = new List<int>(), _p = new List<int>();
        readonly List<int[]> _votes = new List<int[]>();
        int _gd = -1, _pb = -1;
        public string Name { get { return MechanismName; } }
        public int KeyCount { get { return _keys.Count; } }
        public void SetContext(int g, int p) { _gd = g; _pb = p; }
        public void Ingest(FeelTrajectory t)
        {
            if (t == null || !t.Reached || t.Feels.Length < L) return;
            for (int e = L - 1; e < t.Feels.Length; e++)
            {
                int mv = t.Moves[e]; if (mv < 0 || mv > 7) continue;
                string key = FeelUtil.Key(t.Feels, e, L) + GoalCtxUtil.Ctx(t.Gd[e], t.Pb[e]);
                int idx;
                if (!_index.TryGetValue(key, out idx))
                {
                    if (_keys.Count >= GoalCtxUtil.MaxKeys) continue;
                    idx = _keys.Count; _index[key] = idx;
                    var w = new byte[L]; Array.Copy(t.Feels, e - L + 1, w, 0, L);
                    _keys.Add(w); _g.Add(t.Gd[e]); _p.Add(t.Pb[e]); _votes.Add(new int[8]);
                }
                _votes[idx][mv]++;
            }
        }
        public bool TryLookup(IList<byte> h, out int rel, out int support, out string mk)
        {
            rel = -1; support = 0; mk = null; LastConf = 0;
            var q = _gd >= 0 ? FeelUtil.Tail(h, L) : null;
            if (q != null && _keys.Count > 0)
            {
                var cand = new List<KeyValuePair<double, int>>();
                for (int i = 0; i < _keys.Count; i++)
                {
                    int ham = FeelUtil.Hamming(q, _keys[i]);
                    if (ham > MaxHamming) continue;
                    double d = ham + Wg * GoalCtxUtil.CircDir(_gd, _g[i]) + Wp * Math.Abs(_pb - _p[i]);
                    cand.Add(new KeyValuePair<double, int>(d, i));
                }
                if (cand.Count > 0)
                {
                    cand.Sort((x, y) => x.Key.CompareTo(y.Key));
                    double cutoff = cand[Math.Min(K, cand.Count) - 1].Key;
                    var score = new double[8];
                    foreach (var c in cand)
                    {
                        if (c.Key > cutoff + 1e-9) break;
                        double w = 1.0 / (1.0 + c.Key);
                        for (int a = 0; a < 8; a++) score[a] += w * _votes[c.Value][a];
                        support++;
                    }
                    int best = 0; for (int a = 1; a < 8; a++) if (score[a] > score[best]) best = a;
                    if (score[best] > 0) { double tot = 0; for (int a = 0; a < 8; a++) tot += score[a]; LastConf = score[best] / tot; rel = best; mk = "gknn"; return true; }
                }
            }
            return Fallback != null && Fallback.TryLookup(h, out rel, out support, out mk);
        }
        public string StatusLine() { return MechanismName + " K=" + K + " Wg=" + Wg + " keys=" + _keys.Count; }
    }

    /// <summary>Goal-conditioned episodic control: (window, goal dir, progress) -> best MC return per action;
    /// unseen action -> mean of K nearest (combined distance) items. Unknown context or nothing -> feel-only EC fallback.</summary>
    public sealed class GcEpisodicControl : IFeelPolicy, IGoalCtxPolicy
    {
        public const string MechanismName = "EpisodicControl_GC";
        public int L = 5, K = 5, MaxHamming = 2;
        public double Wg = 0.5, Wp = 0.25, FailPenalty = 50.0;
        public IFeelPolicy Fallback;
        readonly Dictionary<string, int> _index = new Dictionary<string, int>();
        readonly List<byte[]> _keys = new List<byte[]>();
        readonly List<int> _g = new List<int>(), _p = new List<int>();
        readonly List<double[]> _q = new List<double[]>();
        int _gd = -1, _pb = -1;
        public string Name { get { return MechanismName; } }
        public int KeyCount { get { return _keys.Count; } }
        public void SetContext(int g, int p) { _gd = g; _pb = p; }
        public void Ingest(FeelTrajectory t)
        {
            if (t == null || t.Feels.Length < L) return;
            int n = t.Feels.Length;
            var ret = new double[n];
            double acc = t.Reached ? 0 : -FailPenalty;
            for (int i = n - 1; i >= 0; i--) { acc -= t.Costs[i]; ret[i] = acc; }
            for (int e = L - 1; e < n; e++)
            {
                int mv = t.Moves[e]; if (mv < 0 || mv > 7) continue;
                string key = FeelUtil.Key(t.Feels, e, L) + GoalCtxUtil.Ctx(t.Gd[e], t.Pb[e]);
                int idx;
                if (!_index.TryGetValue(key, out idx))
                {
                    if (_keys.Count >= GoalCtxUtil.MaxKeys) continue;
                    idx = _keys.Count; _index[key] = idx;
                    var w = new byte[L]; Array.Copy(t.Feels, e - L + 1, w, 0, L);
                    _keys.Add(w); _g.Add(t.Gd[e]); _p.Add(t.Pb[e]);
                    var qa = new double[8]; for (int a = 0; a < 8; a++) qa[a] = double.NaN; _q.Add(qa);
                }
                var row = _q[idx];
                if (double.IsNaN(row[mv]) || ret[e] > row[mv]) row[mv] = ret[e];
            }
        }
        public bool TryLookup(IList<byte> h, out int rel, out int support, out string mk)
        {
            rel = -1; support = 0; mk = null;
            var q = _gd >= 0 ? FeelUtil.Tail(h, L) : null;
            if (q != null && _keys.Count > 0)
            {
                string key = FeelUtil.Key(q, L - 1, L) + GoalCtxUtil.Ctx(_gd, _pb);
                int exact; bool hasExact = _index.TryGetValue(key, out exact);
                var cand = new List<KeyValuePair<double, int>>();
                for (int i = 0; i < _keys.Count; i++)
                {
                    int ham = FeelUtil.Hamming(q, _keys[i]);
                    if (ham > MaxHamming) continue;
                    cand.Add(new KeyValuePair<double, int>(ham + Wg * GoalCtxUtil.CircDir(_gd, _g[i]) + Wp * Math.Abs(_pb - _p[i]), i));
                }
                cand.Sort((x, y) => x.Key.CompareTo(y.Key));
                double bestV = double.NegativeInfinity; int bestA = -1;
                for (int a = 0; a < 8; a++)
                {
                    double v = double.NaN;
                    if (hasExact && !double.IsNaN(_q[exact][a])) v = _q[exact][a];
                    else
                    {
                        double s = 0; int c = 0;
                        foreach (var kv in cand) { double qv = _q[kv.Value][a]; if (double.IsNaN(qv)) continue; s += qv; c++; if (c >= K) break; }
                        if (c > 0) v = s / c;
                    }
                    if (!double.IsNaN(v) && v > bestV) { bestV = v; bestA = a; }
                }
                if (bestA >= 0) { rel = bestA; support = 1; mk = hasExact ? "gec:exact" : "gec:knn"; return true; }
            }
            return Fallback != null && Fallback.TryLookup(h, out rel, out support, out mk);
        }
        public string StatusLine() { return MechanismName + " K=" + K + " Wg=" + Wg + " keys=" + _keys.Count; }
    }

    /// <summary>Goal-conditioned tabular Q: state = feel n-gram (<=L) + goal dir + progress bin; same offline batch Q as TabularQ.
    /// Unseen state or unknown context -> feel-only TabularQ fallback.</summary>
    public sealed class GcTabularQ : IFeelPolicy, IGoalCtxPolicy
    {
        public const string MechanismName = "TabularQ_GC";
        public int L = 5, Sweeps = 30;
        public double Alpha = 0.2, Gamma = 0.99, FailPenalty = 50.0;
        public IFeelPolicy Fallback;
        readonly Dictionary<string, double[]> _q = new Dictionary<string, double[]>();
        readonly List<FeelTrajectory> _data = new List<FeelTrajectory>();
        int _gd = -1, _pb = -1;
        public string Name { get { return MechanismName; } }
        public int KeyCount { get { return _q.Count; } }
        public void SetContext(int g, int p) { _gd = g; _pb = p; }
        string SK(FeelTrajectory t, int i) { int len = Math.Min(L, i + 1); return FeelUtil.Key(t.Feels, i, len) + GoalCtxUtil.Ctx(t.Gd[i], t.Pb[i]); }
        double[] Row(string k) { double[] r; if (!_q.TryGetValue(k, out r)) { r = new double[8]; for (int a = 0; a < 8; a++) r[a] = double.NaN; _q[k] = r; } return r; }
        static double MaxQ(double[] r) { double m = double.NaN; for (int a = 0; a < 8; a++) if (!double.IsNaN(r[a]) && (double.IsNaN(m) || r[a] > m)) m = r[a]; return double.IsNaN(m) ? 0 : m; }
        public void Ingest(FeelTrajectory t) { if (t != null && t.Feels.Length > 0) _data.Add(t); }
        public void Train()
        {
            for (int sweep = 0; sweep < Sweeps; sweep++)
                foreach (var t in _data)
                {
                    int n = t.Feels.Length;
                    for (int i = n - 1; i >= 0; i--)
                    {
                        int a = t.Moves[i]; if (a < 0 || a > 7) continue;
                        var row = Row(SK(t, i));
                        double target = (i == n - 1) ? -t.Costs[i] + (t.Reached ? 0 : -FailPenalty) : -t.Costs[i] + Gamma * MaxQ(Row(SK(t, i + 1)));
                        if (double.IsNaN(row[a])) row[a] = target; else row[a] += Alpha * (target - row[a]);
                    }
                }
        }
        public bool TryLookup(IList<byte> h, out int rel, out int support, out string mk)
        {
            rel = -1; support = 0; mk = null;
            if (_gd >= 0 && h != null && h.Count > 0)
            {
                int len = Math.Min(L, h.Count);
                var tail = FeelUtil.Tail(h, len);
                string k = FeelUtil.Key(tail, len - 1, len) + GoalCtxUtil.Ctx(_gd, _pb);
                double[] r;
                if (_q.TryGetValue(k, out r))
                {
                    int best = -1; for (int a = 0; a < 8; a++) if (!double.IsNaN(r[a]) && (best < 0 || r[a] > r[best])) best = a;
                    if (best >= 0) { rel = best; support = 1; mk = k; return true; }
                }
            }
            return Fallback != null && Fallback.TryLookup(h, out rel, out support, out mk);
        }
        public string StatusLine() { return MechanismName + " states=" + _q.Count; }
    }

    /// <summary>All eight baseline arms built from one set of teacher rollouts (optionally excluding one world for inner-fold tuning).</summary>
    public sealed class StageBSet
    {
        public TabularNgramBC Bc; public KnnFeelMemory Knn; public EpisodicControlFeel Ec; public TabularQFeel Q;
        public GcTabularBC GBc; public GcKnn GKnn; public GcEpisodicControl GEc; public GcTabularQ GQ;

        public static StageBSet FromBundle(RevisionTrainer.Bundle b, int ngram)
        {
            var s = new StageBSet { Bc = b.Bc, Knn = b.Knn, Ec = b.Ec, Q = b.Q };
            s.AddGc(b.Recs, null, ngram);
            return s;
        }

        public static StageBSet Build(List<StageBRec> recs, string excludeWorld, int ngram)
        {
            var s = new StageBSet { Bc = new TabularNgramBC(), Knn = new KnnFeelMemory { L = ngram }, Ec = new EpisodicControlFeel { L = ngram }, Q = new TabularQFeel { L = ngram } };
            s.Bc.Clear();
            foreach (var r in recs)
            {
                if (r.World.Name == excludeWorld) continue;
                if (r.Att != null) { s.Bc.IngestPath(r.Att, r.World); s.Knn.Ingest(r.Traj); }
                s.Ec.Ingest(r.Traj); s.Q.Ingest(r.Traj);
            }
            s.Q.Train(new Random(1));
            s.AddGc(recs, excludeWorld, ngram);
            return s;
        }

        void AddGc(List<StageBRec> recs, string excludeWorld, int ngram)
        {
            GBc = new GcTabularBC { Fallback = Bc };
            GKnn = new GcKnn { L = ngram, Fallback = Knn };
            GEc = new GcEpisodicControl { L = ngram, Fallback = Ec };
            GQ = new GcTabularQ { L = ngram, Fallback = Q };
            foreach (var r in recs)
            {
                if (excludeWorld != null && r.World.Name == excludeWorld) continue;
                GBc.Ingest(r.Traj); GKnn.Ingest(r.Traj); GEc.Ingest(r.Traj); GQ.Ingest(r.Traj);
            }
            GQ.Train();
        }
    }
}
