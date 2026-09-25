using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Evolution
{
    public sealed class EvalRunTotals
    {
        public string Label;
        public int Runs;
        public int GoalsReached;
        public double TotalEnergy;
        public double EnergyToGoalSum;
        public long DecisionSteps;
        public int StepsSmooth, StepsMedium, StepsRough, StepsObstacle;

        public double AvgEnergy => Runs <= 0 ? 0 : TotalEnergy / Runs;

        public double Score
        {
            get
            {
                if (Runs <= 0) return 0;
                double goalRate = (double)GoalsReached / Runs;
                double avgEnergy = TotalEnergy / Runs;
                return 50.0 * goalRate - 0.5 * avgEnergy;
            }
        }

        public double EnergyScore
        {
            get
            {
                if (Runs <= 0) return double.MaxValue;
                int timeouts = Runs - GoalsReached;
                double maxEnergy = DecisionSteps > 0 ? (double)DecisionSteps / Runs * 1.0 : 1000.0;
                double failEnergy = timeouts * maxEnergy;
                return (EnergyToGoalSum + failEnergy) / Runs;
            }
        }

        public double AvgDecisionSteps => Runs <= 0 ? 0 : (double)DecisionSteps / Runs;
        public double AvgEnergyToGoal => GoalsReached <= 0 ? 0 : EnergyToGoalSum / GoalsReached;
        public double GoalRate => Runs <= 0 ? 0 : (double)GoalsReached / Runs;
    }

    public sealed class TransferMatrixResult
    {
        public string TrainWorld;
        public Dictionary<string, EvalRunTotals> WithBrain = new Dictionary<string, EvalRunTotals>();
        public Dictionary<string, EvalRunTotals> NoBrain = new Dictionary<string, EvalRunTotals>();
    }

    public sealed class PrefToLtmResult
    {
        public BrainMemory Brain;
        public int TrainEpisodes;
        public int TrainGoals;
        public double TrainAvgEnergy;
        public int PreferredSignatures;
        public int RulesWritten;
        public string MemoryPath;
        public TransferMatrixResult Transfer;
    }

    public static class Evaluator
    {
        public const int DefaultRuns = 100;
        public const int DefaultMaxSteps = 3000;
        public const int TransferTrainRuns = 100;
        public const int TransferEvalRuns = 50;

        /// <summary>Default episodes per world for Headless Prefâ†’LTM (4 worlds â‰ˆ 320 total).</summary>
        public const int PrefToLtmEpisodesPerWorld = 80;
        public const int PrefToLtmEvalRuns = 40;

        public static string DefaultLogDirectory
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectEval = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "EvalLogs"));
                string workspaceEval = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "EvalLogs"));
                if (Directory.Exists(Path.GetDirectoryName(workspaceEval)) ||
                    workspaceEval.IndexOf("LifeSim", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        Directory.CreateDirectory(workspaceEval);
                        return workspaceEval;
                    }
                    catch
                    {
                    }
                }
                try
                {
                    Directory.CreateDirectory(projectEval);
                    return projectEval;
                }
                catch
                {
                    string local = Path.Combine(baseDir, "EvalLogs");
                    Directory.CreateDirectory(local);
                    return local;
                }
            }
        }

        public static EvalRunTotals RunBatch(
            World world,
            bool useBrain,
            int runs,
            int maxSteps,
            Random rng,
            StringBuilder log)
        {
            var totals = new EvalRunTotals
            {
                Label = useBrain ? "With brain memory" : "Without brain memory",
                Runs = runs
            };

            log.AppendLine("brain_policy," + BrainMemory.PolicyVersion);
            log.AppendLine("=== " + totals.Label + " ===");
            log.AppendLine("run,goal,energy,steps_smooth,steps_medium,steps_rough,steps_obstacle,decision_steps");

            for (int r = 1; r <= runs; r++)
            {
                var robot = new Robot(world.StartCell, new Random(rng.Next()));
                robot.UseBrain = useBrain;

                int safety = maxSteps * 20;
                int ticks = 0;
                while (!robot.IsOnGoal && robot.DecisionSteps < maxSteps && ticks < safety)
                {
                    robot.Tick(world);
                    ticks++;
                }

                bool goal = robot.IsOnGoal;
                robot.NotifyEpisodeEnd(goal);

                if (goal)
                {
                    totals.GoalsReached++;
                    totals.EnergyToGoalSum += robot.TotalEnergy;
                }

                totals.TotalEnergy += robot.TotalEnergy;
                totals.StepsSmooth += robot.StepsSmooth;
                totals.StepsMedium += robot.StepsMedium;
                totals.StepsRough += robot.StepsRough;
                totals.StepsObstacle += robot.StepsOnObstacle;
                totals.DecisionSteps += robot.DecisionSteps;

                log.AppendLine(string.Format(
                    "{0},{1},{2:0.0},{3},{4},{5},{6},{7}",
                    r,
                    goal ? 1 : 0,
                    robot.TotalEnergy,
                    robot.StepsSmooth,
                    robot.StepsMedium,
                    robot.StepsRough,
                    robot.StepsOnObstacle,
                    robot.DecisionSteps));
            }

            log.AppendLine();
            return totals;
        }

        /// <summary>
        /// V11: Train a fresh brain on one world, then evaluate on all worlds (frozen).
        /// Returns transfer matrix: for each test world, with-brain and no-brain results.
        /// </summary>
        public static TransferMatrixResult RunTransferMatrix(
            World trainWorld,
            IReadOnlyList<World> allWorlds,
            int trainRuns,
            int evalRuns,
            int maxSteps,
            Random rng,
            StringBuilder log,
            Action<string> progressCallback = null)
        {
            var result = new TransferMatrixResult { TrainWorld = trainWorld.Name };
            log.AppendLine("=== Transfer Matrix Evaluation ===");
            log.AppendLine("brain_policy," + BrainMemory.PolicyVersion);
            log.AppendLine("train_world," + trainWorld.Name);
            log.AppendLine("train_runs," + trainRuns);
            log.AppendLine("eval_runs," + evalRuns);
            log.AppendLine("max_steps," + maxSteps);
            log.AppendLine();

            var brain = new BrainMemory();

            log.AppendLine("=== Training Phase on " + trainWorld.Name + " ===");
            progressCallback?.Invoke("Training on " + trainWorld.Name + "...");
            int trainGoals = 0;
            double trainEnergy = 0;
            for (int r = 1; r <= trainRuns; r++)
            {
                var robot = new Robot(trainWorld.StartCell, new Random(rng.Next()));
                robot.UseBrain = true;
                robot.LearnEnabled = true;
                robot.RecordSenseTrace = true;
                robot.ClearBrainOnReset = false;
                robot.SetBrain(brain);

                int ticks = 0;
                int safety = maxSteps * 20;
                while (!robot.IsOnGoal && robot.DecisionSteps < maxSteps && ticks < safety)
                {
                    robot.Tick(trainWorld);
                    ticks++;
                }

                bool goal = robot.IsOnGoal;
                robot.NotifyEpisodeEnd(goal);
                if (goal) trainGoals++;
                trainEnergy += robot.TotalEnergy;
                brain.TrainFromShortTerm(2);
            }
            brain.TrainFromShortTerm(3);

            log.AppendLine("train_goals," + trainGoals + "/" + trainRuns);
            log.AppendLine("train_avg_energy," + (trainEnergy / trainRuns).ToString("0.0"));
            log.AppendLine("brain_ltm_count," + brain.LtmCount);
            log.AppendLine("brain_stm_count," + brain.StmCount);
            log.AppendLine();

            log.AppendLine("=== Evaluation Phase (brain frozen) ===");
            foreach (var testWorld in allWorlds)
            {
                progressCallback?.Invoke("Evaluating on " + testWorld.Name + "...");
                log.AppendLine();
                log.AppendLine("--- Test world: " + testWorld.Name + (testWorld.Name == trainWorld.Name ? " (same as train)" : " (transfer)") + " ---");

                int pairedBase = rng.Next();
                EvalRunTotals withBrain, noBrain;
                EvalFrozenPaired(testWorld, brain, evalRuns, maxSteps, pairedBase, out withBrain, out noBrain, false);
                result.WithBrain[testWorld.Name] = withBrain;
                result.NoBrain[testWorld.Name] = noBrain;

                log.AppendLine("with_brain: goals=" + withBrain.GoalsReached + "/" + withBrain.Runs +
                    ", energy_score=" + withBrain.EnergyScore.ToString("0.0"));
                log.AppendLine("no_brain: goals=" + noBrain.GoalsReached + "/" + noBrain.Runs +
                    ", energy_score=" + noBrain.EnergyScore.ToString("0.0"));
                double gain = noBrain.EnergyScore - withBrain.EnergyScore;
                log.AppendLine("brain_gain=" + gain.ToString("+0.0;-0.0;0.0") +
                    (testWorld.Name == trainWorld.Name ? " (same-world)" : " (held-out)"));
            }

            return result;
        }

        /// <summary>Fix2 paired RNG: episodeSeed = unchecked(baseSeed * 1000003 + runIndex).</summary>
        public static int MixEpisodeSeed(int baseSeed, int runIndex)
        {
            return unchecked(baseSeed * 1000003 + runIndex);
        }

        /// <summary>
        /// Fix2: Paired frozen eval. Clears STM (LTM-only). Same episodeSeed for both arms.
        /// hideDestination: V15 dest-hidden arm (no goal-coord bias).
        /// </summary>
        public static void EvalFrozenPaired(
            World world,
            BrainMemory sourceBrain,
            int runs,
            int maxSteps,
            int pairedBaseSeed,
            out EvalRunTotals withBrain,
            out EvalRunTotals noBrain,
            bool hideDestination = false)
        {
            withBrain = new EvalRunTotals { Runs = runs, Label = "With brain on " + world.Name };
            noBrain = new EvalRunTotals { Runs = runs, Label = "No brain on " + world.Name };
            for (int r = 1; r <= runs; r++)
            {
                int episodeSeed = MixEpisodeSeed(pairedBaseSeed, r);
                AccumulateFrozenEpisode(world, sourceBrain, true, maxSteps, episodeSeed, withBrain, hideDestination);
                AccumulateFrozenEpisode(world, sourceBrain, false, maxSteps, episodeSeed, noBrain, hideDestination);
            }
        }

        static void AccumulateFrozenEpisode(
            World world,
            BrainMemory sourceBrain,
            bool useBrain,
            int maxSteps,
            int episodeSeed,
            EvalRunTotals totals,
            bool hideDestination = false)
        {
            var robot = new Robot(world.StartCell, new Random(episodeSeed));
            robot.UseBrain = useBrain;
            robot.LearnEnabled = false;
            robot.RecordSenseTrace = false;
            robot.ClearBrainOnReset = false;
            robot.HideDestination = hideDestination;
            if (useBrain)
                robot.SetBrain(sourceBrain.CloneFrozenLtmOnly());
            else
                robot.SetBrain(new BrainMemory());

            int ticks = 0;
            int safety = maxSteps * 20;
            while (!robot.IsOnGoal && robot.DecisionSteps < maxSteps && ticks < safety)
            {
                robot.Tick(world);
                ticks++;
            }

            bool goal = robot.IsOnGoal;
            if (goal)
            {
                totals.GoalsReached++;
                totals.EnergyToGoalSum += robot.TotalEnergy;
            }
            totals.TotalEnergy += robot.TotalEnergy;
            totals.StepsSmooth += robot.StepsSmooth;
            totals.StepsMedium += robot.StepsMedium;
            totals.StepsRough += robot.StepsRough;
            totals.StepsObstacle += robot.StepsOnObstacle;
            totals.DecisionSteps += robot.DecisionSteps;
        }

        /// <summary>Legacy single-arm frozen eval; still clears STM. Prefer EvalFrozenPaired.</summary>
        static EvalRunTotals EvalFrozen(
            World world,
            BrainMemory sourceBrain,
            bool useBrain,
            int runs,
            int maxSteps,
            Random rng,
            StringBuilder log,
            bool hideDestination = false)
        {
            int baseSeed = rng != null ? rng.Next() : Environment.TickCount;
            var totals = new EvalRunTotals { Runs = runs };
            for (int r = 1; r <= runs; r++)
                AccumulateFrozenEpisode(world, sourceBrain, useBrain, maxSteps, MixEpisodeSeed(baseSeed, r), totals, hideDestination);
            return totals;
        }

        /// <summary>
        /// Headless Prefâ†’LTM pipeline:
        /// 1) Many learning episodes across Canvas-A..D (exact 8-neighbor sense signatures).
        /// 2) Aggregate durable preferred-move votes (energy/success-weighted); argmax â†’ LTM SenseMoveRule.
        /// 3) Persist brain.mem; freeze learn; transfer eval vs no-brain on Canvas-A..D.
        /// Does not invent worlds â€” uses the provided list (typically WorldLibrary.All).
        /// </summary>
        public static PrefToLtmResult RunPreferredMoveHeadless(
            IReadOnlyList<World> worlds,
            int episodesPerWorld,
            int evalRuns,
            int maxSteps,
            Random rng,
            StringBuilder log,
            string memorySavePath,
            Action<string> progressCallback = null)
        {
            if (worlds == null || worlds.Count == 0)
                throw new ArgumentException("worlds required");
            if (episodesPerWorld < 1) episodesPerWorld = PrefToLtmEpisodesPerWorld;
            if (evalRuns < 1) evalRuns = PrefToLtmEvalRuns;
            if (maxSteps < 1) maxSteps = DefaultMaxSteps;
            if (rng == null) rng = new Random();
            if (log == null) log = new StringBuilder();

            var result = new PrefToLtmResult();
            log.AppendLine("=== Headless Preferred-Move â†’ LTM ===");
            log.AppendLine("brain_policy," + BrainMemory.PolicyVersion);
            log.AppendLine("episodes_per_world," + episodesPerWorld);
            log.AppendLine("eval_runs," + evalRuns);
            log.AppendLine("max_steps," + maxSteps);
            log.AppendLine("weighting,outcome_energy: smooth=1.0 medium=0.55 rough=0.15 obst=0.05 * 1/(1+energyDelta); goal-episode boost");
            log.AppendLine("signature,path10_8neigh_nearness_goaldir_relative");
            log.AppendLine();

            var brain = new BrainMemory();
            brain.ClearPreferredVotes();

            int totalEps = 0;
            int totalGoals = 0;
            double totalEnergy = 0;

            log.AppendLine("=== Phase 1: Headless learning + preferred-move tallies ===");
            foreach (var world in worlds)
            {
                progressCallback?.Invoke("Prefâ†’LTM train " + world.Name + "...");
                int goals = 0;
                double energy = 0;
                for (int r = 1; r <= episodesPerWorld; r++)
                {
                    var robot = new Robot(world.StartCell, new Random(rng.Next()));
                    robot.UseBrain = true;
                    robot.LearnEnabled = true;
                    robot.RecordSenseTrace = true;
                    robot.ClearBrainOnReset = false;
                    robot.SetBrain(brain);

                    int ticks = 0;
                    int safety = maxSteps * 20;
                    while (!robot.IsOnGoal && robot.DecisionSteps < maxSteps && ticks < safety)
                    {
                        robot.Tick(world);
                        ticks++;
                    }
                    bool goal = robot.IsOnGoal;
                    robot.NotifyEpisodeEnd(goal);
                    if (goal) goals++;
                    energy += robot.TotalEnergy;
                    brain.TrainFromShortTerm(1);
                    totalEps++;
                }
                brain.TrainFromShortTerm(2);
                totalGoals += goals;
                totalEnergy += energy;
                log.AppendLine(string.Format("train,{0},goals,{1}/{2},avg_energy,{3:0.0},pref_sig,{4},ltm,{5}",
                    world.Name, goals, episodesPerWorld, energy / episodesPerWorld,
                    brain.PreferredVoteSignatureCount, brain.LtmCount));
            }

            log.AppendLine();
            log.AppendLine("=== Phase 2: Populate LTM from preferred-move argmax ===");
            progressCallback?.Invoke("Flushing preferred moves â†’ LTM...");
            // Backfill any remaining STM into tallies, then flush argmax per exact signature
            brain.PromotePreferredMovesFromTraces(false, 0.5f);
            int written = brain.PopulateLtmFromPreferredMoves(0.8f, brain.LtmCapacity);
            result.RulesWritten = written;
            result.PreferredSignatures = brain.PreferredVoteSignatureCount;
            result.TrainEpisodes = totalEps;
            result.TrainGoals = totalGoals;
            result.TrainAvgEnergy = totalEps > 0 ? totalEnergy / totalEps : 0;
            result.Brain = brain;

            log.AppendLine("pref_signatures," + brain.PreferredVoteSignatureCount);
            log.AppendLine("pref_total_mass," + brain.PreferredVoteTotalMass.ToString("0.0"));
            log.AppendLine("ltm_rules_written_or_merged," + written);
            log.AppendLine("ltm_count," + brain.LtmCount);
            log.AppendLine("stm_count," + brain.StmCount);
            log.AppendLine();

            if (!string.IsNullOrEmpty(memorySavePath))
            {
                progressCallback?.Invoke("Saving " + memorySavePath + "...");
                brain.SaveToFile(memorySavePath);
                result.MemoryPath = memorySavePath;
                log.AppendLine("memory_saved," + memorySavePath);
            }

            log.AppendLine();
            log.AppendLine("=== Phase 3: Transfer eval (brain frozen) on Canvas worlds ===");
            result.Transfer = new TransferMatrixResult { TrainWorld = "Prefâ†’LTM(curriculum)" };
            foreach (var testWorld in worlds)
            {
                progressCallback?.Invoke("Transfer eval " + testWorld.Name + "...");
                log.AppendLine();
                log.AppendLine("--- Test world: " + testWorld.Name + " ---");

                int pairedBase = rng.Next();
                EvalRunTotals withBrain, noBrain;
                EvalFrozenPaired(testWorld, brain, evalRuns, maxSteps, pairedBase, out withBrain, out noBrain, false);
                result.Transfer.WithBrain[testWorld.Name] = withBrain;
                result.Transfer.NoBrain[testWorld.Name] = noBrain;

                log.AppendLine("with_brain: goals=" + withBrain.GoalsReached + "/" + withBrain.Runs +
                    ", energy_score=" + withBrain.EnergyScore.ToString("0.0"));
                log.AppendLine("no_brain: goals=" + noBrain.GoalsReached + "/" + noBrain.Runs +
                    ", energy_score=" + noBrain.EnergyScore.ToString("0.0"));
                double gain = noBrain.EnergyScore - withBrain.EnergyScore;
                log.AppendLine("brain_gain=" + gain.ToString("+0.0;-0.0;0.0") + " (held-out/curriculum)");
            }

            return result;
        }


        /// <summary>
        /// V15 TeacherSuccessâ†’LTM: destination-known teacher generates many attempts;
        /// only successful trajectories contribute features. Then frozen transfer eval
        /// on both goal-known and dest-hidden arms (Fix2 honesty).
        /// </summary>
        public static PrefToLtmResult RunTeacherSuccessHeadless(
            IReadOnlyList<World> worlds,
            int attemptsPerWorld,
            int evalRuns,
            int maxSteps,
            Random rng,
            StringBuilder log,
            string memorySavePath,
            Action<string> progressCallback = null,
            bool evalHideDestination = false)
        {
            if (worlds == null || worlds.Count == 0)
                throw new ArgumentException("worlds required");
            if (attemptsPerWorld < 1) attemptsPerWorld = 400;
            if (evalRuns < 1) evalRuns = PrefToLtmEvalRuns;
            if (maxSteps < 1) maxSteps = DefaultMaxSteps;
            if (rng == null) rng = new Random();
            if (log == null) log = new StringBuilder();

            var result = new PrefToLtmResult();
            log.AppendLine("=== Headless TeacherSuccess â†’ LTM ===");
            log.AppendLine("brain_policy," + BrainMemory.PolicyVersion);
            log.AppendLine("population_mode,TeacherSuccess->LTM");
            log.AppendLine("attempts_per_world," + attemptsPerWorld);
            log.AppendLine("eval_runs," + evalRuns);
            log.AppendLine("max_steps," + maxSteps);
            log.AppendLine("eval_hide_destination," + (evalHideDestination ? 1 : 0));
            log.AppendLine("signature,path10_8neigh_nearness_goaldir_relative");
            log.AppendLine();

            var brain = new BrainMemory();
            progressCallback?.Invoke("TeacherSuccess populate...");
            var stats = TeacherPrimitive.PopulateBrainFromSuccesses(
                brain, worlds, attemptsPerWorld, maxSteps, rng,
                msg => progressCallback?.Invoke(msg));

            result.RulesWritten = stats.RulesWritten;
            result.PreferredSignatures = stats.PreferredSignatures;
            result.TrainEpisodes = stats.SuccessPaths + stats.FailedAttempts;
            result.TrainGoals = stats.SuccessPaths;
            result.TrainAvgEnergy = 0;
            result.Brain = brain;

            log.AppendLine("teacher_success_paths," + stats.SuccessPaths);
            log.AppendLine("teacher_failed_attempts," + stats.FailedAttempts);
            log.AppendLine("teacher_success_steps," + stats.SuccessSteps);
            log.AppendLine("pref_signatures," + stats.PreferredSignatures);
            log.AppendLine("ltm_rules_written_or_merged," + stats.RulesWritten);
            log.AppendLine("ltm_count," + brain.LtmCount);
            foreach (var kv in stats.WorldSuccesses)
            {
                int fail = stats.WorldFailures.ContainsKey(kv.Key) ? stats.WorldFailures[kv.Key] : 0;
                log.AppendLine(string.Format("world,{0},successes,{1},failures,{2}", kv.Key, kv.Value, fail));
            }
            log.AppendLine();

            if (!string.IsNullOrEmpty(memorySavePath))
            {
                brain.SaveToFile(memorySavePath);
                result.MemoryPath = memorySavePath;
                log.AppendLine("memory_saved," + memorySavePath);
            }

            log.AppendLine();
            log.AppendLine("=== Frozen eval (Fix2 paired, STM cleared) hideDest=" + evalHideDestination + " ===");
            result.Transfer = new TransferMatrixResult { TrainWorld = "TeacherSuccess->LTM" };
            foreach (var testWorld in worlds)
            {
                progressCallback?.Invoke("Teacher eval " + testWorld.Name + (evalHideDestination ? " (dest-hidden)" : " (goal-known)") + "...");
                log.AppendLine();
                log.AppendLine("--- Test world: " + testWorld.Name + " ---");
                int pairedBase = rng.Next();
                EvalRunTotals withBrain, noBrain;
                EvalFrozenPaired(testWorld, brain, evalRuns, maxSteps, pairedBase, out withBrain, out noBrain, evalHideDestination);
                result.Transfer.WithBrain[testWorld.Name] = withBrain;
                result.Transfer.NoBrain[testWorld.Name] = noBrain;
                log.AppendLine("with_brain: goals=" + withBrain.GoalsReached + "/" + withBrain.Runs +
                    ", energy_score=" + withBrain.EnergyScore.ToString("0.0"));
                log.AppendLine("no_brain: goals=" + noBrain.GoalsReached + "/" + noBrain.Runs +
                    ", energy_score=" + noBrain.EnergyScore.ToString("0.0"));
                double gain = noBrain.EnergyScore - withBrain.EnergyScore;
                log.AppendLine("brain_gain=" + gain.ToString("+0.0;-0.0;0.0"));
            }
            return result;
        }

        public static string SavePrefToLtmLog(StringBuilder log)
        {
            Directory.CreateDirectory(DefaultLogDirectory);
            string name = string.Format("pref_to_ltm_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
            string path = Path.Combine(DefaultLogDirectory, name);
            File.WriteAllText(path, log.ToString(), Encoding.UTF8);
            return path;
        }

        public static string FormatTransferMatrix(TransferMatrixResult result, IReadOnlyList<World> allWorlds)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Transfer Matrix Results");
            sb.AppendLine("Trained on: " + result.TrainWorld);
            sb.AppendLine("Policy: " + BrainMemory.PolicyVersion);
            sb.AppendLine();
            sb.AppendLine("Test World          | With Brain    | No Brain      | Gain");
            sb.AppendLine("--------------------+---------------+---------------+-------");

            foreach (var world in allWorlds)
            {
                var wb = result.WithBrain[world.Name];
                var nb = result.NoBrain[world.Name];
                double gain = nb.EnergyScore - wb.EnergyScore;
                string tag = world.Name == result.TrainWorld ? "*" : " ";
                sb.AppendLine(string.Format("{0,-18}{1} | {2,5:0.0} ({3,2}%) | {4,5:0.0} ({5,2}%) | {6,5:+0.0;-0.0;0.0}",
                    world.Name, tag,
                    wb.EnergyScore, (int)(wb.GoalRate * 100),
                    nb.EnergyScore, (int)(nb.GoalRate * 100),
                    gain));
            }

            sb.AppendLine();
            sb.AppendLine("* = same world as training (in-sample), others = held-out transfer");
            sb.AppendLine("Energy score = mean energy to goal (timeouts = max penalty). Lower is better.");
            sb.AppendLine("Gain = no_brain - with_brain (positive = brain helps)");
            return sb.ToString();
        }

        public static string FormatComparison(EvalRunTotals withBrain, EvalRunTotals noBrain, string worldName, string logPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Evaluation â€” " + worldName);
            sb.AppendLine("Brain policy: " + BrainMemory.PolicyVersion);
            sb.AppendLine(withBrain.Runs + " runs each, max " + DefaultMaxSteps + " steps/run, no graphics.");
            sb.AppendLine();
            sb.AppendLine(FormatBlock(withBrain));
            sb.AppendLine();
            sb.AppendLine(FormatBlock(noBrain));
            sb.AppendLine();
            sb.AppendLine("Comparison");
            sb.AppendLine("  Energy score with brain:    " + withBrain.EnergyScore.ToString("0.00"));
            sb.AppendLine("  Energy score without brain: " + noBrain.EnergyScore.ToString("0.00"));
            string winner;
            if (Math.Abs(withBrain.EnergyScore - noBrain.EnergyScore) < 1.0)
            {
                winner = "Roughly tied.";
            }
            else if (withBrain.EnergyScore < noBrain.EnergyScore)
            {
                winner = "Winner: WITH brain memory (lower energy).";
            }
            else
            {
                winner = "Winner: WITHOUT brain memory (lower energy).";
            }
            sb.AppendLine("  " + winner);
            sb.AppendLine();
            sb.AppendLine("Energy score = mean energy to goal (timeouts count as max energy). Lower is better.");
            sb.AppendLine("Log: " + logPath);
            return sb.ToString();
        }

        static string FormatBlock(EvalRunTotals t)
        {
            var sb = new StringBuilder();
            sb.AppendLine(t.Label);
            sb.AppendLine("  Goals reached:     " + t.GoalsReached + " / " + t.Runs);
            sb.AppendLine("  Avg energy:        " + t.AvgEnergy.ToString("0.0"));
            sb.AppendLine("  Avg steps smooth:  " + (t.Runs > 0 ? (double)t.StepsSmooth / t.Runs : 0).ToString("0.0"));
            sb.AppendLine("  Avg steps medium:  " + (t.Runs > 0 ? (double)t.StepsMedium / t.Runs : 0).ToString("0.0"));
            sb.AppendLine("  Avg steps rough:   " + (t.Runs > 0 ? (double)t.StepsRough / t.Runs : 0).ToString("0.0"));
            sb.AppendLine("  Avg steps obstacle:" + (t.Runs > 0 ? (double)t.StepsObstacle / t.Runs : 0).ToString("0.0"));
            sb.AppendLine("  Avg decision steps:" + t.AvgDecisionSteps.ToString("0.0"));
            if (t.GoalsReached > 0)
            {
                sb.AppendLine("  Avg energy to goal: " + t.AvgEnergyToGoal.ToString("0.0"));
            }
            sb.AppendLine("  Energy score:      " + t.EnergyScore.ToString("0.00"));
            sb.AppendLine("  Composite score:   " + t.Score.ToString("0.00"));
            return sb.ToString();
        }

        public static string SaveLog(StringBuilder log, string worldName)
        {
            Directory.CreateDirectory(DefaultLogDirectory);
            string name = string.Format(
                "eval_{0}_{1:yyyyMMdd_HHmmss}.csv",
                Sanitize(worldName),
                DateTime.Now);
            string path = Path.Combine(DefaultLogDirectory, name);
            File.WriteAllText(path, log.ToString(), Encoding.UTF8);
            return path;
        }

        public static string SaveTransferLog(StringBuilder log)
        {
            Directory.CreateDirectory(DefaultLogDirectory);
            string name = string.Format("transfer_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
            string path = Path.Combine(DefaultLogDirectory, name);
            File.WriteAllText(path, log.ToString(), Encoding.UTF8);
            return path;
        }

        static string Sanitize(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }
            return s;
        }
    }
}

