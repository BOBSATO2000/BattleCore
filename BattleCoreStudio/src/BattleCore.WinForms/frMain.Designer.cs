namespace BattleCoreStudio
{
    partial class frMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            menuStrip = new MenuStrip();
            menuFile = new ToolStripMenuItem();
            menuNew = new ToolStripMenuItem();
            menuSave = new ToolStripMenuItem();
            menuSaveAs = new ToolStripMenuItem();
            menuLoad = new ToolStripMenuItem();
            menuSep = new ToolStripSeparator();
            menuExit = new ToolStripMenuItem();
            menuSim = new ToolStripMenuItem();
            menuSimRun = new ToolStripMenuItem();
            panel1 = new Panel();
            btnStep = new Button();
            btnAuto = new Button();
            btnStop = new Button();
            btnRestart = new Button();
            btnConfirm = new Button();
            lblSpeed = new Label();
            cmbSpeed = new ComboBox();
            lblStatus = new Label();
            splitContainer1 = new SplitContainer();
            pnlMap = new Panel();
            splitContainer2 = new SplitContainer();
            groupBox4 = new GroupBox();
            lstArmies = new ListBox();
            groupBox3 = new GroupBox();
            pnlClans = new Panel();
            groupBox2 = new GroupBox();
            rtbDebug = new RichTextBox();
            panel2 = new Panel();
            groupBox1 = new GroupBox();
            lstEvents = new ListBox();
            menuStrip.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox2.SuspendLayout();
            panel2.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, menuSim });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1366, 24);
            menuStrip.TabIndex = 0;
            // 
            // menuFile
            // 
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuNew, menuSave, menuSaveAs, menuLoad, menuSep, menuExit });
            menuFile.Name = "menuFile";
            menuFile.Size = new Size(67, 20);
            menuFile.Text = "ファイル(&F)";
            // 
            // menuNew
            // 
            menuNew.Name = "menuNew";
            menuNew.ShortcutKeys = Keys.Control | Keys.N;
            menuNew.Size = new Size(186, 22);
            menuNew.Text = "新規ゲーム(&N)";
            menuNew.Click += menuNew_Click;
            // 
            // menuSave
            // 
            menuSave.Name = "menuSave";
            menuSave.ShortcutKeys = Keys.Control | Keys.S;
            menuSave.Size = new Size(186, 22);
            menuSave.Text = "保存(&S)";
            menuSave.Click += menuSave_Click;
            // 
            // menuSaveAs
            // 
            menuSaveAs.Name = "menuSaveAs";
            menuSaveAs.Size = new Size(186, 22);
            menuSaveAs.Text = "名前を付けて保存(&A)...";
            menuSaveAs.Click += menuSaveAs_Click;
            // 
            // menuLoad
            // 
            menuLoad.Name = "menuLoad";
            menuLoad.ShortcutKeys = Keys.Control | Keys.O;
            menuLoad.Size = new Size(186, 22);
            menuLoad.Text = "読込(&O)...";
            menuLoad.Click += menuLoad_Click;
            // 
            // menuSep
            // 
            menuSep.Name = "menuSep";
            menuSep.Size = new Size(183, 6);
            // 
            // menuExit
            // 
            menuExit.Name = "menuExit";
            menuExit.Size = new Size(186, 22);
            menuExit.Text = "終了(&X)";
            menuExit.Click += menuExit_Click;
            // 
            // menuSim
            // 
            menuSim.DropDownItems.AddRange(new ToolStripItem[] { menuSimRun });
            menuSim.Name = "menuSim";
            menuSim.Size = new Size(101, 20);
            menuSim.Text = "シミュレーション(&S)";
            // 
            // menuSimRun
            // 
            menuSimRun.Name = "menuSimRun";
            menuSimRun.ShortcutKeys = Keys.Control | Keys.R;
            menuSimRun.Size = new Size(186, 22);
            menuSimRun.Text = "自動実行(&R)...";
            menuSimRun.Click += menuSimRun_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnStep);
            panel1.Controls.Add(btnAuto);
            panel1.Controls.Add(btnStop);
            panel1.Controls.Add(btnRestart);
            panel1.Controls.Add(btnConfirm);
            panel1.Controls.Add(lblSpeed);
            panel1.Controls.Add(cmbSpeed);
            panel1.Controls.Add(lblStatus);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 24);
            panel1.Name = "panel1";
            panel1.Size = new Size(1366, 50);
            panel1.TabIndex = 17;
            // 
            // btnStep
            // 
            btnStep.Location = new Point(9, 9);
            btnStep.Name = "btnStep";
            btnStep.Size = new Size(100, 32);
            btnStep.TabIndex = 8;
            btnStep.Text = "1ターン進める";
            btnStep.Click += btnStep_Click;
            // 
            // btnAuto
            // 
            btnAuto.Location = new Point(117, 9);
            btnAuto.Name = "btnAuto";
            btnAuto.Size = new Size(80, 32);
            btnAuto.TabIndex = 9;
            btnAuto.Text = "▶ オート";
            btnAuto.Click += btnAuto_Click;
            // 
            // btnStop
            // 
            btnStop.Enabled = false;
            btnStop.Location = new Point(205, 9);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(60, 32);
            btnStop.TabIndex = 10;
            btnStop.Text = "■ 停止";
            btnStop.Click += btnStop_Click;
            // 
            // btnRestart
            // 
            btnRestart.Enabled = false;
            btnRestart.Location = new Point(273, 9);
            btnRestart.Name = "btnRestart";
            btnRestart.Size = new Size(90, 32);
            btnRestart.TabIndex = 11;
            btnRestart.Text = "↺ もう一度";
            btnRestart.Click += btnRestart_Click;
            // 
            // btnConfirm
            // 
            btnConfirm.Enabled = false;
            btnConfirm.Location = new Point(371, 9);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.Size = new Size(90, 32);
            btnConfirm.TabIndex = 15;
            btnConfirm.Text = "✔ 命令確定";
            btnConfirm.BackColor = Color.FromArgb(40, 80, 40);
            btnConfirm.ForeColor = Color.White;
            btnConfirm.Click += btnConfirm_Click;
            // 
            // lblSpeed
            // 
            lblSpeed.Location = new Point(571, 15);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(36, 20);
            lblSpeed.TabIndex = 12;
            lblSpeed.Text = "速度:";
            // 
            // cmbSpeed
            // 
            cmbSpeed.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSpeed.Items.AddRange(new object[] { "遅い(2s)", "普通(1s)", "速い(0.5s)" });
            cmbSpeed.Location = new Point(477, 11);
            cmbSpeed.Name = "cmbSpeed";
            cmbSpeed.Size = new Size(80, 23);
            cmbSpeed.TabIndex = 13;
            // 
            // lblStatus
            // 
            lblStatus.Location = new Point(642, 15);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(460, 20);
            lblStatus.TabIndex = 14;
            lblStatus.Text = "Tick: 0  Year: 1560  春";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 74);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(pnlMap);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Panel2.Controls.Add(panel2);
            splitContainer1.Size = new Size(1366, 747);
            splitContainer1.SplitterDistance = 455;
            splitContainer1.TabIndex = 18;
            // 
            // pnlMap
            // 
            pnlMap.BackColor = Color.FromArgb(30, 30, 50);
            pnlMap.BorderStyle = BorderStyle.FixedSingle;
            pnlMap.Dock = DockStyle.Fill;
            pnlMap.Location = new Point(0, 0);
            pnlMap.Name = "pnlMap";
            pnlMap.Size = new Size(455, 747);
            pnlMap.TabIndex = 18;
            pnlMap.Paint += pnlMap_Paint;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(groupBox4);
            splitContainer2.Panel1.Controls.Add(groupBox3);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(groupBox2);
            splitContainer2.Size = new Size(907, 546);
            splitContainer2.SplitterDistance = 302;
            splitContainer2.TabIndex = 27;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(lstArmies);
            groupBox4.Dock = DockStyle.Fill;
            groupBox4.Location = new Point(0, 227);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(302, 319);
            groupBox4.TabIndex = 27;
            groupBox4.TabStop = false;
            groupBox4.Text = "軍隊";
            // 
            // lstArmies
            // 
            lstArmies.Dock = DockStyle.Fill;
            lstArmies.Font = new Font("ＭＳ ゴシック", 9F);
            lstArmies.Location = new Point(3, 19);
            lstArmies.Name = "lstArmies";
            lstArmies.Size = new Size(296, 297);
            lstArmies.TabIndex = 26;
            lstArmies.SelectedIndexChanged += lstArmies_SelectedIndexChanged;
            lstArmies.DoubleClick += lstArmies_DoubleClick;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(pnlClans);
            groupBox3.Dock = DockStyle.Top;
            groupBox3.Location = new Point(0, 0);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(302, 227);
            groupBox3.TabIndex = 26;
            groupBox3.TabStop = false;
            groupBox3.Text = "勢力概要";
            // 
            // pnlClans
            // 
            pnlClans.BackColor = Color.FromArgb(20, 20, 40);
            pnlClans.BorderStyle = BorderStyle.FixedSingle;
            pnlClans.Dock = DockStyle.Fill;
            pnlClans.Font = new Font("ＭＳ ゴシック", 9F);
            pnlClans.ForeColor = Color.White;
            pnlClans.Location = new Point(3, 19);
            pnlClans.Name = "pnlClans";
            pnlClans.Size = new Size(296, 205);
            pnlClans.TabIndex = 24;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(rtbDebug);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Location = new Point(0, 0);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(601, 546);
            groupBox2.TabIndex = 28;
            groupBox2.TabStop = false;
            groupBox2.Text = "BattleCore Debug Console";
            // 
            // rtbDebug
            // 
            rtbDebug.BackColor = Color.FromArgb(15, 15, 30);
            rtbDebug.BorderStyle = BorderStyle.FixedSingle;
            rtbDebug.Dock = DockStyle.Fill;
            rtbDebug.Font = new Font("ＭＳ ゴシック", 9F);
            rtbDebug.ForeColor = Color.White;
            rtbDebug.Location = new Point(3, 19);
            rtbDebug.Name = "rtbDebug";
            rtbDebug.ReadOnly = true;
            rtbDebug.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbDebug.Size = new Size(595, 524);
            rtbDebug.TabIndex = 28;
            rtbDebug.Text = "";
            // 
            // panel2
            // 
            panel2.Controls.Add(groupBox1);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 546);
            panel2.Name = "panel2";
            panel2.Size = new Size(907, 201);
            panel2.TabIndex = 26;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lstEvents);
            groupBox1.Dock = DockStyle.Bottom;
            groupBox1.Location = new Point(0, 1);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(907, 200);
            groupBox1.TabIndex = 26;
            groupBox1.TabStop = false;
            groupBox1.Text = "イベントログ";
            // 
            // lstEvents
            // 
            lstEvents.Dock = DockStyle.Fill;
            lstEvents.DrawMode = DrawMode.OwnerDrawFixed;
            lstEvents.Font = new Font("ＭＳ ゴシック", 9F);
            lstEvents.Location = new Point(3, 19);
            lstEvents.Name = "lstEvents";
            lstEvents.Size = new Size(901, 178);
            lstEvents.TabIndex = 26;
            lstEvents.DrawItem += lstEvents_DrawItem;
            // 
            // frMain
            // 
            ClientSize = new Size(1366, 821);
            Controls.Add(splitContainer1);
            Controls.Add(panel1);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            Name = "frMain";
            Text = "BattleCoreStudio";
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            panel1.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            panel2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private MenuStrip          menuStrip;
        private ToolStripMenuItem  menuFile;
        private ToolStripMenuItem  menuNew;
        private ToolStripMenuItem  menuSave;
        private ToolStripMenuItem  menuSaveAs;
        private ToolStripMenuItem  menuLoad;
        private ToolStripSeparator menuSep;
        private ToolStripMenuItem  menuExit;
        private ToolStripMenuItem  menuSim;
        private ToolStripMenuItem  menuSimRun;
        private Panel              panel1;
        private Button             btnStep;
        private Button             btnAuto;
        private Button             btnStop;
        private Button             btnRestart;
        private Button             btnConfirm;
        private Label              lblSpeed;
        private ComboBox           cmbSpeed;
        private Label              lblStatus;
        private SplitContainer     splitContainer1;
        private Panel              pnlMap;
        private Panel              panel2;
        private GroupBox           groupBox1;
        private SplitContainer     splitContainer2;
        private ListBox            lstEvents;
        private GroupBox           groupBox4;
        private ListBox            lstArmies;
        private GroupBox           groupBox3;
        private Panel              pnlClans;
        private GroupBox           groupBox2;
        private RichTextBox        rtbDebug;
    }
}
