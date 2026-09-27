using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Evolution
{
    public partial class LifeSim : Form
    {
        int count = 0;
        World _world;
        Robot _robot;
        string _memoryPath;
        BrainMemory _durableBrain;
        readonly Random _rng = new Random();

        Bitmap _backdrop;
        float _cell;
        float _ox;
        float _oy;
        Point _lastRobotCell = new Point(-1, -1);

        public LifeSim()
        {
            InitializeComponent();
            LoadDurableBrain();
            this.FormClosing += Life_FormClosing;
            this.MaximizeBox = true;
            simulationArea.Paint += simulationArea_Paint;
            simulationArea.Resize += simulationArea_Resize;
        }

        private void LifeSim_Shown(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
        }

        private void LifeSim_Load(object sender, EventArgs e)
        {
            SizeMenuButtons();
            LoadWorldLibraryIntoCombo();
            SelectWorld(WorldLibrary.All[0].Name);
        }

        private void LoadWorldLibraryIntoCombo()
        {
            cboWorld.Items.Clear();
            foreach (var w in WorldLibrary.All)
            {
                cboWorld.Items.Add(w.Name);
            }
        }

        private void SelectWorld(string name)
        {
            _world = WorldLibrary.GetByName(name);
            if (cboWorld.SelectedItem as string != _world.Name)
            {
                cboWorld.SelectedItem = _world.Name;
            }

            if (_robot == null)
            {
                _robot = new Robot(_world.StartCell, _rng);
                if (_durableBrain == null) LoadDurableBrain();
                _robot.SetBrain(_durableBrain);
                _robot.ClearBrainOnReset = false;
                _robot.RecordSenseTrace = true;
            }
            else
            {
                _robot.Reset(_world.StartCell);
            }

            _lastRobotCell = _robot.Cell;
            count = 0;
            RebuildBackdrop();
            UpdateStatusLabels();
            simulationArea.Invalidate();
        }

        private void cboWorld_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboWorld.SelectedItem == null)
            {
                return;
            }
            timer1.Stop();
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            SelectWorld(cboWorld.SelectedItem.ToString());
        }

        private void flowMenu_Resize(object sender, EventArgs e)
        {
            SizeMenuButtons();
        }

        private void simulationArea_Resize(object sender, EventArgs e)
        {
            RebuildBackdrop();
            simulationArea.Invalidate();
        }

        private void SizeMenuButtons()
        {
            int width = flowMenu.ClientSize.Width;
            if (width <= 0)
            {
                return;
            }

            lblTitle.Width = width;
            lblWorld.Width = width;
            cboWorld.Width = width;
            btnStart.Width = width;
            btnPause.Width = width;
            btnReset.Width = width;
            btnSettings.Width = width;
            if (btnTrain != null) btnTrain.Width = width;
            if (btnTrainCurriculum != null) btnTrainCurriculum.Width = width;
            btnEvaluate.Width = width;
            if (btnEvalTransfer != null) btnEvalTransfer.Width = width;
            if (btnPrefToLtm != null) btnPrefToLtm.Width = width;
            if (btnMemReset != null) btnMemReset.Width = width;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (_robot == null && _world != null)
            {
                _robot = new Robot(_world.StartCell, _rng);
                if (_durableBrain == null) LoadDurableBrain();
                _robot.SetBrain(_durableBrain);
                _robot.ClearBrainOnReset = false;
                _robot.RecordSenseTrace = true;
                _lastRobotCell = _robot.Cell;
            }

            btnStart.Enabled = false;
            btnPause.Enabled = true;
            cboWorld.Enabled = false;
            timer1.Enabled = true;
            timer1.Interval = 20;
            timer1.Start();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            cboWorld.Enabled = true;
            timer1.Stop();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            cboWorld.Enabled = true;
            timer1.Stop();
            count = 0;
            if (_world != null)
            {
                if (_robot == null)
                {
                    _robot = new Robot(_world.StartCell, _rng);
                    if (_durableBrain == null) LoadDurableBrain();
                    _robot.SetBrain(_durableBrain);
                    _robot.ClearBrainOnReset = false;
                    _robot.RecordSenseTrace = true;
                }
                else
                {
                    _robot.Reset(_world.StartCell);
                }
                InvalidateRobotCells(_lastRobotCell, _robot.Cell);
                _lastRobotCell = _robot.Cell;
            }
            UpdateStatusLabels();
        }

        private void btnTrain_Click(object sender, EventArgs e)
        {
            if (_world == null)
            {
                MessageBox.Show(this, "Select a world first.", "Train");
                return;
            }

            timer1.Stop();
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            cboWorld.Enabled = true;
            if (btnTrain != null) btnTrain.Enabled = false;
            if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = false;
            btnEvaluate.Enabled = false;
            if (btnEvalTransfer != null) btnEvalTransfer.Enabled = false;
            if (btnPrefToLtm != null) btnPrefToLtm.Enabled = false;
            UseWaitCursor = true;
            try
            {
                Application.DoEvents();
                if (_durableBrain == null) LoadDurableBrain();

                int trainRuns = 100;
                int maxSteps = Evaluator.DefaultMaxSteps;
                var rng = new Random();
                int goals = 0;
                double totalEnergy = 0;
                long decisions = 0;
                int promotedTotal = 0;

                for (int r = 1; r <= trainRuns; r++)
                {
                    var robot = new Robot(_world.StartCell, new Random(rng.Next()));
                    robot.UseBrain = true;
                    robot.LearnEnabled = true;
                    robot.RecordSenseTrace = true;
                    robot.ClearBrainOnReset = false;
                    robot.SetBrain(_durableBrain);

                    int guard = 0;
                    int safety = maxSteps * 20;
                    while (!robot.IsOnGoal && robot.DecisionSteps < maxSteps && guard < safety)
                    {
                        robot.Tick(_world);
                        guard++;
                    }
                    bool goal = robot.IsOnGoal;
                    robot.NotifyEpisodeEnd(goal);
                    if (goal) goals++;
                    promotedTotal += _durableBrain.TrainFromShortTerm(2);

                    totalEnergy += robot.TotalEnergy;
                    decisions += robot.DecisionSteps;

                    if ((r % 10) == 0) Application.DoEvents();
                }

                promotedTotal += _durableBrain.TrainFromShortTerm(3);
                SaveDurableBrain();

                double goalPct = 100.0 * goals / trainRuns;
                double avgEnergy = totalEnergy / trainRuns;
                double avgSteps = (double)decisions / trainRuns;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Train (STM -> LTM) Ã¢â‚¬â€ Current World");
                sb.AppendLine("Policy: " + BrainMemory.PolicyVersion);
                sb.AppendLine("World: " + _world.Name);
                sb.AppendLine("Runs: " + trainRuns + " (learning ON)");
                sb.AppendLine("Goals: " + goals + "/" + trainRuns + " (" + goalPct.ToString("0.0") + "%)");
                sb.AppendLine("Avg energy: " + avgEnergy.ToString("0.0"));
                sb.AppendLine("Avg steps: " + avgSteps.ToString("0.0"));
                sb.AppendLine("Promote deltas (approx): " + promotedTotal);
                sb.AppendLine(_durableBrain.StatusLine());
                sb.AppendLine("Memory saved. Evaluate is separate and will not retrain.");
                MessageBox.Show(this, sb.ToString(), "Train results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Train failed: " + ex.Message, "Train", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                if (btnTrain != null) btnTrain.Enabled = true;
                if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = true;
                btnEvaluate.Enabled = true;
                if (btnEvalTransfer != null) btnEvalTransfer.Enabled = true;
                if (btnPrefToLtm != null) btnPrefToLtm.Enabled = true;
            }
        }

        private void btnTrainCurriculum_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            cboWorld.Enabled = true;
            if (btnTrain != null) btnTrain.Enabled = false;
            if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = false;
            btnEvaluate.Enabled = false;
            if (btnEvalTransfer != null) btnEvalTransfer.Enabled = false;
            if (btnPrefToLtm != null) btnPrefToLtm.Enabled = false;
            UseWaitCursor = true;

            try
            {
                Application.DoEvents();

                _durableBrain = new BrainMemory();
                _durableBrain.MarkEpisodeStart();

                var curriculum = new[] { "Canvas-A", "Canvas-B", "Canvas-C", "Canvas-D" };
                int runsPerWorld = 50;
                int maxSteps = Evaluator.DefaultMaxSteps;
                var rng = new Random();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Curriculum Training (4 worlds)");
                sb.AppendLine("Policy: " + BrainMemory.PolicyVersion);
                sb.AppendLine("Order: " + string.Join(" Ã¢â€ â€™ ", curriculum));
                sb.AppendLine("Runs per world: " + runsPerWorld);
                sb.AppendLine();

                int totalGoals = 0;
                double totalEnergy = 0;
                int totalRuns = 0;

                foreach (string worldName in curriculum)
                {
                    var world = WorldLibrary.GetByName(worldName);
                    int goals = 0;
                    double energy = 0;

                    for (int r = 1; r <= runsPerWorld; r++)
                    {
                        var robot = new Robot(world.StartCell, new Random(rng.Next()));
                        robot.UseBrain = true;
                        robot.LearnEnabled = true;
                        robot.RecordSenseTrace = true;
                        robot.ClearBrainOnReset = false;
                        robot.SetBrain(_durableBrain);

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
                        _durableBrain.TrainFromShortTerm(2);
                    }
                    _durableBrain.TrainFromShortTerm(3);

                    totalGoals += goals;
                    totalEnergy += energy;
                    totalRuns += runsPerWorld;

                    sb.AppendLine(string.Format("{0}: {1}/{2} goals, avg energy {3:0.0}",
                        worldName, goals, runsPerWorld, energy / runsPerWorld));
                    Application.DoEvents();
                }

                SaveDurableBrain();

                sb.AppendLine();
                sb.AppendLine("Total: " + totalGoals + "/" + totalRuns + " goals");
                sb.AppendLine("Overall avg energy: " + (totalEnergy / totalRuns).ToString("0.0"));
                sb.AppendLine(_durableBrain.StatusLine());
                sb.AppendLine("Durable brain saved.");

                MessageBox.Show(this, sb.ToString(), "Curriculum Train", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (_robot != null)
                {
                    _robot.SetBrain(_durableBrain);
                    _robot.ClearBrainOnReset = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Curriculum train failed: " + ex.Message, "Train", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                if (btnTrain != null) btnTrain.Enabled = true;
                if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = true;
                btnEvaluate.Enabled = true;
                if (btnEvalTransfer != null) btnEvalTransfer.Enabled = true;
                if (btnPrefToLtm != null) btnPrefToLtm.Enabled = true;
            }
        }

        private void btnEvaluate_Click(object sender, EventArgs e)
        {
            if (_world == null)
            {
                MessageBox.Show(this, "Select a world first.", "Evaluate");
                return;
            }

            timer1.Stop();
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            cboWorld.Enabled = true;
            btnEvaluate.Enabled = false;
            if (btnTrain != null) btnTrain.Enabled = false;
            if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = false;
            if (btnEvalTransfer != null) btnEvalTransfer.Enabled = false;
            if (btnPrefToLtm != null) btnPrefToLtm.Enabled = false;
            UseWaitCursor = true;
            try
            {
                Application.DoEvents();
                if (_durableBrain == null) LoadDurableBrain();

                int runs = Evaluator.DefaultRuns;
                int maxSteps = Evaluator.DefaultMaxSteps;
                var rng = new Random();

                int stmBefore = _durableBrain.StmCount;
                int ltmBefore = _durableBrain.LtmCount;

                var withMem = RunFrozenBatch(_world, _durableBrain, true, runs, maxSteps, rng);
                var noMem = RunFrozenBatch(_world, _durableBrain, false, runs, maxSteps, rng);

                int stmAfter = _durableBrain.StmCount;
                int ltmAfter = _durableBrain.LtmCount;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Evaluate (frozen, no training) Ã¢â‚¬â€ Same World");
                sb.AppendLine("Policy: " + BrainMemory.PolicyVersion);
                sb.AppendLine("World: " + _world.Name);
                sb.AppendLine("Runs each: " + runs + ", max steps: " + maxSteps);
                sb.AppendLine();
                sb.AppendLine(FormatFrozenBlock("WITH memory (LTM)", withMem));
                sb.AppendLine();
                sb.AppendLine(FormatFrozenBlock("WITHOUT memory", noMem));
                sb.AppendLine();
                sb.AppendLine("Comparison (energy to goal, 100 runs each)");
                sb.AppendLine("  Mean energy WITH memory:    " + withMem.Score.ToString("0.0"));
                sb.AppendLine("  Mean energy WITHOUT memory: " + noMem.Score.ToString("0.0"));
                string winner;
                if (Math.Abs(withMem.Score - noMem.Score) < 1.0)
                    winner = "Roughly tied on energy-to-goal.";
                else if (withMem.Score < noMem.Score)
                    winner = "Winner: WITH memory (lower energy to goal).";
                else
                    winner = "Winner: WITHOUT memory (lower energy to goal).";
                sb.AppendLine("  " + winner);
                sb.AppendLine("  Goal delta: " + (withMem.Goals - noMem.Goals).ToString("+0;-0;0") +
                    "  Success-energy delta: " +
                    ((withMem.Goals > 0 && noMem.Goals > 0)
                        ? (withMem.AvgEnergyToGoal - noMem.AvgEnergyToGoal).ToString("+0.0;-0.0;0.0")
                        : "n/a"));
                sb.AppendLine();
                sb.AppendLine("Score = mean energy to goal over all runs;");
                sb.AppendLine("timeouts count as maxEnergy. Lower is better.");
                sb.AppendLine("Memory unchanged: STM " + stmBefore + "->" + stmAfter +
                    ", LTM " + ltmBefore + "->" + ltmAfter);
                sb.AppendLine(_durableBrain.StatusLine());

                MessageBox.Show(this, sb.ToString(), "Evaluate results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Evaluate failed: " + ex.Message, "Evaluate", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                btnEvaluate.Enabled = true;
                if (btnTrain != null) btnTrain.Enabled = true;
                if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = true;
                if (btnEvalTransfer != null) btnEvalTransfer.Enabled = true;
                if (btnPrefToLtm != null) btnPrefToLtm.Enabled = true;
            }
        }

        /// <summary>
        /// Headless Prefâ†’LTM: run many episodes on Canvas-A..D with learning, aggregate
        /// exact 8-neighbor senseâ†’preferred move votes (energy/success-weighted), flush argmax
        /// into LTM, save memory-dat\brain.mem, then freeze and transfer-eval vs no-brain.
        /// Does not replace Train / Eval Same / Eval Transfer â€” runs alongside.
        /// </summary>
        private void btnPrefToLtm_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            cboWorld.Enabled = true;
            btnEvaluate.Enabled = false;
            if (btnTrain != null) btnTrain.Enabled = false;
            if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = false;
            if (btnEvalTransfer != null) btnEvalTransfer.Enabled = false;
            if (btnPrefToLtm != null) btnPrefToLtm.Enabled = false;
            UseWaitCursor = true;

            try
            {
                Application.DoEvents();
                if (string.IsNullOrEmpty(_memoryPath))
                    _memoryPath = BrainMemory.DefaultMemoryPath();

                var log = new System.Text.StringBuilder();
                var rng = new Random();

                var result = Evaluator.RunPreferredMoveHeadless(
                    WorldLibrary.All,
                    Evaluator.PrefToLtmEpisodesPerWorld,
                    Evaluator.PrefToLtmEvalRuns,
                    Evaluator.DefaultMaxSteps,
                    rng,
                    log,
                    _memoryPath,
                    msg =>
                    {
                        lblCount.Text = msg;
                        Application.DoEvents();
                    });

                _durableBrain = result.Brain;
                if (_robot != null)
                {
                    _robot.SetBrain(_durableBrain);
                    _robot.ClearBrainOnReset = false;
                }

                string logPath = Evaluator.SavePrefToLtmLog(log);
                string matrix = result.Transfer != null
                    ? Evaluator.FormatTransferMatrix(result.Transfer, WorldLibrary.All)
                    : "(no transfer matrix)";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Headless Prefâ†’LTM");
                sb.AppendLine("Policy: " + BrainMemory.PolicyVersion);
                sb.AppendLine("Episodes/world: " + Evaluator.PrefToLtmEpisodesPerWorld + " Ã— " + WorldLibrary.All.Count);
                sb.AppendLine("Train goals: " + result.TrainGoals + "/" + result.TrainEpisodes);
                sb.AppendLine("Avg train energy: " + result.TrainAvgEnergy.ToString("0.0"));
                sb.AppendLine("Preferred signatures: " + result.PreferredSignatures);
                sb.AppendLine("LTM rules written/merged: " + result.RulesWritten);
                sb.AppendLine(_durableBrain.StatusLine());
                sb.AppendLine("Memory: " + (result.MemoryPath ?? _memoryPath));
                sb.AppendLine();
                sb.AppendLine(matrix);
                sb.AppendLine();
                sb.AppendLine("Log: " + logPath);
                sb.AppendLine();
                sb.AppendLine("Weighting: smooth/medium preferred; vote *= 1/(1+energyDelta);");
                sb.AppendLine("goal episodes get extra boost. Exact signature match only.");

                MessageBox.Show(this, sb.ToString(), "Headless Prefâ†’LTM", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Prefâ†’LTM failed: " + ex.Message, "Headless Prefâ†’LTM", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                btnEvaluate.Enabled = true;
                if (btnTrain != null) btnTrain.Enabled = true;
                if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = true;
                if (btnEvalTransfer != null) btnEvalTransfer.Enabled = true;
                if (btnPrefToLtm != null) btnPrefToLtm.Enabled = true;
                UpdateStatusLabels();
            }
        }

        private void btnEvalTransfer_Click(object sender, EventArgs e)

        {
            if (_world == null)
            {
                MessageBox.Show(this, "Select a training world first.", "Transfer Evaluate");
                return;
            }

            timer1.Stop();
            btnStart.Enabled = true;
            btnPause.Enabled = false;
            cboWorld.Enabled = true;
            btnEvaluate.Enabled = false;
            if (btnTrain != null) btnTrain.Enabled = false;
            if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = false;
            if (btnEvalTransfer != null) btnEvalTransfer.Enabled = false;
            if (btnPrefToLtm != null) btnPrefToLtm.Enabled = false;
            UseWaitCursor = true;

            try
            {
                Application.DoEvents();
                var log = new System.Text.StringBuilder();
                var rng = new Random();

                var result = Evaluator.RunTransferMatrix(
                    _world,
                    WorldLibrary.All,
                    Evaluator.TransferTrainRuns,
                    Evaluator.TransferEvalRuns,
                    Evaluator.DefaultMaxSteps,
                    rng,
                    log,
                    msg =>
                    {
                        lblCount.Text = msg;
                        Application.DoEvents();
                    });

                string logPath = Evaluator.SaveTransferLog(log);
                string summary = Evaluator.FormatTransferMatrix(result, WorldLibrary.All);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Transfer Evaluation");
                sb.AppendLine();
                sb.AppendLine(summary);
                sb.AppendLine();
                sb.AppendLine("Log saved: " + logPath);

                MessageBox.Show(this, sb.ToString(), "Transfer Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Transfer eval failed: " + ex.Message, "Transfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                btnEvaluate.Enabled = true;
                if (btnTrain != null) btnTrain.Enabled = true;
                if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = true;
                if (btnEvalTransfer != null) btnEvalTransfer.Enabled = true;
                if (btnPrefToLtm != null) btnPrefToLtm.Enabled = true;
                UpdateStatusLabels();
            }
        }

        sealed class FrozenBatchResult
        {
            public int Runs;
            public int Goals;
            public double TotalEnergy, EnergyToGoalSum;
            public long Decisions;
            public double GoalPct { get { return Runs <= 0 ? 0 : 100.0 * Goals / Runs; } }
            public double AvgEnergy { get { return Runs <= 0 ? 0 : TotalEnergy / Runs; } }
            public double AvgDecisions { get { return Runs <= 0 ? 0 : (double)Decisions / Runs; } }
            public double AvgEnergyToGoal { get { return Goals <= 0 ? 0 : EnergyToGoalSum / Goals; } }
            public int Timeouts { get { return Runs - Goals; } }
            public int MaxSteps;
            public double MaxEnergy;
            public double Score
            {
                get
                {
                    if (Runs <= 0) return 0;
                    double failEnergy = Timeouts * MaxEnergy;
                    return (EnergyToGoalSum + failEnergy) / Runs;
                }
            }
        }

        FrozenBatchResult RunFrozenBatch(World world, BrainMemory durable, bool useMemory, int runs, int maxSteps, Random rng)
        {
            double maxEnergy = maxSteps * 1.0;
            var totals = new FrozenBatchResult { Runs = runs, MaxSteps = maxSteps, MaxEnergy = maxEnergy };
            for (int i = 1; i <= runs; i++)
            {
                var robot = new Robot(world.StartCell, new Random(rng.Next()));
                robot.UseBrain = useMemory;
                robot.LearnEnabled = false;
                robot.RecordSenseTrace = false;
                robot.ClearBrainOnReset = false;
                if (useMemory)
                    robot.SetBrain(durable.Clone());
                else
                    robot.SetBrain(new BrainMemory());

                int guard = 0;
                int safety = maxSteps * 20;
                while (!robot.IsOnGoal && robot.DecisionSteps < maxSteps && guard < safety)
                {
                    robot.Tick(world);
                    guard++;
                }
                if (robot.IsOnGoal)
                {
                    totals.Goals++;
                    totals.EnergyToGoalSum += robot.TotalEnergy;
                }
                totals.TotalEnergy += robot.TotalEnergy;
                totals.Decisions += robot.DecisionSteps;
                if ((i % 20) == 0) Application.DoEvents();
            }
            return totals;
        }

        static string FormatFrozenBlock(string title, FrozenBatchResult t)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine("  Goals: " + t.Goals + "/" + t.Runs + " (" + t.GoalPct.ToString("0.0") + "%)  timeouts: " + t.Timeouts);
            sb.AppendLine("  Mean energy to goal (all runs): " + t.Score.ToString("0.0") + "   [lower is better]");
            if (t.Goals > 0)
                sb.AppendLine("  Mean energy among successes: " + t.AvgEnergyToGoal.ToString("0.0"));
            else
                sb.AppendLine("  Mean energy among successes: n/a (no goals)");
            sb.AppendLine("  Avg decisions: " + t.AvgDecisions.ToString("0.0"));
            sb.AppendLine("  Avg energy: " + t.AvgEnergy.ToString("0.0"));
            return sb.ToString();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "LifeSim - feel/motor STM + world-tagged feel-atlas LTM.\n\n" +
                "Policy: " + BrainMemory.PolicyVersion + "\n\n" +
                "Key V11 changes:\n" +
                "Ã¢â‚¬Â¢ Brain leads terrain preference when UseBrain=true\n" +
                "Ã¢â‚¬Â¢ Motor-feel multipliers shrunk to let brain dominate\n" +
                "Ã¢â‚¬Â¢ Episode-end credit based on total energy (not feel-only)\n" +
                "Ã¢â‚¬Â¢ Curriculum training across all 4 worlds\n" +
                "Ã¢â‚¬Â¢ Transfer evaluation: train A, test A+B+C+D\n\n" +
                "Terrain: 0=smooth (fast), 1=rough (slow)\n" +
                "Shared brain persists in memory-dat/brain.mem",
                "LifeSim",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LifeSim_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            count++;
            if (_robot != null && _world != null)
            {
                Point before = _robot.Cell;
                bool moved = _robot.Tick(_world);
                if (_robot.DecisionSteps > 0 && (_robot.DecisionSteps % 25) == 0) SaveDurableBrain();
                if (moved)
                {
                    Point after = _robot.Cell;
                    InvalidateRobotCells(before, after);
                    _lastRobotCell = after;
                }

                if (_robot.IsOnGoal)
                {
                    if (_durableBrain != null && _robot != null && _robot.LearnEnabled)
                    {
                        _robot.NotifyEpisodeEnd(true);
                        SaveDurableBrain();
                    }
                    timer1.Stop();
                    btnStart.Enabled = true;
                    btnPause.Enabled = false;
                    cboWorld.Enabled = true;
                    UpdateStatusLabels();
                    return;
                }
            }
            if (count % 5 == 0 || (_robot != null && _robot.IsOnGoal))
            {
                UpdateStatusLabels();
            }
        }

        void LoadDurableBrain()
        {
            _memoryPath = BrainMemory.DefaultMemoryPath();
            try
            {
                _durableBrain = BrainMemory.LoadFromFile(_memoryPath);
            }
            catch
            {
                _durableBrain = new BrainMemory();
            }
            if (_robot != null)
            {
                _robot.SetBrain(_durableBrain);
                _robot.ClearBrainOnReset = false;
            }
        }

        void SaveDurableBrain()
        {
            try
            {
                if (_durableBrain == null) return;
                if (string.IsNullOrEmpty(_memoryPath))
                    _memoryPath = BrainMemory.DefaultMemoryPath();
                _durableBrain.SaveToFile(_memoryPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Memory save failed: " + ex.Message, "Mem");
            }
        }

        void Life_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveDurableBrain();
        }

        private void btnTeacherSuccess_Click(object sender, EventArgs e)
        {
            var choice = MessageBox.Show(
                "TeacherSuccessâ†’LTM on Primary (Terrain-A..D).\n\nYes = Leave-one-out\nNo = In-sample\nCancel = abort\n\nOnly teacher success paths enter LTM. Fix2 honesty on frozen eval.",
                "TeacherSuccessâ†’LTM",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (choice == DialogResult.Cancel) return;
            bool leaveOneOut = (choice == DialogResult.Yes);

            SetLongRunningUiEnabled(false);
            var worker = new System.ComponentModel.BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += (s, args) =>
            {
                var worlds = new System.Collections.Generic.List<World>(WorldLibrary.Primary);
                var log = new System.Text.StringBuilder();
                string memDir = Path.Combine(Evaluator.DefaultLogDirectory, "teacher_ui");
                Directory.CreateDirectory(memDir);
                string memPath = Path.Combine(memDir, "teacher_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mem");
                var summary = new System.Text.StringBuilder();
                summary.AppendLine(leaveOneOut ? "TeacherSuccess LOO (goal-known)" : "TeacherSuccess in-sample (goal-known)");

                if (!leaveOneOut)
                {
                    var result = Evaluator.RunTeacherSuccessHeadless(
                        worlds, 200, 20, Evaluator.DefaultMaxSteps, new Random(42), log, memPath,
                        msg => worker.ReportProgress(0, msg), false);
                    summary.AppendLine("ltm=" + result.Brain.LtmCount + " rules=" + result.RulesWritten +
                        " sigs=" + result.PreferredSignatures);
                    summary.AppendLine(Evaluator.FormatTransferMatrix(result.Transfer, worlds));
                    summary.AppendLine("mem=" + memPath);
                    args.Result = new object[] { result.Brain, summary.ToString() };
                }
                else
                {
                    for (int hi = 0; hi < worlds.Count; hi++)
                    {
                        var held = worlds[hi];
                        var train = new System.Collections.Generic.List<World>();
                        for (int i = 0; i < worlds.Count; i++) if (i != hi) train.Add(worlds[i]);
                        worker.ReportProgress(0, "LOO held " + held.Name);
                        var result = Evaluator.RunTeacherSuccessHeadless(
                            train, 150, 1, Evaluator.DefaultMaxSteps, new Random(100 + hi), log, null, null, false);
                        EvalRunTotals wb, nb;
                        Evaluator.EvalFrozenPaired(held, result.Brain, 20, Evaluator.DefaultMaxSteps, 500 + hi,
                            out wb, out nb, false);
                        double gain = nb.EnergyScore - wb.EnergyScore;
                        double pct = Math.Abs(nb.EnergyScore) < 1e-9 ? 0 : 100.0 * gain / nb.EnergyScore;
                        summary.AppendLine(string.Format("{0}: brain {1:0.0} vs nobrain {2:0.0} ({3:+0.0}%) ltm={4}",
                            held.Name, wb.EnergyScore, nb.EnergyScore, pct, result.Brain.LtmCount));
                    }
                    args.Result = new object[] { null, summary.ToString() };
                }
            };
            worker.ProgressChanged += (s, args) =>
            {
                try { UpdateStatusLabels(); } catch { }
            };
            worker.RunWorkerCompleted += (s, args) =>
            {
                SetLongRunningUiEnabled(true);
                if (args.Error != null)
                {
                    MessageBox.Show(args.Error.ToString(), "TeacherSuccess error");
                    return;
                }
                var pack = args.Result as object[];
                if (pack != null && pack[0] is BrainMemory)
                {
                    var brain = (BrainMemory)pack[0];
                    // Install frozen LTM into live robot if possible
                    try
                    {
                        _durableBrain = brain.CloneFrozenLtmOnly();
                        if (_robot != null) _robot.SetBrain(_durableBrain.CloneFrozenLtmOnly());
                    }
                    catch { }
                }
                MessageBox.Show(pack != null ? Convert.ToString(pack[1]) : "(done)", "TeacherSuccessâ†’LTM");
                UpdateStatusLabels();
            };
            worker.RunWorkerAsync();
        }

        void SetLongRunningUiEnabled(bool enabled)
        {
            if (btnStart != null) btnStart.Enabled = enabled;
            if (btnPause != null) btnPause.Enabled = enabled;
            if (btnReset != null) btnReset.Enabled = enabled;
            if (btnTrain != null) btnTrain.Enabled = enabled;
            if (btnTrainCurriculum != null) btnTrainCurriculum.Enabled = enabled;
            if (btnEvaluate != null) btnEvaluate.Enabled = enabled;
            if (btnEvalTransfer != null) btnEvalTransfer.Enabled = enabled;
            if (btnPrefToLtm != null) btnPrefToLtm.Enabled = enabled;
            if (btnTeacherSuccess != null) btnTeacherSuccess.Enabled = enabled;
            if (btnMemReset != null) btnMemReset.Enabled = enabled;
            UseWaitCursor = !enabled;
        }

        private void btnMemReset_Click(object sender, EventArgs e)
        {
            var ans = MessageBox.Show(this,
                "Clear short-term and long-term memory and delete memory-dat?",
                "Mem-Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (ans != DialogResult.Yes) return;
            if (_durableBrain == null) _durableBrain = new BrainMemory();
            _durableBrain.Clear();
            if (_robot != null)
            {
                _robot.SetBrain(_durableBrain);
                _robot.ClearBrainOnReset = false;
            }
            try
            {
                if (string.IsNullOrEmpty(_memoryPath))
                    _memoryPath = BrainMemory.DefaultMemoryPath();
                if (File.Exists(_memoryPath))
                    File.Delete(_memoryPath);
            }
            catch { }
            MessageBox.Show(this, "Memory cleared.\n" + (_memoryPath ?? ""), "Mem-Reset");
            UpdateStatusLabels();
        }

        void UpdateStatusLabels()
        {
            if (lblCount == null) return;
            if (_robot == null)
            {
                lblCount.Text = "Count: " + count;
                return;
            }
            double sps = _world != null ? _robot.CurrentStepsPerSec(_world) : 0;
            lblCount.Text =
                "t=" + count + "\n" +
                "feel-atlas\n" +
                "pos=(" + _robot.Cell.X + "," + _robot.Cell.Y + ")\n" +
                "energy=" + _robot.TotalEnergy.ToString("0.0") + "\n" +
                "ltm=" + (_durableBrain != null ? _durableBrain.LtmCount.ToString() : "-") + "\n" +
                "blue=" + _robot.GoalTouches + (_robot.IsOnGoal ? " *" : "");
        }
        private void RebuildBackdrop()
        {
            if (_backdrop != null)
            {
                _backdrop.Dispose();
                _backdrop = null;
            }

            int w = Math.Max(1, simulationArea.ClientSize.Width);
            int h = Math.Max(1, simulationArea.ClientSize.Height);
            _backdrop = new Bitmap(w, h);

            using (var g = Graphics.FromImage(_backdrop))
            {
                g.Clear(Color.FromArgb(18, 20, 24));
                if (_world == null)
                {
                    return;
                }

                float cellW = (float)w / _world.Cols;
                float cellH = (float)h / _world.Rows;
                _cell = Math.Min(cellW, cellH);
                _ox = (w - _cell * _world.Cols) / 2f;
                _oy = (h - _cell * _world.Rows) / 2f;

                using (var obstacle = new SolidBrush(Color.FromArgb(140, 70, 60)))
                using (var startBrush = new SolidBrush(Color.FromArgb(46, 160, 90)))
                using (var goalBrush = new SolidBrush(Color.FromArgb(70, 130, 220)))
                using (var gridPen = new Pen(Color.FromArgb(40, 44, 54), 1f))
                {
                    for (int cx = 0; cx < _world.Cols; cx++)
                    {
                        for (int cy = 0; cy < _world.Rows; cy++)
                        {
                            float px = _ox + cx * _cell;
                            float py = _oy + cy * _cell;
                            var cellRect = new RectangleF(px, py, _cell, _cell);

                            if (_world.CellHasObstacle(cx, cy))
                            {
                                g.FillRectangle(obstacle, cellRect);
                            }
                            else
                            {
                                float roughness = _world.CellRoughness(cx, cy);
                                Color terrain = CanvasWorldGenerator.TerrainColor(roughness);
                                using (var terrainBrush = new SolidBrush(terrain))
                                {
                                    g.FillRectangle(terrainBrush, cellRect);
                                }
                            }
                        }
                    }

                    for (int x = 0; x <= _world.Cols; x++)
                    {
                        float px = _ox + x * _cell;
                        g.DrawLine(gridPen, px, _oy, px, _oy + _world.Rows * _cell);
                    }
                    for (int y = 0; y <= _world.Rows; y++)
                    {
                        float py = _oy + y * _cell;
                        g.DrawLine(gridPen, _ox, py, _ox + _world.Cols * _cell, py);
                    }

                    var startRect = new RectangleF(_ox + _world.StartCell.X * _cell, _oy + _world.StartCell.Y * _cell, _cell, _cell);
                    g.FillEllipse(startBrush, startRect);
                    var goalRect = new RectangleF(_ox + _world.GoalCell.X * _cell, _oy + _world.GoalCell.Y * _cell, _cell, _cell);
                    g.FillEllipse(goalBrush, goalRect);

                    using (var labelFont = new Font("Segoe UI", Math.Max(7f, _cell * 0.35f), FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var labelBrush = new SolidBrush(Color.White))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("S", labelFont, labelBrush, startRect, sf);
                        g.DrawString("D", labelFont, labelBrush, goalRect, sf);
                    }
                }

                using (var font = new Font("Segoe UI", 9f))
                using (var textBrush = new SolidBrush(Color.FromArgb(200, 200, 210)))
                {
                    string line =
                        $"{_world.Name} | V11-EpisodeEnergy | yellow=robot  green=start  blue=goal | " +
                        "heatmap: blue/black=smooth  yellow/white=rough";
                    g.DrawString(line, font, textBrush, 8, 8);
                }
            }
        }

        private void InvalidateRobotCells(Point a, Point b)
        {
            if (_cell <= 0)
            {
                simulationArea.Invalidate();
                return;
            }

            Rectangle ra = Rectangle.Round(new RectangleF(_ox + a.X * _cell, _oy + a.Y * _cell, _cell, _cell));
            Rectangle rb = Rectangle.Round(new RectangleF(_ox + b.X * _cell, _oy + b.Y * _cell, _cell, _cell));
            ra.Inflate(2, 2);
            rb.Inflate(2, 2);
            simulationArea.Invalidate(Rectangle.Union(ra, rb));
        }

        private void simulationArea_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            if (_backdrop != null)
            {
                g.DrawImageUnscaled(_backdrop, 0, 0);
            }
            else
            {
                g.Clear(Color.FromArgb(18, 20, 24));
            }

            if (_robot != null && _world != null && _cell > 0)
            {
                using (var robotBrush = new SolidBrush(Color.FromArgb(240, 200, 60)))
                using (var robotOutline = new Pen(Color.FromArgb(255, 240, 160), 2f))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    var rr = new RectangleF(_ox + _robot.Cell.X * _cell, _oy + _robot.Cell.Y * _cell, _cell, _cell);
                    float pad = _cell * 0.18f;
                    var body = new RectangleF(rr.X + pad, rr.Y + pad, rr.Width - 2 * pad, rr.Height - 2 * pad);
                    g.FillEllipse(robotBrush, body);
                    g.DrawEllipse(robotOutline, body);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_backdrop != null)
            {
                _backdrop.Dispose();
                _backdrop = null;
            }
            base.OnFormClosed(e);
        }
    }
}


