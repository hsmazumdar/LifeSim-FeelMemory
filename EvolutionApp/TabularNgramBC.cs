using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Evolution
{
    /// <summary>
    /// Tabular n-gram → action behavior-cloning baseline (baseline Phase A/B/C).
    /// Trains from the SAME TeacherSuccess trajectories used to fill LTM
    /// (feel at SOURCE cell, Smooth/Medium/Rough/Obstacle via MotorMemory thresholds,
    /// n-gram lengths 3..5, action = relative move at END of window).
    /// Flat count / majority table — NO success-weighting LTM promotion.
    /// At test: exact key lookup (prefer longest 5→3); miss → EmptyLTM reactive.
    /// Mechanism name in logs: TabularNgramBC
    /// </summary>
    public sealed class TabularNgramBC
    {
        public const string MechanismName = "TabularNgramBC";
        public const int MinNgram = 3;
        public const int MaxNgram = 5;

        // key -> vote counts for relative moves 0..7
        readonly Dictionary<string, int[]> _votes = new Dictionary<string, int[]>();
        public int KeyCount { get { return _votes.Count; } }
        public int TotalVotes { get; private set; }
        public int PathsIngested { get; private set; }
        public int StepsIngested { get; private set; }

        public void Clear()
        {
            _votes.Clear();
            TotalVotes = 0;
            PathsIngested = 0;
            StepsIngested = 0;
        }

        /// <summary>Identical feel encoding to TeacherPrimitive.IngestPath (source cell).</summary>
        public static FeelCode FeelAtSource(World world, Point cell)
        {
            if (world == null) return FeelCode.Medium;
            if (world.CellHasObstacle(cell)) return FeelCode.Obstacle;
            float r = world.CellRoughness(cell);
            if (r <= MotorMemory.SmoothThreshold) return FeelCode.Smooth;
            if (r >= MotorMemory.RoughThreshold) return FeelCode.Rough;
            return FeelCode.Medium;
        }

        public static string MakeKey(byte[] feels, int offset, int len)
        {
            var sb = new StringBuilder(8 + len * 2);
            sb.Append('L').Append(len).Append(':');
            for (int k = 0; k < len; k++)
            {
                if (k > 0) sb.Append('-');
                sb.Append(feels[offset + k]);
            }
            return sb.ToString();
        }

        public static string MakeKey(IList<byte> feels)
        {
            if (feels == null || feels.Count < MinNgram) return null;
            int len = Math.Min(MaxNgram, feels.Count);
            var arr = new byte[len];
            int start = feels.Count - len;
            for (int i = 0; i < len; i++) arr[i] = feels[start + i];
            return MakeKey(arr, 0, len);
        }

        public void IngestPath(TeacherPrimitive.AttemptResult att, World world)
        {
            if (att == null || !att.ReachedGoal || att.Path == null || att.Path.Count < MinNgram) return;
            int n = att.Path.Count;
            var feels = new byte[n];
            var moves = new int[n];
            for (int i = 0; i < n; i++)
            {
                var s = att.Path[i];
                feels[i] = (byte)FeelAtSource(world, s.Cell);
                moves[i] = s.MoveDir;
            }
            for (int len = MinNgram; len <= MaxNgram; len++)
            {
                for (int i = 0; i + len <= n; i++)
                {
                    string key = MakeKey(feels, i, len);
                    int mv = moves[i + len - 1];
                    if (mv < 0 || mv > 7) continue;
                    int[] votes;
                    if (!_votes.TryGetValue(key, out votes))
                    {
                        votes = new int[8];
                        _votes[key] = votes;
                    }
                    votes[mv] += 1; // flat count — no energy/success boost beyond being on a success path
                    TotalVotes++;
                }
            }
            PathsIngested++;
            StepsIngested += n;
        }

        /// <summary>
        /// Train from TeacherSuccess on worlds (Dijkstra + noisy attempts), same protocol as LTM fill.
        /// Does NOT touch BrainMemory / LTM.
        /// </summary>
        public TeacherPopulateStats TrainFromWorlds(
            IReadOnlyList<World> worlds,
            int attemptsPerWorld,
            int maxSteps,
            Random rng,
            Action<string> progress = null)
        {
            if (rng == null) rng = new Random();
            if (attemptsPerWorld < 1) attemptsPerWorld = 200;
            var stats = new TeacherPopulateStats();
            Clear();
            foreach (var world in worlds)
            {
                if (progress != null) progress("TabularNgramBC " + world.Name + "...");
                int successes = 0, failures = 0;
                var opt = TeacherPrimitive.DijkstraSuccess(world);
                if (opt.ReachedGoal)
                {
                    IngestPath(opt, world);
                    successes++;
                    stats.SuccessPaths++;
                    stats.SuccessSteps += opt.Path.Count;
                }
                for (int a = 0; a < attemptsPerWorld; a++)
                {
                    double temp = 0.25 + rng.NextDouble() * 0.55;
                    var att = TeacherPrimitive.TryReachGoal(world, rng, maxSteps, temp);
                    if (att.ReachedGoal && att.Path != null && att.Path.Count > 0)
                    {
                        IngestPath(att, world);
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
            stats.RulesWritten = KeyCount;
            stats.LtmCount = KeyCount;
            stats.PreferredSignatures = TotalVotes;
            return stats;
        }

        /// <summary>
        /// Exact-key lookup. Prefers longest n-gram (5→3) with a strict majority (or any unique max).
        /// Returns false on miss → caller must fall back to EmptyLTM reactive.
        /// </summary>
        public bool TryLookup(IList<byte> recentFeelsOldestFirst, out int relativeMove, out int support, out string matchedKey)
        {
            relativeMove = -1;
            support = 0;
            matchedKey = null;
            if (recentFeelsOldestFirst == null || recentFeelsOldestFirst.Count < MinNgram) return false;
            int n = recentFeelsOldestFirst.Count;
            var arr = new byte[Math.Min(MaxNgram, n)];
            for (int len = Math.Min(MaxNgram, n); len >= MinNgram; len--)
            {
                int offsetHist = n - len;
                for (int k = 0; k < len; k++) arr[k] = recentFeelsOldestFirst[offsetHist + k];
                string key = MakeKey(arr, 0, len);
                int[] votes;
                if (!_votes.TryGetValue(key, out votes)) continue;
                int best = 0, bestV = votes[0], sum = votes[0];
                for (int i = 1; i < 8; i++)
                {
                    sum += votes[i];
                    if (votes[i] > bestV) { bestV = votes[i]; best = i; }
                }
                if (bestV <= 0) continue;
                relativeMove = best;
                support = bestV;
                matchedKey = key;
                return true;
            }
            return false;
        }

        public string StatusLine()
        {
            return string.Format("{0} keys={1} votes={2} paths={3} steps={4}",
                MechanismName, KeyCount, TotalVotes, PathsIngested, StepsIngested);
        }
    }
}
