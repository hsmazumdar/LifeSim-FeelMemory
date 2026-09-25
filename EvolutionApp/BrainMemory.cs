using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Evolution
{
    /// <summary>
    /// Direction cell kind for sensing: categorizes terrain by roughness/feel.
    /// </summary>
    public enum DirCellKind : byte
    {
        Smooth = 0,
        Medium = 1,
        Rough = 2,
        Obstacle = 3,
        Edge = 4,
        Goal = 5
    }

    /// <summary>
    /// One path step in STM: relative 8-neigh sense + move + goal nearness/direction.
    /// </summary>
    public enum FeelCode : byte { Smooth = 0, Medium = 1, Rough = 2, Obstacle = 3 }

    public struct SenseMoveTrace
    {
        public DirCellKind[] Sense;
        public int MoveDir;
        public byte Outcome;
        public long Tick;
        public float EnergyDelta;
        public bool EpisodeEnd;
        /// <summary>Goal nearness bin 0..9 at this step (encode-time; goal known).</summary>
        public byte GoalNearness;
        /// <summary>Facing-relative goal direction octant 0..7.</summary>
        public byte GoalDirection;
        public FeelCode Feel;
        public int StepFromStart;
        public byte ProgressBin;
        public bool IsStartMarker;
        public bool IsGoalMarker;
        public string WorldId;
        public short CellX;
        public short CellY;
        public byte Facing;
    }

    /// <summary>
    /// V14 memory UNIT: 10-tuple (8 neigh + nearness + goalDir) plus preferred move/strength for LTM.
    /// Uniqueness is on the 10-tuple only (UnitKey).
    /// </summary>
    public struct SenseMoveRule
    {
        public DirCellKind D0, D1, D2, D3, D4, D5, D6, D7;
        public byte GoalNearness;
        public byte GoalDirection;
        public int MoveDir;
        public float Strength;
        public int Sightings;
        public bool Used;

        public byte F0, F1, F2, F3, F4;
        public byte NgramLen;
        public byte ProgressBin;
        public string WorldId;
        public const byte DirUnset = 255;

        public byte GetFeel(int i)
        {
            switch (i) { case 0: return F0; case 1: return F1; case 2: return F2; case 3: return F3; default: return F4; }
        }
        public void SetFeel(int i, byte f)
        {
            switch (i) { case 0: F0 = f; break; case 1: F1 = f; break; case 2: F2 = f; break; case 3: F3 = f; break; default: F4 = f; break; }
        }
        public static SenseMoveRule FromFeelNgram(byte[] feels, int len, byte progressBin, byte goalDir, int moveDir, float strength, string worldId)
        {
            var r = new SenseMoveRule {
                MoveDir = moveDir, Strength = strength, Sightings = 1, Used = true,
                GoalNearness = progressBin, GoalDirection = goalDir,
                NgramLen = (byte)Math.Max(0, Math.Min(5, len)), ProgressBin = progressBin, WorldId = worldId ?? ""
            };
            for (int i = 0; i < 5; i++) r.SetFeel(i, 255);
            for (int i = 0; i < r.NgramLen; i++) r.SetFeel(i, feels[i]);
            return r;
        }
        public static string FeelAtlasKey(string worldId, byte progressBin, byte goalDir, byte nlen, byte f0, byte f1, byte f2, byte f3, byte f4)
        {
            var sb = new StringBuilder(48);
            sb.Append(worldId ?? "").Append('|').Append('P').Append(progressBin).Append('D').Append(goalDir).Append('L').Append(nlen).Append(':');
            if (nlen > 0) sb.Append(f0);
            if (nlen > 1) sb.Append('-').Append(f1);
            if (nlen > 2) sb.Append('-').Append(f2);
            if (nlen > 3) sb.Append('-').Append(f3);
            if (nlen > 4) sb.Append('-').Append(f4);
            return sb.ToString();
        }

        public DirCellKind Get(int i)
        {
            switch (i)
            {
                case 0: return D0; case 1: return D1; case 2: return D2; case 3: return D3;
                case 4: return D4; case 5: return D5; case 6: return D6; default: return D7;
            }
        }

        public void Set(int i, DirCellKind k)
        {
            switch (i)
            {
                case 0: D0 = k; break; case 1: D1 = k; break; case 2: D2 = k; break; case 3: D3 = k; break;
                case 4: D4 = k; break; case 5: D5 = k; break; case 6: D6 = k; break; default: D7 = k; break;
            }
        }

        public bool ExactMatch(DirCellKind[] sense)
        {
            for (int i = 0; i < 8; i++) if (Get(i) != sense[i]) return false;
            return true;
        }

        public bool ExactUnitMatch(DirCellKind[] sense, byte nearness, byte goalDir)
        {
            if (!ExactMatch(sense)) return false;
            return GoalNearness == nearness && GoalDirection == goalDir;
        }

        public int Hamming(DirCellKind[] sense)
        {
            int d = 0;
            for (int i = 0; i < 8; i++) if (Get(i) != sense[i]) d++;
            return d;
        }

        public static SenseMoveRule FromSense(DirCellKind[] sense, int moveDir, float strength)
        {
            return FromUnit(sense, 0, 0, moveDir, strength);
        }

        public static SenseMoveRule FromUnit(DirCellKind[] sense, byte nearness, byte goalDir, int moveDir, float strength)
        {
            var r = new SenseMoveRule
            {
                MoveDir = moveDir,
                Strength = strength,
                Sightings = 1,
                Used = true,
                GoalNearness = nearness,
                GoalDirection = goalDir
            };
            for (int i = 0; i < 8; i++) r.Set(i, sense[i]);
            return r;
        }

        /// <summary>10-tuple uniqueness key (no move).</summary>
        public string UnitKey()
        {
            if (NgramLen > 0)
                return FeelAtlasKey(WorldId, ProgressBin, GoalDirection, NgramLen, F0, F1, F2, F3, F4);
            var sb = new StringBuilder(20);
            for (int i = 0; i < 8; i++) sb.Append((int)Get(i));
            sb.Append('N').Append(GoalNearness);
            sb.Append('D').Append(GoalDirection);
            return sb.ToString();
        }

        public static string UnitKeyFrom(DirCellKind[] sense, byte nearness, byte goalDir)
        {
            if (sense == null || sense.Length != 8) return null;
            var sb = new StringBuilder(20);
            for (int i = 0; i < 8; i++) sb.Append((int)sense[i]);
            sb.Append('N').Append(nearness);
            sb.Append('D').Append(goalDir);
            return sb.ToString();
        }

        public string PackKey()
        {
            return UnitKey() + ":" + MoveDir;
        }

        public string SenseKey()
        {
            var sb = new StringBuilder(8);
            for (int i = 0; i < 8; i++) sb.Append((int)Get(i));
            return sb.ToString();
        }

        public static string SenseKeyFrom(DirCellKind[] sense)
        {
            if (sense == null || sense.Length != 8) return null;
            var sb = new StringBuilder(8);
            for (int i = 0; i < 8; i++) sb.Append((int)sense[i]);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Durable per-signature move tallies (Pref-&gt;LTM compatibility path).
    /// </summary>
    public sealed class PreferredMoveVoteBucket
    {
        public DirCellKind[] Sense;
        public readonly float[] Votes = new float[8];
        public int TotalSamples;
        public float TotalMass;

        public int ArgMaxMove()
        {
            int best = 0;
            float bestV = Votes[0];
            for (int i = 1; i < 8; i++)
            {
                if (Votes[i] > bestV) { bestV = Votes[i]; best = i; }
            }
            return best;
        }

        public float BestVote()
        {
            float best = Votes[0];
            for (int i = 1; i < 8; i++) if (Votes[i] > best) best = Votes[i];
            return best;
        }
    }

    /// <summary>
    /// V14: STM = full path history of a single tour (cap = 2*W*H, default 800).
    /// Tours longer than MaxPathSteps are discarded with no LTM update.
    /// At end of a finished tour (&lt;=cap), unique 10-slot units (8-neigh + nearness + goalDir)
    /// not already in STM-unique or LTM are promoted into LTM (strength++ / vote).
    /// Policy: brain-v16-feel-atlas. Fix2: ClearStm / CloneFrozenLtmOnly preserved. Success-only LTM promote.
    /// </summary>
    public sealed class WorldFingerprint
    {
        public string WorldId = "";
        public readonly int[] RoughnessHist = new int[4];
        public readonly Dictionary<string, int> NgramCounts = new Dictionary<string, int>();
        public int SuccessfulTours;
        public int FailedTours;
        public int TotalStepsOnSuccess;
        public int StartGoalBrackets;
        public void AddFeel(FeelCode f) { int i = (int)f; if (i >= 0 && i < 4) RoughnessHist[i]++; }
        public void AddNgram(string key) { if (string.IsNullOrEmpty(key)) return; int c; NgramCounts.TryGetValue(key, out c); NgramCounts[key] = c + 1; }
    }

    public sealed class BrainMemory
    {
        public const string PolicyVersion = "brain-v16.1-feel-atlas";
        public const int MemFormatVersion = 2;
        public const int DefaultStmCapacity = 1000;
        public const int DefaultLtmCapacity = 512;
        public const int DefaultCapacity = DefaultLtmCapacity;
        public int Capacity { get { return LtmCapacity; } }
        public const int MaxHamming = 2;
        public const int PromoteEvery = 8; // unused in V14 path-end promote; kept for API

        public static readonly int[] SenseDx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        public static readonly int[] SenseDy = { -1, -1, 0, 1, 1, 1, 0, -1 };

        readonly SenseMoveTrace[] _stm;
        readonly SenseMoveRule[] _ltm;
        int _stmHead;
        int _stmCount;
        long _tick;
        int _sincePromote;
        bool _pathOverflow;

        int _episodeStartStm;
        long _episodeStartTick;
        int _maxPathSteps;

        /// <summary>Tour-local unique 10-tuple keys (filled at end-of-tour extract).</summary>
        readonly HashSet<string> _uniqueStmKeys = new HashSet<string>();
        readonly List<SenseMoveRule> _uniqueStmUnits = new List<SenseMoveRule>();

        /// <summary>All LTM unit keys for O(1) uniqueness checks.</summary>
        readonly HashSet<string> _ltmUnitKeys = new HashSet<string>();

        readonly Dictionary<string, PreferredMoveVoteBucket> _prefVotes =
            new Dictionary<string, PreferredMoveVoteBucket>();
        readonly Dictionary<string, WorldFingerprint> _worldFingerprints = new Dictionary<string, WorldFingerprint>();
        string _activeWorldId = "";
        int _tourStep;
        FeelCode _lastFeel = FeelCode.Medium;
        int _nextAnalyzeAt = 150;
        readonly Random _analyzeRng = new Random(42);
        public const byte GoalDirUnset = 255;
        public string ActiveWorldId { get { return _activeWorldId; } set { _activeWorldId = value ?? ""; } }
        /// <summary>When true, ObserveFeel records STM for matching but does not promote into LTM (frozen eval).</summary>
        public bool SuppressPromote { get; set; }
        public int MatchAttempts { get; private set; }
        public int MatchHits { get; private set; }
        public int MatchSteerApplied { get; private set; }
        public void ResetMatchCounters() { MatchAttempts = MatchHits = MatchSteerApplied = 0; }
        public int WorldFingerprintCount { get { return _worldFingerprints.Count; } }

        public int PreferredVoteSignatureCount { get { return _prefVotes.Count; } }
        public float PreferredVoteTotalMass
        {
            get
            {
                float s = 0;
                foreach (var kv in _prefVotes) s += kv.Value.TotalMass;
                return s;
            }
        }

        public int StmCapacity { get { return _stm.Length; } }
        public int LtmCapacity { get { return _ltm.Length; } }
        public int StmCount { get { return _stmCount; } }
        public int LtmCount { get; private set; }
        public int Count { get { return LtmCount; } }
        public int PromoteCount { get; private set; }
        public int StmDropCount { get; private set; }
        public int ToursDiscarded { get; private set; }
        public int UniquePromotedLastTour { get; private set; }
        public int MaxPathSteps { get { return _maxPathSteps; } }

        public bool HasStep { get { return _stmCount > 0; } }
        public bool LastWasObstacle { get; private set; }
        public bool LastWasRough { get; private set; }
        public int MergeCount { get { return PromoteCount; } }
        public int RoughCount { get; private set; }
        public int ObstacleBumpCount { get; private set; }
        public int SmoothCount { get; private set; }

        public BrainMemory(int stmCapacity = DefaultStmCapacity, int ltmCapacity = DefaultLtmCapacity)
        {
            if (stmCapacity < 16) stmCapacity = 16;
            if (ltmCapacity < 32) ltmCapacity = 32;
            _stm = new SenseMoveTrace[stmCapacity];
            _ltm = new SenseMoveRule[ltmCapacity];
            _maxPathSteps = stmCapacity;
        }

        /// <summary>Set MaxPathSteps = 2 * cols * rows (default world 20x20 =&gt; 800).</summary>
        public void ConfigureForWorld(int cols, int rows)
        {
            if (cols < 1) cols = 20;
            if (rows < 1) rows = 20;
            int cap = 2 * cols * rows;
            if (cap < 16) cap = 16;
            _maxPathSteps = Math.Min(cap, _stm.Length);
        }

        public void ConfigureForWorld(World world)
        {
            if (world == null) { _maxPathSteps = Math.Min(DefaultStmCapacity, _stm.Length); return; }
            ConfigureForWorld(world.Cols, world.Rows);
            if (!string.IsNullOrEmpty(world.Name)) ActiveWorldId = world.Name;
        }

        public void Clear()
        {
            for (int i = 0; i < _stm.Length; i++) _stm[i] = default(SenseMoveTrace);
            for (int i = 0; i < _ltm.Length; i++) _ltm[i] = default(SenseMoveRule);
            _stmHead = _stmCount = 0;
            LtmCount = 0;
            _tick = 0;
            PromoteCount = 0;
            StmDropCount = 0;
            ToursDiscarded = 0;
            UniquePromotedLastTour = 0;
            RoughCount = ObstacleBumpCount = SmoothCount = 0;
            LastWasObstacle = LastWasRough = false;
            _episodeStartStm = 0;
            _episodeStartTick = 0;
            _pathOverflow = false;
            _prefVotes.Clear();
            _uniqueStmKeys.Clear();
            _uniqueStmUnits.Clear();
            _ltmUnitKeys.Clear();
            _worldFingerprints.Clear();
            _activeWorldId = "";
            _tourStep = 0;
            _lastFeel = FeelCode.Medium;
            _nextAnalyzeAt = 150;
        }

        public void ClearPreferredVotes()
        {
            _prefVotes.Clear();
        }

        /// <summary>
        /// Fix2 honesty: wipe STM only so frozen eval is LTM + formula, not warm training STM.
        /// </summary>
        public void ClearStm()
        {
            for (int i = 0; i < _stm.Length; i++) _stm[i] = default(SenseMoveTrace);
            _stmHead = 0;
            _stmCount = 0;
            StmDropCount = 0;
            _episodeStartStm = 0;
            _episodeStartTick = 0;
            _pathOverflow = false;
            _tourStep = 0;
            _lastFeel = FeelCode.Medium;
            _nextAnalyzeAt = 150;
            _uniqueStmKeys.Clear();
            _uniqueStmUnits.Clear();
        }

        public BrainMemory CloneFrozenLtmOnly()
        {
            var c = Clone();
            c.ClearStm();
            c.SuppressPromote = true; // Fix2: sense for match, do not grow LTM mid-eval
            c.ResetMatchCounters();
            return c;
        }

        /// <summary>
        /// Goal nearness bin 0..9 from Manhattan distance relative to map diagonal.
        /// </summary>
        public static byte ComputeGoalNearness(Point cell, Point goal, int cols, int rows)
        {
            int man = Math.Abs(goal.X - cell.X) + Math.Abs(goal.Y - cell.Y);
            int diag = Math.Max(1, (cols - 1) + (rows - 1));
            // 0 = farthest, 9 = at/near goal
            double t = 1.0 - Math.Min(1.0, (double)man / diag);
            int bin = (int)Math.Floor(t * 9.999);
            if (bin < 0) bin = 0;
            if (bin > 9) bin = 9;
            return (byte)bin;
        }

        /// <summary>
        /// Facing-relative goal direction octant 0..7 (same rotation as relative moves).
        /// </summary>
        public static byte ComputeGoalDirection(Point cell, Point goal, int facingDir)
        {
            int dx = Math.Sign(goal.X - cell.X);
            int dy = Math.Sign(goal.Y - cell.Y);
            if (dx == 0 && dy == 0) return 0;
            int abs = -1;
            for (int i = 0; i < 8; i++)
            {
                if (SenseDx[i] == dx && SenseDy[i] == dy) { abs = i; break; }
            }
            if (abs < 0)
            {
                // Prefer axis-aligned if one component dominates
                int adx = Math.Abs(goal.X - cell.X);
                int ady = Math.Abs(goal.Y - cell.Y);
                if (adx >= ady) dy = 0; else dx = 0;
                dx = Math.Sign(dx == 0 && goal.X != cell.X ? goal.X - cell.X : dx);
                dy = Math.Sign(dy == 0 && goal.Y != cell.Y ? goal.Y - cell.Y : dy);
                for (int i = 0; i < 8; i++)
                    if (SenseDx[i] == dx && SenseDy[i] == dy) { abs = i; break; }
            }
            if (abs < 0) abs = 0;
            return (byte)RelativeMoveFromAbsolute(facingDir, abs);
        }

        /// <summary>
        /// V14 TeacherSuccess: ingest one successful teacher step into current tour path STM.
        /// Call EndTourAndPromote after the full path (or use IngestTeacherSuccessTour).
        /// </summary>
        public void IngestTeacherSuccessStep(DirCellKind[] relativeSense, int relativeMove, float boost = 1.0f)
        {
            IngestTeacherSuccessStep(relativeSense, relativeMove, 0, 0, boost);
        }

        public void IngestTeacherSuccessStep(DirCellKind[] relativeSense, int relativeMove,
            byte goalNearness, byte goalDirection, float boost = 1.0f)
        {
            IngestTeacherSuccessStep(relativeSense, relativeMove, OutcomeToFeel(0), short.MinValue, short.MinValue, 0, boost);
        }

        /// <summary>V15 teacher step: store feel/move without goal-dir; backfill after success.</summary>
        public void IngestTeacherSuccessStep(DirCellKind[] relativeSense, int relativeMove, FeelCode feel,
            short cellX, short cellY, byte facing, float boost = 1.0f)
        {
            if (relativeMove < 0 || relativeMove > 7) return;
            byte outcome = (byte)feel;
            RecordPreferredMoveVote(relativeSense, relativeMove, outcome, 0f, Math.Max(0.1f, boost));
            AppendPathStep(relativeSense, relativeMove, outcome, 0f, GoalDirUnset, feel, cellX, cellY, facing);
        }

        /// <summary>
        /// Preferred: ingest a full successful teacher path with goal known; unique 10-slot promote.
        /// Discards if path length &gt; MaxPathSteps.
        /// </summary>
        public int IngestTeacherSuccessTour(
            IList<DirCellKind[]> relativeSenses,
            IList<int> relativeMoves,
            IList<byte> nearness,
            IList<byte> goalDirs,
            float boost = 1.0f)
        {
            return IngestTeacherSuccessTour(relativeSenses, relativeMoves, null, null, null, Point.Empty, Point.Empty, boost);
        }

        public int IngestTeacherSuccessTour(
            IList<DirCellKind[]> relativeSenses,
            IList<int> relativeMoves,
            IList<FeelCode> feels,
            IList<Point> cells,
            IList<byte> facings,
            Point start,
            Point goal,
            float boost = 1.0f)
        {
            if (relativeSenses == null || relativeMoves == null) return 0;
            int n = relativeSenses.Count;
            if (n != relativeMoves.Count) return 0;
            ClearStm();
            MarkEpisodeStart();
            if (cells != null && cells.Count > 0)
                SetTourGoalForBackfill(start, goal);
            for (int i = 0; i < n; i++)
            {
                FeelCode feel = (feels != null && i < feels.Count) ? feels[i] : FeelCode.Medium;
                short cx = short.MinValue, cy = short.MinValue;
                byte facing = 0;
                if (cells != null && i < cells.Count) { cx = (short)cells[i].X; cy = (short)cells[i].Y; }
                if (facings != null && i < facings.Count) facing = facings[i];
                IngestTeacherSuccessStep(relativeSenses[i], relativeMoves[i], feel, cx, cy, facing, boost);
            }
            return EndTourAndPromote(true, boost);
        }

        public int PopulateLtmFromTeacherSuccess(float minVoteMass = 0.5f, int maxRules = -1)
        {
            // V14: successful teacher tours already unique-promoted via IngestTeacherSuccessTour.
            // Do not Pref-flush (that would inject near=0/dir=0 units and pollute uniqueness).
            RecountLtm();
            return LtmCount;
        }

        public BrainMemory Clone()
        {
            var c = new BrainMemory(_stm.Length, _ltm.Length);
            Array.Copy(_stm, c._stm, _stm.Length);
            Array.Copy(_ltm, c._ltm, _ltm.Length);
            c._stmHead = _stmHead;
            c._stmCount = _stmCount;
            c.LtmCount = LtmCount;
            c._tick = _tick;
            c._sincePromote = _sincePromote;
            c.PromoteCount = PromoteCount;
            c.StmDropCount = StmDropCount;
            c.ToursDiscarded = ToursDiscarded;
            c.SmoothCount = SmoothCount;
            c.RoughCount = RoughCount;
            c.ObstacleBumpCount = ObstacleBumpCount;
            c._episodeStartStm = _episodeStartStm;
            c._episodeStartTick = _episodeStartTick;
            c._maxPathSteps = _maxPathSteps;
            c._pathOverflow = _pathOverflow;
            foreach (var kv in _prefVotes)
            {
                var src = kv.Value;
                var dst = new PreferredMoveVoteBucket
                {
                    Sense = src.Sense != null ? (DirCellKind[])src.Sense.Clone() : null,
                    TotalSamples = src.TotalSamples,
                    TotalMass = src.TotalMass
                };
                Array.Copy(src.Votes, dst.Votes, 8);
                c._prefVotes[kv.Key] = dst;
            }
            foreach (var k in _ltmUnitKeys) c._ltmUnitKeys.Add(k);
            foreach (var k in _uniqueStmKeys) c._uniqueStmKeys.Add(k);
            foreach (var u in _uniqueStmUnits) c._uniqueStmUnits.Add(u);
            return c;
        }

        public static DirCellKind[] Sense8(World world, Point cell)
        {
            var s = new DirCellKind[8];
            for (int i = 0; i < 8; i++)
            {
                int nx = cell.X + SenseDx[i];
                int ny = cell.Y + SenseDy[i];
                if (nx < 0 || ny < 0 || nx >= world.Cols || ny >= world.Rows)
                {
                    s[i] = DirCellKind.Edge;
                    continue;
                }
                var p = new Point(nx, ny);
                if (p == world.GoalCell)
                {
                    s[i] = DirCellKind.Goal;
                }
                else if (world.CellHasObstacle(p))
                {
                    s[i] = DirCellKind.Obstacle;
                }
                else
                {
                    float roughness = world.CellRoughness(p);
                    if (roughness <= MotorMemory.SmoothThreshold)
                        s[i] = DirCellKind.Smooth;
                    else if (roughness >= MotorMemory.RoughThreshold)
                        s[i] = DirCellKind.Rough;
                    else
                        s[i] = DirCellKind.Medium;
                }
            }
            return s;
        }

        public static DirCellKind[] RotateSense(DirCellKind[] absolute, int facingDir)
        {
            if (absolute == null || absolute.Length != 8) return absolute;
            facingDir = ((facingDir % 8) + 8) % 8;
            var rel = new DirCellKind[8];
            for (int i = 0; i < 8; i++)
                rel[i] = absolute[(facingDir + i) % 8];
            return rel;
        }

        public static int AbsoluteMoveFromRelative(int facingDir, int relativeMove)
        {
            return ((facingDir + relativeMove) % 8 + 8) % 8;
        }

        public static int RelativeMoveFromAbsolute(int facingDir, int absoluteMove)
        {
            return ((absoluteMove - facingDir) % 8 + 8) % 8;
        }

        public void MarkEpisodeStart()
        {
            _episodeStartStm = _stmCount;
            _episodeStartTick = _tick;
            _pathOverflow = false;
            _tourStep = 0;
            _lastFeel = FeelCode.Medium;
            _nextAnalyzeAt = 100 + _analyzeRng.Next(0, 101);
            _uniqueStmKeys.Clear();
            _uniqueStmUnits.Clear();
            if (!string.IsNullOrEmpty(_activeWorldId))
            {
                AppendSpecialMarker(true, false);
                var fp = EnsureFingerprint(_activeWorldId);
                fp.StartGoalBrackets++;
            }
        }

        public void Observe(DirCellKind[] sense, int moveDir, byte outcome, int facingDir)
        {
            Observe(sense, moveDir, outcome, facingDir, 0f, 0, 0);
        }

        public void Observe(DirCellKind[] sense, int moveDir, byte outcome, int facingDir, float energyDelta)
        {
            Observe(sense, moveDir, outcome, facingDir, energyDelta, 0, 0);
        }

        public void Observe(DirCellKind[] sense, int moveDir, byte outcome, int facingDir,
            float energyDelta, byte goalNearness, byte goalDirection)
        {
            // V15: do not store live goal-direction (unknown while walking).
            ObserveFeel(sense, moveDir, outcome, facingDir, energyDelta, short.MinValue, short.MinValue);
        }

        public void ObserveFeel(DirCellKind[] sense, int moveDir, byte outcome, int facingDir,
            float energyDelta, short cellX, short cellY)
        {
            if (moveDir < 0 || moveDir > 7) return;
            DirCellKind[] relSense = sense;
            int relMove = moveDir;
            if (sense != null && sense.Length == 8)
            {
                relSense = RotateSense(sense, facingDir);
                relMove = RelativeMoveFromAbsolute(facingDir, moveDir);
            }
            if (outcome == 0) { SmoothCount++; LastWasRough = false; LastWasObstacle = false; }
            else if (outcome == 1) { LastWasRough = false; LastWasObstacle = false; }
            else if (outcome == 2) { RoughCount++; LastWasRough = true; LastWasObstacle = false; }
            else { ObstacleBumpCount++; LastWasRough = false; LastWasObstacle = true; }

            FeelCode feel = OutcomeToFeel(outcome);
            bool roughChange = (_tourStep > 0 && feel != _lastFeel);
            AppendPathStep(relSense, relMove, outcome, energyDelta, GoalDirUnset, feel, cellX, cellY, (byte)(((facingDir % 8) + 8) % 8));
            RecordPreferredMoveVote(relSense, relMove, outcome, energyDelta, 1.0f);
            if (!string.IsNullOrEmpty(_activeWorldId)) EnsureFingerprint(_activeWorldId).AddFeel(feel);

            // V16 Fix E: no mid-tour LTM writes (random interval / roughness change).
            // AnalyzeStmAndMaybePromote only commits on successful EndTour / teacher ingest.
            bool due = _tourStep >= _nextAnalyzeAt;
            if (due)
            {
                _nextAnalyzeAt = _tourStep + 100 + _analyzeRng.Next(0, 101);
            }
            // roughChange retained for fingerprint / lastFeel tracking only (no LTM promote)
            _lastFeel = feel;
        }

        public static FeelCode OutcomeToFeel(byte outcome)
        {
            if (outcome == 0) return FeelCode.Smooth;
            if (outcome == 1) return FeelCode.Medium;
            if (outcome == 2) return FeelCode.Rough;
            return FeelCode.Obstacle;
        }

        public static byte ProgressBinFromStep(int step, int maxSteps)
        {
            double t = (double)step / Math.Max(1, maxSteps);
            if (t < 0.33) return 0;
            if (t < 0.67) return 1;
            return 2;
        }

        void AppendPathStep(DirCellKind[] sense, int moveDir, byte outcome, float energyDelta,
            byte goalDirection, FeelCode feel, short cellX, short cellY, byte facing)
        {
            _tick++;
            _tourStep++;
            if (_tourStep > _maxPathSteps)
            {
                _pathOverflow = true;
                StmDropCount++;
            }
            byte pbin = ProgressBinFromStep(_tourStep, _maxPathSteps);
            var tr = new SenseMoveTrace
            {
                Sense = sense != null ? (DirCellKind[])sense.Clone() : null,
                MoveDir = moveDir,
                Outcome = outcome,
                Tick = _tick,
                EnergyDelta = energyDelta,
                EpisodeEnd = false,
                GoalNearness = pbin,
                GoalDirection = goalDirection,
                Feel = feel,
                StepFromStart = _tourStep,
                ProgressBin = pbin,
                IsStartMarker = false,
                IsGoalMarker = false,
                WorldId = _activeWorldId,
                CellX = cellX,
                CellY = cellY,
                Facing = facing
            };
            _stm[_stmHead] = tr;
            _stmHead = (_stmHead + 1) % _stm.Length;
            if (_stmCount < _stm.Length) _stmCount++;
        }

        void AppendSpecialMarker(bool isStart, bool isGoal)
        {
            _tick++;
            var tr = new SenseMoveTrace
            {
                Sense = null,
                MoveDir = -1,
                Outcome = 0,
                Tick = _tick,
                EnergyDelta = 0,
                EpisodeEnd = isGoal,
                GoalNearness = isStart ? (byte)0 : (byte)2,
                GoalDirection = GoalDirUnset,
                Feel = FeelCode.Smooth,
                StepFromStart = _tourStep,
                ProgressBin = isStart ? (byte)0 : (byte)2,
                IsStartMarker = isStart,
                IsGoalMarker = isGoal,
                WorldId = _activeWorldId,
                CellX = short.MinValue,
                CellY = short.MinValue,
                Facing = 0
            };
            _stm[_stmHead] = tr;
            _stmHead = (_stmHead + 1) % _stm.Length;
            if (_stmCount < _stm.Length) _stmCount++;
        }

        WorldFingerprint EnsureFingerprint(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) worldId = "";
            WorldFingerprint fp;
            if (!_worldFingerprints.TryGetValue(worldId, out fp))
            {
                fp = new WorldFingerprint { WorldId = worldId };
                _worldFingerprints[worldId] = fp;
            }
            return fp;
        }

        /// <summary>Honest LOO: remove all LTM atlas entries tagged with worldId.</summary>
        public int StripWorldShard(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) return 0;
            int removed = 0;
            for (int i = 0; i < _ltm.Length; i++)
            {
                if (!_ltm[i].Used) continue;
                if (!string.Equals(_ltm[i].WorldId, worldId, StringComparison.OrdinalIgnoreCase)) continue;
                _ltmUnitKeys.Remove(_ltm[i].UnitKey());
                _ltm[i] = default(SenseMoveRule);
                removed++;
            }
            if (_worldFingerprints.ContainsKey(worldId)) _worldFingerprints.Remove(worldId);
            RecountLtm();
            return removed;
        }

        public BrainMemory CloneWithoutWorld(string worldId)
        {
            var c = Clone();
            c.StripWorldShard(worldId);
            c.ClearStm();
            return c;
        }

        void _v15_noop() { }

        public void RecordPreferredMoveVote(DirCellKind[] sense, int moveDir, byte outcome, float energyDelta, float boost = 1.0f)
        {
            if (sense == null || sense.Length != 8) return;
            if (moveDir < 0 || moveDir > 7) return;
            if (boost <= 0) return;

            float baseW;
            if (outcome == 0) baseW = 1.0f;
            else if (outcome == 1) baseW = 0.55f;
            else if (outcome == 2) baseW = 0.15f;
            else baseW = 0.05f;

            float energyW = 1.0f / (1.0f + Math.Max(0f, energyDelta));
            float w = baseW * energyW * boost;
            if (w < 0.01f) return;

            string key = SenseMoveRule.SenseKeyFrom(sense);
            if (key == null) return;

            PreferredMoveVoteBucket bucket;
            if (!_prefVotes.TryGetValue(key, out bucket))
            {
                bucket = new PreferredMoveVoteBucket { Sense = (DirCellKind[])sense.Clone() };
                _prefVotes[key] = bucket;
            }
            bucket.Votes[moveDir] += w;
            bucket.TotalMass += w;
            bucket.TotalSamples++;
        }

        public int PromotePreferredMovesFromTraces(bool episodeOnly, float boost = 1.0f)
        {
            int added = 0;
            for (int n = 0; n < _stmCount; n++)
            {
                int idx = (_stmHead - _stmCount + n + _stm.Length * 2) % _stm.Length;
                var t = _stm[idx];
                if (t.Sense == null) continue;
                if (episodeOnly && t.Tick < _episodeStartTick) continue;
                RecordPreferredMoveVote(t.Sense, t.MoveDir, t.Outcome, t.EnergyDelta, boost);
                added++;
            }
            return added;
        }

        public int PopulateLtmFromPreferredMoves(float minVoteMass = 0.8f, int maxRules = -1)
        {
            if (maxRules < 0) maxRules = LtmCapacity;
            var list = new List<KeyValuePair<string, PreferredMoveVoteBucket>>(_prefVotes);
            list.Sort((a, b) => b.Value.BestVote().CompareTo(a.Value.BestVote()));

            int written = 0;
            for (int i = 0; i < list.Count && written < maxRules; i++)
            {
                var bucket = list[i].Value;
                float best = bucket.BestVote();
                if (best < minVoteMass) continue;
                int move = bucket.ArgMaxMove();
                float str = Math.Min(6f, 0.25f * best);
                if (str < 0.15f) str = 0.15f;
                // Pref path has no goal encoding; store near=0,dir=0 (still unique 10-tuple)
                AppendOrMergeLtmUnit(bucket.Sense, 0, 0, move, str);
                written++;
                PromoteCount++;
            }
            RecountLtm();
            return written;
        }

        /// <summary>
        /// End-of-tour: if path &gt; MaxPathSteps discard; else extract unique 10-units vs STM+LTM and promote.
        /// Returns number of new unique units promoted into LTM.
        /// </summary>
        public int EndTourAndPromote(bool reachedGoal, float strengthMult = 1.0f)
        {
            UniquePromotedLastTour = 0;
            if (_pathOverflow || _tourStep > _maxPathSteps)
            {
                ToursDiscarded++;
                if (!string.IsNullOrEmpty(_activeWorldId))
                    EnsureFingerprint(_activeWorldId).FailedTours++;
                ClearStm();
                MarkEpisodeStart();
                return 0;
            }
            if (_stmCount < 1 || _tourStep < 1)
            {
                ClearStm();
                MarkEpisodeStart();
                return 0;
            }

            if (reachedGoal)
            {
                AppendSpecialMarker(false, true);
                BackfillGoalDirectionsForTour();
                int promoted = AnalyzeStmAndMaybePromote(true, Math.Max(0.1f, strengthMult));
                UniquePromotedLastTour = promoted;
                if (!string.IsNullOrEmpty(_activeWorldId))
                {
                    var fp = EnsureFingerprint(_activeWorldId);
                    fp.SuccessfulTours++;
                    fp.TotalStepsOnSuccess += _tourStep;
                }
                ClearStm();
                MarkEpisodeStart();
                return promoted;
            }

            // Failed tour: no direction backfill; weak/no promote (V15: no promote on failure)
            if (!string.IsNullOrEmpty(_activeWorldId))
                EnsureFingerprint(_activeWorldId).FailedTours++;
            ClearStm();
            MarkEpisodeStart();
            return 0;
        }

        void ExtractUniqueUnitsFromPath()
        {
            _uniqueStmKeys.Clear();
            _uniqueStmUnits.Clear();
            // Vote moves per unit key within this tour
            var votes = new Dictionary<string, float[]>();
            var senseOf = new Dictionary<string, DirCellKind[]>();
            var nearOf = new Dictionary<string, byte>();
            var dirOf = new Dictionary<string, byte>();

            for (int n = 0; n < _stmCount; n++)
            {
                int idx = (_stmHead - _stmCount + n + _stm.Length * 2) % _stm.Length;
                var t = _stm[idx];
                if (t.Sense == null) continue;
                string key = SenseMoveRule.UnitKeyFrom(t.Sense, t.GoalNearness, t.GoalDirection);
                if (key == null) continue;

                // Uniqueness vs LTM and vs already-accepted STM unique
                if (_ltmUnitKeys.Contains(key)) continue;
                if (_uniqueStmKeys.Contains(key))
                {
                    // still vote move for this unique unit
                    float[] vv;
                    if (votes.TryGetValue(key, out vv) && t.MoveDir >= 0 && t.MoveDir < 8)
                    {
                        float w = 0.4f;
                        if (t.Outcome == 0) w = 1.0f;
                        else if (t.Outcome == 1) w = 0.6f;
                        else if (t.Outcome == 2) w = 0.25f;
                        else w = 0.1f;
                        vv[t.MoveDir] += w;
                    }
                    continue;
                }

                _uniqueStmKeys.Add(key);
                senseOf[key] = (DirCellKind[])t.Sense.Clone();
                nearOf[key] = t.GoalNearness;
                dirOf[key] = t.GoalDirection;
                var v = new float[8];
                float w0 = 0.4f;
                if (t.Outcome == 0) w0 = 1.0f;
                else if (t.Outcome == 1) w0 = 0.6f;
                else if (t.Outcome == 2) w0 = 0.25f;
                else w0 = 0.1f;
                if (t.MoveDir >= 0 && t.MoveDir < 8) v[t.MoveDir] += w0;
                votes[key] = v;
            }

            foreach (var key in _uniqueStmKeys)
            {
                var v = votes[key];
                int bestMove = 0;
                float bestV = v[0];
                for (int i = 1; i < 8; i++) if (v[i] > bestV) { bestV = v[i]; bestMove = i; }
                var unit = SenseMoveRule.FromUnit(senseOf[key], nearOf[key], dirOf[key], bestMove, Math.Max(0.15f, bestV));
                _uniqueStmUnits.Add(unit);
            }
        }

        int PromoteUniqueStmIntoLtm(float strengthMult)
        {
            int promoted = 0;
            for (int i = 0; i < _uniqueStmUnits.Count; i++)
            {
                var u = _uniqueStmUnits[i];
                string key = u.UnitKey();
                if (_ltmUnitKeys.Contains(key))
                {
                    // Merge: strength++ / vote on existing
                    AppendOrMergeLtmUnit(
                        new DirCellKind[] { u.D0, u.D1, u.D2, u.D3, u.D4, u.D5, u.D6, u.D7 },
                        u.GoalNearness, u.GoalDirection, u.MoveDir, u.Strength * strengthMult);
                    continue;
                }
                AppendOrMergeLtmUnit(
                    new DirCellKind[] { u.D0, u.D1, u.D2, u.D3, u.D4, u.D5, u.D6, u.D7 },
                    u.GoalNearness, u.GoalDirection, u.MoveDir, Math.Max(0.2f, u.Strength * strengthMult));
                promoted++;
                PromoteCount++;
            }
            RecountLtm();
            return promoted;
        }

        public int TrainFromShortTerm(int passes = 3)
        {
            // V14: end-of-tour promote instead of multi-pass ring promote
            int before = LtmCount;
            EndTourAndPromote(true, 1.0f);
            return Math.Max(0, LtmCount - before);
        }

        /// <summary>
        /// V14 episode end: discard if path too long; else unique 10-slot promote into LTM.
        /// </summary>
        public void OnEpisodeEnd(bool reachedGoal, double totalEnergy, int decisionSteps)
        {
            if (decisionSteps < 1) decisionSteps = 1;
            if (_pathOverflow || decisionSteps > _maxPathSteps || _stmCount > _maxPathSteps)
            {
                ToursDiscarded++;
                ClearStm();
                MarkEpisodeStart();
                return;
            }

            double avgEnergyPerStep = totalEnergy / decisionSteps;
            double expectedEnergy = decisionSteps * 0.5;
            double energyEfficiency = reachedGoal ? (expectedEnergy / (totalEnergy + 1.0)) : 0.0;
            energyEfficiency = Math.Min(2.0, Math.Max(0.0, energyEfficiency));
            float mult = reachedGoal
                ? (float)(0.7 + 0.5 * energyEfficiency)
                : 0.4f;

            EndTourAndPromote(reachedGoal, mult);
        }

        public void OnEpisodeSuccess()
        {
            OnEpisodeEnd(true, 50.0, 100);
        }

        public void AppendOrMergeLtm(DirCellKind[] sense, int moveDir, float strength)
        {
            AppendOrMergeLtmUnit(sense, 0, 0, moveDir, strength);
        }

        public void AppendOrMergeLtmUnit(DirCellKind[] sense, byte nearness, byte goalDir, int moveDir, float strength)
        {
            if (sense == null || sense.Length != 8) return;
            string unitKey = SenseMoveRule.UnitKeyFrom(sense, nearness, goalDir);
            if (unitKey == null) return;

            for (int i = 0; i < _ltm.Length; i++)
            {
                if (!_ltm[i].Used) continue;
                if (!_ltm[i].ExactUnitMatch(sense, nearness, goalDir)) continue;
                var s = _ltm[i];
                s.Sightings++;
                // Vote: if same move, strengthen; if different, soft pull toward new move when stronger
                if (s.MoveDir == moveDir)
                {
                    s.Strength = Math.Min(8f, s.Strength + strength);
                }
                else if (strength > s.Strength * 0.85f)
                {
                    s.MoveDir = moveDir;
                    s.Strength = Math.Min(8f, (s.Strength + strength) * 0.5f + 0.25f);
                }
                else
                {
                    s.Strength = Math.Min(8f, s.Strength + strength * 0.25f);
                }
                _ltm[i] = s;
                return;
            }

            int free = -1;
            for (int i = 0; i < _ltm.Length; i++)
            {
                if (!_ltm[i].Used) { free = i; break; }
            }
            if (free < 0)
            {
                free = 0;
                float best = float.MaxValue;
                for (int i = 0; i < _ltm.Length; i++)
                {
                    float key = _ltm[i].Strength * Math.Max(1, _ltm[i].Sightings);
                    if (key < best) { best = key; free = i; }
                }
                if (_ltm[free].Used)
                    _ltmUnitKeys.Remove(_ltm[free].UnitKey());
            }
            bool wasEmpty = !_ltm[free].Used;
            _ltm[free] = SenseMoveRule.FromUnit(sense, nearness, goalDir, moveDir, strength);
            _ltmUnitKeys.Add(unitKey);
            if (wasEmpty) LtmCount++;
            else RecountLtm();
        }

        
        bool TryRecentFeelNgram(out byte[] ngram, out int nlen, out byte progressBin)
        {
            ngram = new byte[5];
            nlen = 0;
            progressBin = 1;
            int take = Math.Min(5, _tourStep);
            if (take < 3) take = Math.Min(5, _stmCount);
            if (take < 3) return false;
            int got = 0;
            // Walk backward from most recent. ProgressBin MUST be from the most recent step
            // (end of window), matching how Analyze stores ProgressBin from last step of n-gram.
            for (int n = 0; n < _stmCount && got < take; n++)
            {
                int idx = (_stmHead - 1 - n + _stm.Length * 2) % _stm.Length;
                var tr = _stm[idx];
                if (tr.IsStartMarker || tr.IsGoalMarker) continue;
                if (tr.MoveDir < 0) continue;
                ngram[take - 1 - got] = (byte)tr.Feel;
                if (got == 0) progressBin = tr.ProgressBin; // most recent only (V16 Fix D)
                got++;
            }
            if (got < 3) return false;
            // compact to leading got
            if (got < take)
            {
                byte[] tmp = new byte[got];
                Array.Copy(ngram, take - got, tmp, 0, got);
                ngram = tmp;
            }
            else
            {
                byte[] tmp = new byte[got];
                Array.Copy(ngram, 0, tmp, 0, got);
                ngram = tmp;
            }
            nlen = got;
            return true;
        }

        void BackfillGoalDirectionsForTour()
        {
            // Requires CellX/CellY + Facing on STM entries and a known goal via last marker world.
            // Goal cell is not stored on brain; teacher/robot backfill positions then call with goal via ComputeGoalDirection at ingest.
            // Here: if Cell coords present and World fingerprint exists, leave directions to caller-provided Compute at ObserveFeel cells.
            // Robot/Teacher should set cells; we compute direction if goal was passed through temporary fields.
            // Soft backfill: ProgressBin already set; GoalDirection filled when CellX valid and _backfillGoal set.
            if (!_hasBackfillGoal) return;
            for (int n = 0; n < _stmCount; n++)
            {
                int idx = (_stmHead - _stmCount + n + _stm.Length * 2) % _stm.Length;
                var tr = _stm[idx];
                if (tr.IsStartMarker || tr.IsGoalMarker) continue;
                if (tr.CellX == short.MinValue) continue;
                var cell = new Point(tr.CellX, tr.CellY);
                tr.GoalDirection = ComputeGoalDirection(cell, _backfillGoal, tr.Facing);
                tr.ProgressBin = ProgressBinFromDistance(cell, _backfillStart, _backfillGoal);
                tr.GoalNearness = tr.ProgressBin;
                _stm[idx] = tr;
            }
        }

        bool _hasBackfillGoal;
        Point _backfillGoal;
        Point _backfillStart;

        public void SetTourGoalForBackfill(Point start, Point goal)
        {
            _backfillStart = start;
            _backfillGoal = goal;
            _hasBackfillGoal = true;
        }

        public static byte ProgressBinFromDistance(Point cell, Point start, Point goal)
        {
            int total = Math.Abs(goal.X - start.X) + Math.Abs(goal.Y - start.Y);
            if (total < 1) total = 1;
            int left = Math.Abs(goal.X - cell.X) + Math.Abs(goal.Y - cell.Y);
            double done = 1.0 - Math.Min(1.0, (double)left / total);
            if (done < 0.33) return 0;
            if (done < 0.67) return 1;
            return 2;
        }

        int AnalyzeStmAndMaybePromote(bool successTour, float strengthMult)
        {
            // Extract feel n-grams length 3..5 that are rare-but-useful; prefer recurring on success.
            var counts = new Dictionary<string, int>();
            var moveVotes = new Dictionary<string, float[]>();
            var meta = new Dictionary<string, Tuple<byte[], int, byte>>(); // key -> feels,len,progress

            for (int n = 0; n < _stmCount; n++)
            {
                // gather contiguous non-marker feels in tour order
            }

            var feels = new List<byte>();
            var moves = new List<int>();
            var pbins = new List<byte>();
            for (int n = 0; n < _stmCount; n++)
            {
                int idx = (_stmHead - _stmCount + n + _stm.Length * 2) % _stm.Length;
                var tr = _stm[idx];
                if (tr.IsStartMarker || tr.IsGoalMarker) continue;
                if (tr.MoveDir < 0 || tr.MoveDir > 7) continue;
                feels.Add((byte)tr.Feel);
                moves.Add(tr.MoveDir);
                pbins.Add(tr.ProgressBin);
            }
            if (feels.Count < 3) return 0;

            for (int len = 3; len <= 5; len++)
            {
                for (int i = 0; i + len <= feels.Count; i++)
                {
                    var ng = new byte[len];
                    for (int k = 0; k < len; k++) ng[k] = feels[i + k];
                    string seq = SenseMoveRule.FeelAtlasKey("", 0, 0, (byte)len, 
                        ng[0], len > 1 ? ng[1] : (byte)255, len > 2 ? ng[2] : (byte)255, len > 3 ? ng[3] : (byte)255, len > 4 ? ng[4] : (byte)255);
                    // use seq without world/dir for counting rarity
                    string localKey = len.ToString() + ":" + string.Join("-", ng);
                    int c; counts.TryGetValue(localKey, out c); counts[localKey] = c + 1;
                    float[] votes;
                    if (!moveVotes.TryGetValue(localKey, out votes))
                    {
                        votes = new float[8];
                        moveVotes[localKey] = votes;
                        meta[localKey] = Tuple.Create(ng, len, pbins[i + len - 1]);
                    }
                    int mv = moves[i + len - 1];
                    votes[mv] += successTour ? 1.0f : 0.25f;
                }
            }

            // Prefer sequences that recur (count>=2) on success; allow count==1 only on success end with low strength
            int promoted = 0;
            foreach (var kv in counts)
            {
                int c = kv.Value;
                if (!successTour && c < 2) continue;
                if (successTour && c < 1) continue;
                // rarity: skip ultra-common (c > feels.Count/2)
                if (c > Math.Max(3, feels.Count / 2)) continue;

                var tup = meta[kv.Key];
                byte[] ng = tup.Item1;
                int len = tup.Item2;
                byte pbin = tup.Item3;
                var votes = moveVotes[kv.Key];
                int bestMove = 0; float bestV = votes[0];
                for (int i = 1; i < 8; i++) if (votes[i] > bestV) { bestV = votes[i]; bestMove = i; }
                if (bestV < 0.2f) continue;

                float str = (successTour ? 1.0f : 0.35f) * strengthMult;
                if (c >= 2) str *= 1.25f;
                byte gdir = GoalDirUnset;
                // if success, try to use backfilled direction from last step of ngram window — approximate via matching STM entry
                if (successTour) gdir = FindBackfilledDirNear(ng, len);

                string world = _activeWorldId ?? "";
                // V16 Fix E: SUCCESS-ONLY promote — never AppendOrMergeLtm on mid-tour analyze.
                if (!successTour) continue;
                var rule = SenseMoveRule.FromFeelNgram(ng, len, pbin, gdir, bestMove, Math.Max(0.2f, str), world);
                string key = rule.UnitKey();
                if (!_ltmUnitKeys.Contains(key))
                {
                    AppendOrMergeLtmFeel(rule);
                    promoted++;
                    PromoteCount++;
                }
                else
                {
                    AppendOrMergeLtmFeel(rule);
                }
                if (!string.IsNullOrEmpty(world))
                    EnsureFingerprint(world).AddNgram(kv.Key);
            }
            RecountLtm();
            return promoted;
        }

        byte FindBackfilledDirNear(byte[] ng, int len)
        {
            // scan stm for matching feel window with GoalDirection set
            for (int n = 0; n + len <= _stmCount; n++)
            {
                bool ok = true;
                byte gdir = GoalDirUnset;
                for (int k = 0; k < len; k++)
                {
                    int idx = (_stmHead - _stmCount + n + k + _stm.Length * 2) % _stm.Length;
                    var tr = _stm[idx];
                    if (tr.IsStartMarker || tr.IsGoalMarker) { ok = false; break; }
                    if ((byte)tr.Feel != ng[k]) { ok = false; break; }
                    gdir = tr.GoalDirection;
                }
                if (ok && gdir != GoalDirUnset) return gdir;
            }
            return GoalDirUnset;
        }

        void AppendOrMergeLtmFeel(SenseMoveRule rule)
        {
            string key = rule.UnitKey();
            for (int i = 0; i < _ltm.Length; i++)
            {
                if (!_ltm[i].Used) continue;
                if (_ltm[i].UnitKey() != key) continue;
                var s = _ltm[i];
                s.Sightings++;
                if (s.MoveDir == rule.MoveDir)
                    s.Strength = Math.Min(8f, s.Strength + rule.Strength);
                else if (rule.Strength > s.Strength * 0.85f)
                {
                    s.MoveDir = rule.MoveDir;
                    s.Strength = Math.Min(8f, (s.Strength + rule.Strength) * 0.5f + 0.25f);
                }
                else
                    s.Strength = Math.Min(8f, s.Strength + rule.Strength * 0.25f);
                // adopt goal dir if previously unset
                if (s.GoalDirection == GoalDirUnset && rule.GoalDirection != GoalDirUnset)
                    s.GoalDirection = rule.GoalDirection;
                _ltm[i] = s;
                return;
            }
            int free = -1;
            for (int i = 0; i < _ltm.Length; i++) if (!_ltm[i].Used) { free = i; break; }
            if (free < 0)
            {
                free = 0; float best = float.MaxValue;
                for (int i = 0; i < _ltm.Length; i++)
                {
                    float keyv = _ltm[i].Strength * Math.Max(1, _ltm[i].Sightings);
                    if (keyv < best) { best = keyv; free = i; }
                }
                if (_ltm[free].Used) _ltmUnitKeys.Remove(_ltm[free].UnitKey());
            }
            bool wasEmpty = !_ltm[free].Used;
            _ltm[free] = rule;
            _ltmUnitKeys.Add(key);
            if (wasEmpty) LtmCount++; else RecountLtm();
        }


        void RecountLtm()
        {
            int n = 0;
            _ltmUnitKeys.Clear();
            for (int i = 0; i < _ltm.Length; i++)
            {
                if (!_ltm[i].Used) continue;
                n++;
                _ltmUnitKeys.Add(_ltm[i].UnitKey());
            }
            LtmCount = n;
        }

        public double DirectionWeight(DirCellKind[] sense, int moveDir) { return DirectionWeight(sense, moveDir, 0); }

        public double DirectionWeight(DirCellKind[] sense, int moveDir, int facingDir)
        {
            double confidence;
            return DirectionWeight(sense, moveDir, facingDir, out confidence);
        }

        public double DirectionWeight(DirCellKind[] sense, int moveDir, int facingDir, out double confidence)
        {
            return DirectionWeight(sense, moveDir, facingDir, -1, -1, out confidence);
        }

        /// <summary>
        /// V16 feel-atlas LTM bias: match recent feel n-gram; prefer goal-direction agreement
        /// (weighted higher than progress). goalNearness/goalDirection &lt; 0 means unknown (dest-hidden).
        /// Obsolete 8-neigh rules (NgramLen&lt;3) are skipped.
        /// </summary>
        public double DirectionWeight(DirCellKind[] sense, int moveDir, int facingDir,
            int goalNearness, int goalDirection, out double confidence)
        {
            confidence = 0;
            int relMove = RelativeMoveFromAbsolute(facingDir, moveDir);

            // Recent feel n-gram from STM (+ last moves are encoded via preferred move on rules)
            byte[] ngram;
            int nlen;
            byte progressBin;
            if (!TryRecentFeelNgram(out ngram, out nlen, out progressBin) || nlen < 3)
            {
                confidence = 0;
                return 1.0;
            }

            string active = _activeWorldId ?? "";
            double bestAgree = 0, bestConflict = 0;
            int hits = 0;
            MatchAttempts++;

            // Prefer active WorldId shard; allow weaker cross-shard matches.
            // V15 loose match: Hamming/edit on feel n-gram (not exact-only); adjacent progress bins OK.
            for (int pass = 0; pass < 2; pass++)
            {
                bool requireWorld = (pass == 0 && !string.IsNullOrEmpty(active));
                for (int i = 0; i < _ltm.Length; i++)
                {
                    if (!_ltm[i].Used || _ltm[i].NgramLen < 3) continue;
                    if (requireWorld)
                    {
                        if (!string.Equals(_ltm[i].WorldId, active, StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    else if (!string.IsNullOrEmpty(active) &&
                             string.Equals(_ltm[i].WorldId, active, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // already counted in pass 0
                    }

                    int ruleLen = _ltm[i].NgramLen;
                    if (ruleLen > nlen) continue;
                    int maxHam = ruleLen <= 3 ? 1 : (ruleLen == 4 ? 1 : 2);
                    int bestHam = int.MaxValue;
                    // Try suffix align first, then any window in recent n-gram (partial overlap)
                    for (int off = 0; off + ruleLen <= nlen; off++)
                    {
                        int ham = 0;
                        for (int k = 0; k < ruleLen; k++)
                            if (_ltm[i].GetFeel(k) != ngram[off + k]) ham++;
                        if (ham < bestHam) bestHam = ham;
                        if (bestHam == 0) break;
                    }
                    // Prefer suffix: if suffix ham is within max, use it; else best window
                    {
                        int suffOff = nlen - ruleLen;
                        int suffHam = 0;
                        for (int k = 0; k < ruleLen; k++)
                            if (_ltm[i].GetFeel(k) != ngram[suffOff + k]) suffHam++;
                        if (suffHam <= maxHam) bestHam = suffHam;
                    }
                    if (bestHam > maxHam) continue;

                    hits++;
                    var rule = _ltm[i];
                    double dirAgree = 1.0;
                    double progAgree = 1.0;
                    if (goalDirection >= 0 && goalDirection <= 7 && rule.GoalDirection != GoalDirUnset)
                    {
                        int dd = Math.Abs(rule.GoalDirection - goalDirection);
                        if (dd > 4) dd = 8 - dd;
                        dirAgree = 1.0 - 0.2 * dd;
                    }
                    else
                    {
                        dirAgree = 0.55; // no labeled direction yet
                    }
                    int pd = Math.Abs(rule.ProgressBin - progressBin);
                    // Adjacent progress bins nearly as good (soft / ignore hard bin equality)
                    progAgree = 1.0 - 0.12 * pd;

                    // Direction weighted higher than progress/nearness when present
                    double situ = (2.5 * dirAgree + 1.0 * progAgree) / 3.5;
                    if (pass == 1) situ *= 0.55; // weaker cross-shard
                    situ *= (1.0 - 0.22 * bestHam); // soft penalty for Hamming distance
                    double score = situ * Math.Min(4.0, rule.Strength);
                    if (rule.MoveDir == relMove)
                        bestAgree = Math.Max(bestAgree, score);
                    else
                        bestConflict = Math.Max(bestConflict, score * 0.85);
                }
                if (hits > 0 && requireWorld) break;
            }
            if (hits > 0) MatchHits++;

            if (hits == 0)
            {
                confidence = 0;
                return 1.0;
            }
            confidence = Math.Min(1.0, 0.50 + 0.12 * Math.Min(4.0, bestAgree > 0 ? bestAgree : bestConflict));
            if (bestAgree >= bestConflict && bestAgree > 0)
            {
                MatchSteerApplied++;
                return 1.0 + 0.9 * bestAgree;
            }
            if (bestConflict > 0)
            {
                MatchSteerApplied++;
                return Math.Pow(0.35, Math.Min(3.0, bestConflict));
            }
            return 1.0;
        }

        // Obsolete 8-neigh STM match (V14); V16 decision path uses feel n-grams only. Kept for API/compat.
        [Obsolete("V16 uses feel-atlas DirectionWeight; 8-neigh MatchStm is unused.")]
        double MatchStm(DirCellKind[] sense, int moveDir)
        {
            int take = Math.Min(32, _stmCount);
            if (take == 0) return 1.0;
            int agree = 0, conflict = 0, seen = 0;
            for (int n = 0; n < take; n++)
            {
                int idx = (_stmHead - 1 - n + _stm.Length * 2) % _stm.Length;
                var t = _stm[idx];
                if (t.Sense == null) continue;
                int h = 0;
                for (int i = 0; i < 8; i++) if (t.Sense[i] != sense[i]) h++;
                if (h > MaxHamming) continue;
                seen++;
                if (t.MoveDir == moveDir) agree++; else conflict++;
            }
            if (seen == 0) return 1.0;
            double ratio = (agree + 0.5) / (agree + conflict + 1.0);
            return 0.5 + ratio;
        }

        public double DirectionWeight(Point from, int dx, int dy) { return 1.0; }

        public void RecordAttempt(Point from, Point to, int dx, int dy) { }
        public void MarkHitObstacle() { }
        public void MarkWentRough() { }
        public void MarkKeptSmooth() { }

        public string StatusLine()
        {
            return string.Format("STM {0}/{1} (ovf/disc {2}/{3}) | LTM {4}/{5} | promotes {6} | uniqLast {7} | maxPath {8}",
                StmCount, StmCapacity, StmDropCount, ToursDiscarded, LtmCount, LtmCapacity, PromoteCount,
                UniquePromotedLastTour, MaxPathSteps);
        }

        public void SaveToFile(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var sb = new StringBuilder();
            sb.AppendLine(PolicyVersion);
            sb.AppendLine("mem_format," + MemFormatVersion);
            sb.AppendLine("stm_cap," + StmCapacity);
            sb.AppendLine("ltm_cap," + LtmCapacity);
            sb.AppendLine("max_path," + _maxPathSteps);
            sb.AppendLine("tick," + _tick);
            sb.AppendLine("active_world," + (_activeWorldId ?? ""));
            sb.AppendLine("stm_drop," + StmDropCount);
            sb.AppendLine("tours_discarded," + ToursDiscarded);
            sb.AppendLine("promote," + PromoteCount);
            sb.AppendLine("smooth," + SmoothCount);
            sb.AppendLine("rough," + RoughCount);
            sb.AppendLine("obs," + ObstacleBumpCount);
            // V16 Fix B: STM is scratch — do not persist incomplete STM; LTM is primary.
            sb.AppendLine("BEGIN_STM");
            sb.AppendLine("END_STM");
            sb.AppendLine("BEGIN_LTM");
            for (int i = 0; i < _ltm.Length; i++)
            {
                if (!_ltm[i].Used) continue;
                var r = _ltm[i];
                if (r.NgramLen >= 3)
                {
                    // FEEL,move,strength,sightings,nlen,f0,f1,f2,f3,f4,progressBin,goalNear,goalDir,worldId
                    sb.Append("FEEL,").Append(r.MoveDir).Append(',').Append(r.Strength.ToString("0.###")).Append(',')
                      .Append(r.Sightings).Append(',').Append(r.NgramLen).Append(',')
                      .Append(r.F0).Append(',').Append(r.F1).Append(',').Append(r.F2).Append(',')
                      .Append(r.F3).Append(',').Append(r.F4).Append(',')
                      .Append(r.ProgressBin).Append(',').Append(r.GoalNearness).Append(',').Append(r.GoalDirection)
                      .Append(',').Append(EscapeCsv(r.WorldId ?? ""));
                    sb.AppendLine();
                }
                else
                {
                    // Obsolete 8-neigh-only (ignored by V16 feel decision path; kept for migration/debug)
                    sb.Append("NEIGH,").Append(r.MoveDir).Append(',').Append(r.Strength.ToString("0.###")).Append(',')
                      .Append(r.Sightings).Append(',');
                    for (int k = 0; k < 8; k++)
                    {
                        if (k > 0) sb.Append('-');
                        sb.Append((int)r.Get(k));
                    }
                    sb.Append(',').Append(r.GoalNearness).Append(',').Append(r.GoalDirection);
                    sb.AppendLine();
                }
            }
            sb.AppendLine("END_LTM");
            sb.AppendLine("BEGIN_FP");
            foreach (var kv in _worldFingerprints)
            {
                var fp = kv.Value;
                sb.Append("FP,").Append(EscapeCsv(fp.WorldId ?? "")).Append(',')
                  .Append(fp.SuccessfulTours).Append(',').Append(fp.FailedTours).Append(',')
                  .Append(fp.TotalStepsOnSuccess);
                sb.AppendLine();
            }
            sb.AppendLine("END_FP");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace(",", "\\,");
        }

        static string UnescapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length) { sb.Append(s[i + 1]); i++; }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }

        public static BrainMemory LoadFromFile(string path)
        {
            if (!File.Exists(path)) return new BrainMemory();
            var lines = File.ReadAllLines(path);
            int stmCap = DefaultStmCapacity, ltmCap = DefaultLtmCapacity, maxPath = DefaultStmCapacity;
            long tick = 0;
            int drop = 0, promote = 0, smooth = 0, rough = 0, obs = 0, discarded = 0;
            int memFormat = 1; // legacy V15 = 1 (8-neigh only); V16 = 2
            string activeWorld = "";
            string headerPolicy = "";
            if (lines.Length > 0) headerPolicy = lines[0].Trim();
            foreach (var line in lines)
            {
                if (line.StartsWith("mem_format,")) int.TryParse(line.Substring(11), out memFormat);
                else if (line.StartsWith("stm_cap,")) int.TryParse(line.Substring(8), out stmCap);
                else if (line.StartsWith("ltm_cap,")) int.TryParse(line.Substring(8), out ltmCap);
                else if (line.StartsWith("max_path,")) int.TryParse(line.Substring(9), out maxPath);
                else if (line.StartsWith("tick,")) long.TryParse(line.Substring(5), out tick);
                else if (line.StartsWith("active_world,")) activeWorld = line.Substring(13);
                else if (line.StartsWith("stm_drop,")) int.TryParse(line.Substring(9), out drop);
                else if (line.StartsWith("tours_discarded,")) int.TryParse(line.Substring(16), out discarded);
                else if (line.StartsWith("promote,")) int.TryParse(line.Substring(8), out promote);
                else if (line.StartsWith("smooth,")) int.TryParse(line.Substring(7), out smooth);
                else if (line.StartsWith("rough,")) int.TryParse(line.Substring(6), out rough);
                else if (line.StartsWith("obs,")) int.TryParse(line.Substring(4), out obs);
            }
            if (headerPolicy.IndexOf("v16", StringComparison.OrdinalIgnoreCase) >= 0 && memFormat < 2)
                memFormat = 2;
            if (stmCap < DefaultStmCapacity) stmCap = DefaultStmCapacity;
            var brain = new BrainMemory(stmCap, ltmCap);
            brain._tick = tick;
            brain.StmDropCount = drop;
            brain.PromoteCount = promote;
            brain.SmoothCount = smooth;
            brain.RoughCount = rough;
            brain.ObstacleBumpCount = obs;
            brain.ToursDiscarded = discarded;
            brain._maxPathSteps = Math.Min(maxPath > 0 ? maxPath : stmCap, brain._stm.Length);
            brain._activeWorldId = activeWorld ?? "";

            bool inLtm = false, inFp = false;
            foreach (var line in lines)
            {
                if (line == "BEGIN_LTM") { inLtm = true; inFp = false; continue; }
                if (line == "END_LTM") { inLtm = false; continue; }
                if (line == "BEGIN_FP") { inFp = true; inLtm = false; continue; }
                if (line == "END_FP") { inFp = false; continue; }
                if (line == "BEGIN_STM" || line == "END_STM") continue;
                if (inLtm)
                {
                    if (line.StartsWith("FEEL,", StringComparison.Ordinal))
                    {
                        // FEEL,move,strength,sightings,nlen,f0,f1,f2,f3,f4,progressBin,goalNear,goalDir,worldId
                        var sp = SplitCsv(line);
                        if (sp.Length < 13) continue;
                        int mv, sight, nlen; float str;
                        byte f0, f1, f2, f3, f4, pbin, near, gdir;
                        if (!int.TryParse(sp[1], out mv)) continue;
                        if (!float.TryParse(sp[2], out str)) continue;
                        if (!int.TryParse(sp[3], out sight)) continue;
                        if (!int.TryParse(sp[4], out nlen)) continue;
                        if (!byte.TryParse(sp[5], out f0)) continue;
                        if (!byte.TryParse(sp[6], out f1)) continue;
                        if (!byte.TryParse(sp[7], out f2)) continue;
                        if (!byte.TryParse(sp[8], out f3)) continue;
                        if (!byte.TryParse(sp[9], out f4)) continue;
                        if (!byte.TryParse(sp[10], out pbin)) continue;
                        if (!byte.TryParse(sp[11], out near)) continue;
                        if (!byte.TryParse(sp[12], out gdir)) continue;
                        string wid = sp.Length >= 14 ? UnescapeCsv(sp[13]) : "";
                        var feels = new byte[] { f0, f1, f2, f3, f4 };
                        var rule = SenseMoveRule.FromFeelNgram(feels, nlen, pbin, gdir, mv, str, wid);
                        rule.Sightings = Math.Max(1, sight);
                        rule.GoalNearness = near;
                        brain.AppendOrMergeLtmFeelPublic(rule);
                    }
                    else if (line.StartsWith("NEIGH,", StringComparison.Ordinal))
                    {
                        // Obsolete 8-neigh: load but NgramLen=0 so V16 feel path ignores them.
                        var sp = line.Substring(6).Split(',');
                        if (sp.Length < 4) continue;
                        int mv, sight; float str;
                        if (!int.TryParse(sp[0], out mv)) continue;
                        if (!float.TryParse(sp[1], out str)) continue;
                        if (!int.TryParse(sp[2], out sight)) continue;
                        var kinds = ParseSense(sp[3]);
                        if (kinds == null) continue;
                        byte near = 0, gdir = 0;
                        if (sp.Length >= 6)
                        {
                            byte.TryParse(sp[4], out near);
                            byte.TryParse(sp[5], out gdir);
                        }
                        brain.AppendOrMergeLtmUnit(kinds, near, gdir, mv, str);
                    }
                    else
                    {
                        // Legacy V15 format: move,strength,sightings,d0-..-d7,near,gdir (no FEEL prefix)
                        // Ignore for V16 decision path (8-neigh only, no feel atlas fields).
                        var sp = line.Split(',');
                        if (sp.Length < 4) continue;
                        int mv, sight; float str;
                        if (!int.TryParse(sp[0], out mv)) continue;
                        if (!float.TryParse(sp[1], out str)) continue;
                        if (!int.TryParse(sp[2], out sight)) continue;
                        var kinds = ParseSense(sp[3]);
                        if (kinds == null) continue;
                        byte near = 0, gdir = 0;
                        if (sp.Length >= 6)
                        {
                            byte.TryParse(sp[4], out near);
                            byte.TryParse(sp[5], out gdir);
                        }
                        brain.AppendOrMergeLtmUnit(kinds, near, gdir, mv, str);
                    }
                }
                else if (inFp)
                {
                    if (!line.StartsWith("FP,", StringComparison.Ordinal)) continue;
                    var sp = SplitCsv(line);
                    if (sp.Length < 5) continue;
                    string wid = UnescapeCsv(sp[1]);
                    int st, ft, tss;
                    int.TryParse(sp[2], out st);
                    int.TryParse(sp[3], out ft);
                    int.TryParse(sp[4], out tss);
                    var fp = brain.EnsureFingerprintPublic(wid);
                    fp.SuccessfulTours = st;
                    fp.FailedTours = ft;
                    fp.TotalStepsOnSuccess = tss;
                }
            }
            brain.RecountLtm();
            // V16 Fix B: STM is scratch — always clear after load (even if file had STM).
            brain.ClearStm();
            return brain;
        }

        /// <summary>Public wrapper for LoadFromFile feel-rule merge.</summary>
        public void AppendOrMergeLtmFeelPublic(SenseMoveRule rule) { AppendOrMergeLtmFeel(rule); }
        public WorldFingerprint EnsureFingerprintPublic(string worldId) { return EnsureFingerprint(worldId); }

        static string[] SplitCsv(string line)
        {
            var list = new List<string>();
            var cur = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\\' && i + 1 < line.Length) { cur.Append(line[i + 1]); i++; continue; }
                if (line[i] == ',') { list.Add(cur.ToString()); cur.Length = 0; continue; }
                cur.Append(line[i]);
            }
            list.Add(cur.ToString());
            return list.ToArray();
        }

        static DirCellKind[] ParseSense(string dash)
        {
            var p = dash.Split('-');
            if (p.Length != 8) return null;
            var s = new DirCellKind[8];
            for (int i = 0; i < 8; i++)
            {
                int v;
                if (!int.TryParse(p[i], out v) || v < 0 || v > 5) return null;
                s[i] = (DirCellKind)v;
            }
            return s;
        }

        public static string DefaultMemoryPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectMem = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "memory-dat", "brain.mem"));
            string workspaceMem = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "memory-dat", "brain.mem"));
            try
            {
                string dir = Path.GetDirectoryName(workspaceMem);
                if (dir != null && dir.IndexOf("LifeSim", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Directory.CreateDirectory(dir);
                    return workspaceMem;
                }
            }
            catch { }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(projectMem));
                return projectMem;
            }
            catch
            {
                string local = Path.Combine(baseDir, "memory-dat", "brain.mem");
                Directory.CreateDirectory(Path.GetDirectoryName(local));
                return local;
            }
        }
    }
}
