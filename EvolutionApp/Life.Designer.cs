namespace Evolution
{
    partial class LifeSim
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.simulationArea = new Evolution.CanvasPanel();
            this.menuBar = new System.Windows.Forms.Panel();
            this.flowMenu = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnTrain = new System.Windows.Forms.Button();
            this.btnTrainCurriculum = new System.Windows.Forms.Button();
            this.btnEvaluate = new System.Windows.Forms.Button();
            this.btnEvalTransfer = new System.Windows.Forms.Button();
            this.btnPrefToLtm = new System.Windows.Forms.Button();
            this.btnTeacherSuccess = new System.Windows.Forms.Button();
            this.btnMemReset = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.lblCount = new System.Windows.Forms.Label();
            this.lblWorld = new System.Windows.Forms.Label();
            this.cboWorld = new System.Windows.Forms.ComboBox();
            this.menuBar.SuspendLayout();
            this.flowMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // simulationArea
            // 
            this.simulationArea.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(20)))), ((int)(((byte)(24)))));
            this.simulationArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.simulationArea.Location = new System.Drawing.Point(0, 0);
            this.simulationArea.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.simulationArea.Name = "simulationArea";
            this.simulationArea.Size = new System.Drawing.Size(705, 525);
            this.simulationArea.TabIndex = 0;
            // 
            // menuBar
            // 
            this.menuBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(44)))));
            this.menuBar.Controls.Add(this.flowMenu);
            this.menuBar.Controls.Add(this.btnExit);
            this.menuBar.Dock = System.Windows.Forms.DockStyle.Right;
            this.menuBar.Location = new System.Drawing.Point(705, 0);
            this.menuBar.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.menuBar.Name = "menuBar";
            this.menuBar.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.menuBar.Size = new System.Drawing.Size(150, 525);
            this.menuBar.TabIndex = 1;
            // 
            // flowMenu
            // 
            this.flowMenu.BackColor = System.Drawing.Color.Transparent;
            this.flowMenu.Controls.Add(this.lblTitle);
            this.flowMenu.Controls.Add(this.lblWorld);
            this.flowMenu.Controls.Add(this.cboWorld);
            this.flowMenu.Controls.Add(this.btnStart);
            this.flowMenu.Controls.Add(this.btnPause);
            this.flowMenu.Controls.Add(this.btnReset);
            this.flowMenu.Controls.Add(this.btnSettings);
            this.flowMenu.Controls.Add(this.btnTrain);
            this.flowMenu.Controls.Add(this.btnTrainCurriculum);
            this.flowMenu.Controls.Add(this.btnEvaluate);
            this.flowMenu.Controls.Add(this.btnEvalTransfer);
            this.flowMenu.Controls.Add(this.btnPrefToLtm);
            this.flowMenu.Controls.Add(this.btnTeacherSuccess);
            this.flowMenu.Controls.Add(this.btnMemReset);
            this.flowMenu.Controls.Add(this.lblCount);
            this.flowMenu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowMenu.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowMenu.Location = new System.Drawing.Point(8, 8);
            this.flowMenu.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.flowMenu.Name = "flowMenu";
            this.flowMenu.Size = new System.Drawing.Size(104, 484);
            this.flowMenu.TabIndex = 0;
            this.flowMenu.AutoScroll = true;
            this.flowMenu.WrapContents = false;
            this.flowMenu.Resize += new System.EventHandler(this.flowMenu_Resize);
            // 
            // lblTitle
            // 
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 14F);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 4);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0, 4, 0, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(104, 21);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "LifeSim V16";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblWorld
            // 
            this.lblWorld.ForeColor = System.Drawing.Color.White;
            this.lblWorld.Location = new System.Drawing.Point(0, 37);
            this.lblWorld.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.lblWorld.Name = "lblWorld";
            this.lblWorld.Size = new System.Drawing.Size(104, 16);
            this.lblWorld.TabIndex = 6;
            this.lblWorld.Text = "World";
            this.lblWorld.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboWorld
            // 
            this.cboWorld.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboWorld.FormattingEnabled = true;
            this.cboWorld.Location = new System.Drawing.Point(0, 55);
            this.cboWorld.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.cboWorld.Name = "cboWorld";
            this.cboWorld.Size = new System.Drawing.Size(104, 21);
            this.cboWorld.TabIndex = 7;
            this.cboWorld.SelectedIndexChanged += new System.EventHandler(this.cboWorld_SelectedIndexChanged);
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(160)))), ((int)(((byte)(90)))));
            this.btnStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStart.FlatAppearance.BorderSize = 0;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(0, 37);
            this.btnStart.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(104, 25);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnPause
            // 
            this.btnPause.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(60)))), ((int)(((byte)(72)))));
            this.btnPause.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPause.Enabled = false;
            this.btnPause.FlatAppearance.BorderSize = 0;
            this.btnPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPause.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnPause.ForeColor = System.Drawing.Color.White;
            this.btnPause.Location = new System.Drawing.Point(0, 68);
            this.btnPause.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(104, 25);
            this.btnPause.TabIndex = 2;
            this.btnPause.Text = "Pause";
            this.btnPause.UseVisualStyleBackColor = false;
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // btnReset
            // 
            this.btnReset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(60)))), ((int)(((byte)(72)))));
            this.btnReset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReset.FlatAppearance.BorderSize = 0;
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnReset.ForeColor = System.Drawing.Color.White;
            this.btnReset.Location = new System.Drawing.Point(0, 99);
            this.btnReset.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(104, 25);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = false;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(60)))), ((int)(((byte)(72)))));
            this.btnSettings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.Location = new System.Drawing.Point(0, 130);
            this.btnSettings.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(104, 25);
            this.btnSettings.TabIndex = 4;
            this.btnSettings.Text = "About";
            this.btnSettings.UseVisualStyleBackColor = false;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnTrain
            // 
            this.btnTrain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.btnTrain.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTrain.FlatAppearance.BorderSize = 0;
            this.btnTrain.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTrain.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnTrain.ForeColor = System.Drawing.Color.White;
            this.btnTrain.Location = new System.Drawing.Point(0, 161);
            this.btnTrain.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.btnTrain.Name = "btnTrain";
            this.btnTrain.Size = new System.Drawing.Size(104, 26);
            this.btnTrain.TabIndex = 5;
            this.btnTrain.Text = "Train (World)";
            this.btnTrain.UseVisualStyleBackColor = false;
            this.btnTrain.Click += new System.EventHandler(this.btnTrain_Click);
            // 
            // btnTrainCurriculum
            // 
            this.btnTrainCurriculum.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(100)))), ((int)(((byte)(130)))));
            this.btnTrainCurriculum.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTrainCurriculum.FlatAppearance.BorderSize = 0;
            this.btnTrainCurriculum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTrainCurriculum.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnTrainCurriculum.ForeColor = System.Drawing.Color.White;
            this.btnTrainCurriculum.Location = new System.Drawing.Point(0, 191);
            this.btnTrainCurriculum.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.btnTrainCurriculum.Name = "btnTrainCurriculum";
            this.btnTrainCurriculum.Size = new System.Drawing.Size(104, 26);
            this.btnTrainCurriculum.TabIndex = 12;
            this.btnTrainCurriculum.Text = "Train (Curriculum)";
            this.btnTrainCurriculum.UseVisualStyleBackColor = false;
            this.btnTrainCurriculum.Click += new System.EventHandler(this.btnTrainCurriculum_Click);
            // 
            // btnEvaluate
            // 
            this.btnEvaluate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(140)))));
            this.btnEvaluate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEvaluate.FlatAppearance.BorderSize = 0;
            this.btnEvaluate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEvaluate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnEvaluate.ForeColor = System.Drawing.Color.White;
            this.btnEvaluate.Location = new System.Drawing.Point(0, 221);
            this.btnEvaluate.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.btnEvaluate.Name = "btnEvaluate";
            this.btnEvaluate.Size = new System.Drawing.Size(104, 26);
            this.btnEvaluate.TabIndex = 8;
            this.btnEvaluate.Text = "Eval (Same)";
            this.btnEvaluate.UseVisualStyleBackColor = false;
            this.btnEvaluate.Click += new System.EventHandler(this.btnEvaluate_Click);
            // 
            // btnEvalTransfer
            // 
            this.btnEvalTransfer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(70)))), ((int)(((byte)(140)))));
            this.btnEvalTransfer.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEvalTransfer.FlatAppearance.BorderSize = 0;
            this.btnEvalTransfer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEvalTransfer.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnEvalTransfer.ForeColor = System.Drawing.Color.White;
            this.btnEvalTransfer.Location = new System.Drawing.Point(0, 251);
            this.btnEvalTransfer.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.btnEvalTransfer.Name = "btnEvalTransfer";
            this.btnEvalTransfer.Size = new System.Drawing.Size(104, 26);
            this.btnEvalTransfer.TabIndex = 13;
            this.btnEvalTransfer.Text = "Eval (Transfer)";
            this.btnEvalTransfer.UseVisualStyleBackColor = false;
            this.btnEvalTransfer.Click += new System.EventHandler(this.btnEvalTransfer_Click);
            // 
            // btnPrefToLtm
            // 
            this.btnPrefToLtm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(110)))), ((int)(((byte)(90)))));
            this.btnPrefToLtm.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrefToLtm.FlatAppearance.BorderSize = 0;
            this.btnPrefToLtm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrefToLtm.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPrefToLtm.ForeColor = System.Drawing.Color.White;
            this.btnPrefToLtm.Location = new System.Drawing.Point(0, 281);
            this.btnPrefToLtm.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.btnPrefToLtm.Name = "btnPrefToLtm";
            this.btnPrefToLtm.Size = new System.Drawing.Size(104, 26);
            this.btnPrefToLtm.TabIndex = 14;
            this.btnPrefToLtm.Text = "Headless Prefâ†’LTM";
            this.btnPrefToLtm.UseVisualStyleBackColor = false;
            this.btnPrefToLtm.Click += new System.EventHandler(this.btnPrefToLtm_Click);
            //
            // btnTeacherSuccess
            //
            this.btnTeacherSuccess.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(140)))));
            this.btnTeacherSuccess.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTeacherSuccess.FlatAppearance.BorderSize = 0;
            this.btnTeacherSuccess.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTeacherSuccess.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnTeacherSuccess.ForeColor = System.Drawing.Color.White;
            this.btnTeacherSuccess.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.btnTeacherSuccess.Name = "btnTeacherSuccess";
            this.btnTeacherSuccess.Size = new System.Drawing.Size(104, 26);
            this.btnTeacherSuccess.TabIndex = 15;
            this.btnTeacherSuccess.Text = "TeacherSuccessâ†’LTM";
            this.btnTeacherSuccess.UseVisualStyleBackColor = false;
            this.btnTeacherSuccess.Click += new System.EventHandler(this.btnTeacherSuccess_Click);
            // 
            // btnMemReset
            // 
            this.btnMemReset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnMemReset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMemReset.FlatAppearance.BorderSize = 0;
            this.btnMemReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMemReset.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnMemReset.ForeColor = System.Drawing.Color.White;
            this.btnMemReset.Location = new System.Drawing.Point(0, 281);
            this.btnMemReset.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.btnMemReset.Name = "btnMemReset";
            this.btnMemReset.Size = new System.Drawing.Size(104, 26);
            this.btnMemReset.TabIndex = 9;
            this.btnMemReset.Text = "Mem-Reset";
            this.btnMemReset.UseVisualStyleBackColor = false;
            this.btnMemReset.Click += new System.EventHandler(this.btnMemReset_Click);
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(56)))), ((int)(((byte)(56)))));
            this.btnExit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExit.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnExit.ForeColor = System.Drawing.Color.White;
            this.btnExit.Location = new System.Drawing.Point(8, 492);
            this.btnExit.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(104, 25);
            this.btnExit.TabIndex = 10;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 20;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // lblCount
            // 
            this.lblCount.AutoSize = false;
            this.lblCount.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblCount.Location = new System.Drawing.Point(3, 313);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(104, 160);
            this.lblCount.TabIndex = 11;
            this.lblCount.Text = "Count";
            // 
            // LifeSim
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(20)))), ((int)(((byte)(24)))));
            this.ClientSize = new System.Drawing.Size(855, 525);
            this.Controls.Add(this.simulationArea);
            this.Controls.Add(this.menuBar);
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MinimumSize = new System.Drawing.Size(458, 331);
            this.Name = "LifeSim";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LifeSim V16";
            this.Load += new System.EventHandler(this.LifeSim_Load);
            this.Shown += new System.EventHandler(this.LifeSim_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LifeSim_KeyDown);
            this.menuBar.ResumeLayout(false);
            this.flowMenu.ResumeLayout(false);
            this.flowMenu.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private Evolution.CanvasPanel simulationArea;
        private System.Windows.Forms.Panel menuBar;
        private System.Windows.Forms.FlowLayoutPanel flowMenu;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnTrain;
        private System.Windows.Forms.Button btnTrainCurriculum;
        private System.Windows.Forms.Button btnEvaluate;
        private System.Windows.Forms.Button btnEvalTransfer;
        private System.Windows.Forms.Button btnPrefToLtm;
        private System.Windows.Forms.Button btnTeacherSuccess;
        private System.Windows.Forms.Button btnMemReset;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.Label lblWorld;
        private System.Windows.Forms.ComboBox cboWorld;
    }
}

