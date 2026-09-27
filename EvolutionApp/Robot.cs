using System;
using System.Collections.Generic;
using System.Drawing;

namespace Evolution
{
    /// <summary>
    /// V14 Robot: path-history STM + unique 10-slot LTM; brain-vs-motor split.
    /// When UseBrain==true: LTM leads only when match confidence is high; otherwise keep strong terrain bias.
    /// When UseBrain==false: keep V10-style terrain/motor biases (strong no-brain baseline).
    /// On goal and timeout: call Brain.OnEpisodeEnd(reachedGoal, totalEnergy, decisionSteps).
    /// </summary>
    public sealed class Robot
    {
        public enum Phase
        {
            TwoMemory
        }

        static readonly int[] DirDx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        static readonly int[] DirDy = { -1, -1, 0, 1, 1, 1, 0, -1 };

        public Point Cell { get; private set; }
        public Phase CurrentPhase { get; private set; }
        public int ObstacleHits { get; private set; }
        public int GoalTouches { get; private set; }
        public bool IsOnGoal { get; private set; }

        public bool PendingObstacleExit { get; private set; }

        public int Facing { get; private set; }

        public MotorMemory Motor { get; } = new MotorMemory();
        BrainMemory _brain = new BrainMemory();
        public BrainMemory Brain { get { return _brain; } }
        public bool ClearBrainOnReset { get; set; } = false;
        public List<StepTrace> Trace { get; } = new List<StepTrace>();
        public bool RecordSenseTrace { get; set; }
        public List<SenseMoveTrace> SenseTrace { get; } = new List<SenseMoveTrace>();

        public void SetBrain(BrainMemory brain)
        {
            if (brain == null) throw new ArgumentNullException("brain");
            _brain = brain;
        }

        public MotorMemory.Feel LastFeel { get; private set; }
        public int MemoryCount => Motor.Count;

        public bool UseBrain { get; set; } = true;
        public bool LearnEnabled { get; set; } = true;

        /// <summary>V13 dest-hidden arm: no goal-coordinate bias / FaceToward(goal). Arrival detection still works.</summary>
        public bool HideDestination { get; set; } = false;

        /// <summary> tabular n-gram BC policy. When set, exact-key lookup steers; miss = EmptyLTM reactive.</summary>
        public TabularNgramBC TabularBC { get; set; }
        public int BcHits { get; private set; }
        public int BcMisses { get; private set; }
        readonly List<byte> _bcFeelHist = new List<byte>(64);

        public int StepsSmooth { get; private set; }
        public int StepsMedium { get; private set; }
        public int StepsRough { get; private set; }
        public int StepsOnObstacle { get; private set; }
        public int DecisionSteps { get; private set; }

        public double TotalEnergy { get; private set; }

        readonly Dictionary<int, int> _visitStep = new Dictionary<int, int>();
        readonly Dictionary<int, int> _visitCount = new Dictionary<int, int>();
        Point _prevCell;
        public bool RevisitInhibition { get; set; } = true;
        public double RevisitPenalty { get; set; } = 0.72;
        public int RevisitHorizon { get; set; } = 48;

        readonly Random _rng;
        double _moveCredit;
        Point _cellBeforeObstacle;

        public const double TimerHz = 50.0;

        public Robot(Point startCell, Random rng = null)
        {
            Cell = startCell;
            CurrentPhase = Phase.TwoMemory;
            _rng = rng ?? new Random();
            LastFeel = MotorMemory.Feel.Unknown;
            Facing = 0;
        }

        public void Reset(Point startCell)
        {
            Cell = startCell;
            CurrentPhase = Phase.TwoMemory;
            ObstacleHits = 0;
            GoalTouches = 0;
            IsOnGoal = false;
            PendingObstacleExit = false;
            Facing = 0;
            _cellBeforeObstacle = startCell;
            Motor.Clear();
            Trace.Clear();
            SenseTrace.Clear();
            if (ClearBrainOnReset)
            {
                Brain.Clear();
            }
            _moveCredit = 0;
            StepsSmooth = 0;
            StepsMedium = 0;
            StepsRough = 0;
            StepsOnObstacle = 0;
            DecisionSteps = 0;
            TotalEnergy = 0;
            ClearVisits();
            _prevCell = startCell;
            NoteVisit(startCell);
            LastFeel = MotorMemory.Feel.Unknown;
            _bcFeelHist.Clear();
            BcHits = 0;
            BcMisses = 0;
            Brain.MarkEpisodeStart();
        }

        public double CurrentStepsPerSec(World world)
        {
            var feel = Motor.Observe(world, Cell);
            return MotorMemory.StepsPerSec(feel);
        }

        public bool Tick(World world)
        {
            if (world == null)
            {
                return false;
            }

            LastFeel = Motor.Observe(world, Cell);
            double rate = MotorMemory.StepsPerSec(LastFeel);
            _moveCredit += rate / TimerHz;

            bool changed = false;
            if (_moveCredit >= 1.0)
            {
                _moveCredit -= 1.0;

                if (PendingObstacleExit)
                {
                    changed = ExitObstacle(world);
                }
                else
                {
                    changed = StepWeightedRealistic(world);
                }
            }
            return changed;
        }

        void NoteFeelStep(MotorMemory.Feel feel, World world)
        {
            DecisionSteps++;
            float energy = world.CellEnergyCost(Cell);
            TotalEnergy += energy;

            if (feel == MotorMemory.Feel.Smooth)
            {
                StepsSmooth++;
            }
            else if (feel == MotorMemory.Feel.Obstacle)
            {
                StepsOnObstacle++;
            }
            else if (feel == MotorMemory.Feel.Rough)
            {
                StepsRough++;
            }
            else
            {
                StepsMedium++;
            }
        }

        bool ExitObstacle(World world)
        {
            Cell = _cellBeforeObstacle;
            PendingObstacleExit = false;
            LastFeel = Motor.Observe(world, Cell);
            NoteFeelStep(LastFeel, world);
            IsOnGoal = Cell == world.GoalCell;
            if (IsOnGoal)
            {
                GoalTouches++;
            }
            return true;
        }

        bool StepWeightedRealistic(World world)
        {
            if (DecisionSteps == 0)
                Brain.ConfigureForWorld(world);
            DirCellKind[] sense = BrainMemory.Sense8(world, Cell);
            int chosen = PickDirection(world, sense);
            if (chosen < 0)
            {
                ObstacleHits++;
                return false;
            }

            int moveDx = DirDx[chosen];
            int moveDy = DirDy[chosen];
            Point from = Cell;
            Point to = new Point(from.X + moveDx, from.Y + moveDy);

            Brain.RecordAttempt(from, to, moveDx, moveDy);
            if (RecordSenseTrace && sense != null)
            {
                SenseTrace.Add(new SenseMoveTrace { Sense = sense, MoveDir = chosen });
            }
            _prevCell = from;
            Cell = to;
            NoteVisit(Cell);

            var feel = Motor.Observe(world, Cell);
            LastFeel = feel;
            NoteFeelStep(feel, world);

            byte outcome = FeelToOutcome(feel);
            float energyDelta = world.CellEnergyCost(Cell);

            if (feel == MotorMemory.Feel.Obstacle)
            {
                ObstacleHits++;
                _cellBeforeObstacle = from;
                PendingObstacleExit = true;
                Trace.Add(new StepTrace { From = from, Dx = moveDx, Dy = moveDy, Outcome = StepOutcome.Obstacle });
                Brain.MarkHitObstacle();
                if ((LearnEnabled || UseBrain) && sense != null) ObserveWithGoal(world, sense, chosen, 3, energyDelta);
                IsOnGoal = false;
                return true;
            }

            if (feel == MotorMemory.Feel.Rough)
            {
                Trace.Add(new StepTrace { From = from, Dx = moveDx, Dy = moveDy, Outcome = StepOutcome.Rough });
                Brain.MarkWentRough();
                if ((LearnEnabled || UseBrain) && sense != null) ObserveWithGoal(world, sense, chosen, 2, energyDelta);
            }
            else if (feel == MotorMemory.Feel.Medium)
            {
                Trace.Add(new StepTrace { From = from, Dx = moveDx, Dy = moveDy, Outcome = StepOutcome.Medium });
                if ((LearnEnabled || UseBrain) && sense != null) ObserveWithGoal(world, sense, chosen, 1, energyDelta);
            }
            else
            {
                Trace.Add(new StepTrace { From = from, Dx = moveDx, Dy = moveDy, Outcome = StepOutcome.Smooth });
                Brain.MarkKeptSmooth();
                if ((LearnEnabled || UseBrain) && sense != null) ObserveWithGoal(world, sense, chosen, 0, energyDelta);
            }

            IsOnGoal = Cell == world.GoalCell;
            if (IsOnGoal)
            {
                GoalTouches++;
            }
            return true;
        }

        byte FeelToOutcome(MotorMemory.Feel feel)
        {
            switch (feel)
            {
                case MotorMemory.Feel.Smooth: return 0;
                case MotorMemory.Feel.Medium: return 1;
                case MotorMemory.Feel.Rough: return 2;
                case MotorMemory.Feel.Obstacle: return 3;
                default: return 1;
            }
        }

        static int DirIndex(int dx, int dy)
        {
            for (int i = 0; i < 8; i++)
            {
                if (DirDx[i] == dx && DirDy[i] == dy)
                {
                    return i;
                }
            }
            return -1;
        }

        void FaceToward(Point goal)
        {
            int dx = goal.X - Cell.X;
            int dy = goal.Y - Cell.Y;
            if (dx == 0 && dy == 0) return;

            int adx = Math.Abs(dx);
            int ady = Math.Abs(dy);
            int sx = Math.Sign(dx);
            int sy = Math.Sign(dy);
            if (adx > ady * 6 / 5) { sy = 0; }
            else if (ady > adx * 6 / 5) { sx = 0; }

            int idx = DirIndex(sx, sy);
            if (idx >= 0) Facing = idx;
        }

        static int CellKey(Point p)
        {
            return (p.X << 16) ^ (p.Y & 0xFFFF);
        }

        void ClearVisits()
        {
            _visitStep.Clear();
            _visitCount.Clear();
        }

        void NoteVisit(Point p)
        {
            int key = CellKey(p);
            _visitStep[key] = DecisionSteps;
            int c;
            _visitCount.TryGetValue(key, out c);
            _visitCount[key] = c + 1;
        }

        double RevisitWeight(Point next)
        {
            if (!RevisitInhibition) return 1.0;
            int key = CellKey(next);
            int last;
            if (!_visitStep.TryGetValue(key, out last))
                return 1.0;
            int age = DecisionSteps - last;
            if (age < 0) age = 0;
            if (age > RevisitHorizon) return 1.0;

            int count;
            _visitCount.TryGetValue(key, out count);
            if (count < 1) count = 1;

            double freshness = 1.0 - (double)age / (double)RevisitHorizon;
            double mult = Math.Pow(1.0 - RevisitPenalty * 0.85, freshness * (0.6 + 0.4 * Math.Min(count, 6)));
            if (next == _prevCell)
                mult *= 0.25;
            if (mult < 0.04) mult = 0.04;
            return mult;
        }


        byte CurrentGoalNearness(World world)
        {
            return BrainMemory.ComputeGoalNearness(Cell, world.GoalCell, world.Cols, world.Rows);
        }

        byte CurrentGoalDirection(World world)
        {
            return BrainMemory.ComputeGoalDirection(Cell, world.GoalCell, Facing);
        }

        void ObserveWithGoal(World world, DirCellKind[] sense, int chosen, byte outcome, float energyDelta)
        {
            // V15: feel/motor STM only â€” no live goal-direction on path units.
            if (world != null && !string.IsNullOrEmpty(world.Name))
                Brain.ActiveWorldId = world.Name;
            Brain.ObserveFeel(sense, chosen, outcome, Facing, energyDelta, (short)Cell.X, (short)Cell.Y);
        }

        /// <summary>
        /// V15: When UseBrain==true, Brain.DirectionWeight leads terrain preference.
        /// Drop/shrink immediate roughness/motor-feel multipliers.
        /// When UseBrain==false, keep V10-style strong terrain/motor biases.
        /// </summary>
        int PickDirection(World world, DirCellKind[] sense)
        {
            if (DecisionSteps == 0 && !HideDestination)
            {
                FaceToward(world.GoalCell);
            }

            // TabularNgramBC: push SOURCE-cell feel (same encoding as teacher) before lookup.
            bool bcActive = TabularBC != null;
            int bcRel = -1;
            bool bcHit = false;
            if (bcActive)
            {
                FeelCode srcFeel = TabularNgramBC.FeelAtSource(world, Cell);
                _bcFeelHist.Add((byte)srcFeel);
                int support; string mkey;
                if (TabularBC.TryLookup(_bcFeelHist, out bcRel, out support, out mkey) && bcRel >= 0 && bcRel <= 7)
                {
                    bcHit = true;
                    BcHits++;
                }
                else
                {
                    BcMisses++;
                }
            }

            double[] weights = new double[8];
            double sum = 0;
            int legal = 0;
            for (int i = 0; i < 8; i++)
            {
                int nx = Cell.X + DirDx[i];
                int ny = Cell.Y + DirDy[i];
                if (nx < 0 || ny < 0 || nx >= world.Cols || ny >= world.Rows)
                {
                    weights[i] = 0;
                    continue;
                }
                legal++;
                double w = 1.0;
                int rel = (i - Facing + 8) % 8;
                if (rel == 0) w = 1.35;
                else if (rel == 1 || rel == 7) w = 1.15;
                else if (rel == 2 || rel == 6) w = 0.95;
                else if (rel == 3 || rel == 5) w = 0.70;
                else w = 0.45;

                var nextCell = new Point(nx, ny);
                float roughness = world.CellRoughness(nextCell);

                if (bcActive)
                {
                    // Hit: strong boost on matched relative move. Miss: EmptyLTM reactive (terrain).
                    if (bcHit)
                    {
                        if (rel == bcRel) w *= 4.0;
                        else w *= 0.35;
                        w *= (1.0 - 0.35 * roughness);
                        var knownBc = Motor.Get(nextCell);
                        if (knownBc == MotorMemory.Feel.Obstacle) w *= 0.10;
                        else if (knownBc == MotorMemory.Feel.Rough) w *= 0.70;
                        else if (knownBc == MotorMemory.Feel.Smooth) w *= 1.15;
                    }
                    else
                    {
                        double terrainWeight = 1.0 - 0.7 * roughness;
                        w *= terrainWeight;
                        var knownMiss = Motor.Get(nextCell);
                        if (knownMiss == MotorMemory.Feel.Obstacle) w *= 0.15;
                        else if (knownMiss == MotorMemory.Feel.Rough) w *= 0.45;
                        else if (knownMiss == MotorMemory.Feel.Medium) w *= 0.75;
                        else if (knownMiss == MotorMemory.Feel.Smooth) w *= 1.35;
                        w *= (1.0 - 0.5 * roughness);
                    }
                }
                else if (UseBrain && sense != null)
                {
                    // Confidence-gated blend: unmatched LTM must not strip terrain bias (transfer poison).
                    double conf;
                    int gNear = HideDestination ? -1 : CurrentGoalNearness(world);
                    int gDir = HideDestination ? -1 : CurrentGoalDirection(world);
                    double brainW = Brain.DirectionWeight(sense, i, Facing, gNear, gDir, out conf);
                    if (conf < 0.05)
                    {
                        double terrainWeight = 1.0 - 0.7 * roughness;
                        w *= terrainWeight;
                        var knownWeak = Motor.Get(nextCell);
                        if (knownWeak == MotorMemory.Feel.Obstacle) w *= 0.15;
                        else if (knownWeak == MotorMemory.Feel.Rough) w *= 0.45;
                        else if (knownWeak == MotorMemory.Feel.Medium) w *= 0.75;
                        else if (knownWeak == MotorMemory.Feel.Smooth) w *= 1.35;
                        w *= (1.0 - 0.5 * roughness);
                    }
                    else
                    {
                        // terrainCoeff: 0.70 at low conf â†’ 0.25 at conf=1
                        double terrainCoeff = 0.70 - 0.45 * conf;
                        w *= (1.0 - conf) + conf * brainW;
                        w *= (1.0 - terrainCoeff * roughness);

                        var known = Motor.Get(nextCell);
                        if (known == MotorMemory.Feel.Obstacle) w *= 0.10 + 0.05 * (1.0 - conf);
                        else if (known == MotorMemory.Feel.Rough) w *= 0.85 - 0.40 * (1.0 - conf);
                        else if (known == MotorMemory.Feel.Medium) w *= 0.95 - 0.20 * (1.0 - conf);
                        else if (known == MotorMemory.Feel.Smooth) w *= 1.10 + 0.25 * (1.0 - conf);
                    }
                }
                else
                {
                    double terrainWeight = 1.0 - 0.7 * roughness;
                    w *= terrainWeight;

                    var known = Motor.Get(nextCell);
                    if (known == MotorMemory.Feel.Obstacle) w *= 0.15;
                    else if (known == MotorMemory.Feel.Rough) w *= 0.45;
                    else if (known == MotorMemory.Feel.Medium) w *= 0.75;
                    else if (known == MotorMemory.Feel.Smooth) w *= 1.35;

                    w *= (1.0 - 0.5 * roughness);
                }

                if (world.CellHasObstacle(nextCell)) w *= 0.05;

                if (!HideDestination)
                {
                    int gdx = Math.Sign(world.GoalCell.X - Cell.X);
                    int gdy = Math.Sign(world.GoalCell.Y - Cell.Y);
                    if (DirDx[i] == gdx && gdx != 0) w *= 1.20;
                    if (DirDy[i] == gdy && gdy != 0) w *= 1.20;
                }
                w *= RevisitWeight(nextCell);
                if (w < 0.05) w = 0.05;

                weights[i] = w;
                sum += w;
            }

            if (legal == 0 || sum <= 0) return -1;

            double roll = _rng.NextDouble() * sum;
            double acc = 0;
            int chosen = -1;
            for (int i = 0; i < 8; i++)
            {
                if (weights[i] <= 0) continue;
                acc += weights[i];
                if (roll <= acc) { chosen = i; break; }
            }
            if (chosen < 0)
            {
                for (int i = 0; i < 8; i++) if (weights[i] > 0) chosen = i;
            }
            if (chosen >= 0) Facing = chosen;
            return chosen;
        }

        public void StepPrimitive(World world)
        {
            if (PendingObstacleExit)
            {
                ExitObstacle(world);
            }
            else
            {
                StepWeightedRealistic(world);
            }
        }

        /// <summary>
        /// V11: Call at end of episode (goal reached or timeout) to trigger episode-energy credit.
        /// </summary>
        public void NotifyEpisodeEnd(bool reachedGoal)
        {
            if (LearnEnabled)
            {
                Brain.OnEpisodeEnd(reachedGoal, TotalEnergy, DecisionSteps);
            }
        }

        public void NotifyEpisodeEnd(bool reachedGoal, World world)
        {
            if (!LearnEnabled) return;
            if (world != null)
            {
                Brain.ActiveWorldId = world.Name ?? "";
                Brain.SetTourGoalForBackfill(world.StartCell, world.GoalCell);
            }
            Brain.OnEpisodeEnd(reachedGoal, TotalEnergy, DecisionSteps);
        }
    }
}

