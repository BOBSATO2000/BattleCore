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
            menuStrip     = new MenuStrip();
            menuFile      = new ToolStripMenuItem();
            menuNew       = new ToolStripMenuItem();
            menuSave      = new ToolStripMenuItem();
            menuSaveAs    = new ToolStripMenuItem();
            menuLoad      = new ToolStripMenuItem();
            menuSep       = new ToolStripSeparator();
            menuExit      = new ToolStripMenuItem();
            btnStep       = new Button();
            btnAuto       = new Button();
            btnStop       = new Button();
            btnRestart    = new Button();
            cmbSpeed      = new ComboBox();
            lblSpeed      = new Label();
            lblStatus     = new Label();
            pnlMap        = new Panel();
            pnlClans      = new Panel();
            lblClans      = new Label();
            lstArmies     = new ListBox();
            lstEvents     = new ListBox();
            lblArmies     = new Label();
            lblEvents     = new Label();

            SuspendLayout();

            // menuStrip
            menuStrip.Items.AddRange(new ToolStripItem[] { menuFile });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Size     = new Size(796, 24);

            // menuFile
            menuFile.Text = "ファイル(&F)";
            menuFile.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuNew, menuSave, menuSaveAs, menuLoad, menuSep, menuExit
            });

            // menuNew
            menuNew.Text         = "新規ゲーム(&N)";
            menuNew.ShortcutKeys = Keys.Control | Keys.N;
            menuNew.Click       += menuNew_Click;

            // menuSave
            menuSave.Text         = "保存(&S)";
            menuSave.ShortcutKeys = Keys.Control | Keys.S;
            menuSave.Click       += menuSave_Click;

            // menuSaveAs
            menuSaveAs.Text  = "名前を付けて保存(&A)...";
            menuSaveAs.Click += menuSaveAs_Click;

            // menuLoad
            menuLoad.Text         = "読込(&O)...";
            menuLoad.ShortcutKeys = Keys.Control | Keys.O;
            menuLoad.Click       += menuLoad_Click;

            // menuExit
            menuExit.Text  = "終了(&X)";
            menuExit.Click += menuExit_Click;

            // btnStep
            btnStep.Location = new Point(12, 36);
            btnStep.Size     = new Size(100, 32);
            btnStep.Text     = "1ターン進める";
            btnStep.Click   += btnStep_Click;

            // btnAuto
            btnAuto.Location = new Point(120, 36);
            btnAuto.Size     = new Size(80, 32);
            btnAuto.Text     = "▶ オート";
            btnAuto.Click   += btnAuto_Click;

            // btnStop
            btnStop.Location = new Point(208, 36);
            btnStop.Size     = new Size(60, 32);
            btnStop.Text     = "■ 停止";
            btnStop.Enabled  = false;
            btnStop.Click   += btnStop_Click;

            // btnRestart
            btnRestart.Location = new Point(276, 36);
            btnRestart.Size     = new Size(90, 32);
            btnRestart.Text     = "↺ もう一度";
            btnRestart.Enabled  = false;
            btnRestart.Click   += btnRestart_Click;

            // cmbSpeed
            cmbSpeed.Location      = new Point(318, 38);
            cmbSpeed.Size          = new Size(80, 24);
            cmbSpeed.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSpeed.Items.AddRange(new object[] { "遅い(2s)", "普通(1s)", "速い(0.5s)" });
            cmbSpeed.SelectedIndex = 1;

            // lblSpeed
            lblSpeed.Location = new Point(378, 42);
            lblSpeed.Size     = new Size(36, 20);
            lblSpeed.Text     = "速度:";

            // lblStatus
            lblStatus.Location = new Point(410, 42);
            lblStatus.Size     = new Size(360, 20);
            lblStatus.Text     = "Tick: 0  Year: 1560  春";

            // pnlMap
            pnlMap.Location    = new Point(12, 80);
            pnlMap.Size        = new Size(560, 500);
            pnlMap.BorderStyle = BorderStyle.FixedSingle;
            pnlMap.BackColor   = Color.FromArgb(30, 30, 50);
            pnlMap.Paint      += pnlMap_Paint;

            // lblClans
            lblClans.Location = new Point(584, 80);
            lblClans.Size     = new Size(200, 20);
            lblClans.Text     = "勢力概要";

            // pnlClans
            pnlClans.Location    = new Point(584, 102);
            pnlClans.Size        = new Size(200, 100);
            pnlClans.BorderStyle = BorderStyle.FixedSingle;
            pnlClans.BackColor   = Color.FromArgb(20, 20, 40);
            pnlClans.ForeColor   = Color.White;
            pnlClans.Font        = new Font("MS Gothic", 9f);

            // lblArmies
            lblArmies.Location = new Point(584, 214);
            lblArmies.Size     = new Size(200, 20);
            lblArmies.Text     = "軍隊";

            // lstArmies
            lstArmies.Location              = new Point(584, 236);
            lstArmies.Size                  = new Size(200, 160);
            lstArmies.Font                  = new Font("MS Gothic", 9f);
            lstArmies.SelectedIndexChanged += lstArmies_SelectedIndexChanged;
            lstArmies.DoubleClick          += lstArmies_DoubleClick;

            // lblEvents
            lblEvents.Location = new Point(584, 408);
            lblEvents.Size     = new Size(200, 20);
            lblEvents.Text     = "イベントログ";

            // lstEvents
            lstEvents.Location = new Point(584, 430);
            lstEvents.Size     = new Size(200, 150);
            lstEvents.Font     = new Font("MS Gothic", 9f);

            // frMain
            ClientSize  = new Size(796, 592);
            MainMenuStrip = menuStrip;
            Text        = "BattleCoreStudio";
            Controls.AddRange(new Control[]
            {
                menuStrip,
                btnStep, btnAuto, btnStop, btnRestart, lblSpeed, cmbSpeed,
                lblStatus,
                pnlMap,
                lblClans, pnlClans,
                lblArmies, lstArmies,
                lblEvents, lstEvents,
            });

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
        private Button   btnStep;
        private Button   btnAuto;
        private Button   btnStop;
        private Button   btnRestart;
        private ComboBox cmbSpeed;
        private Label    lblSpeed;
        private Label    lblStatus;
        private Panel    pnlMap;
        private Panel    pnlClans;
        private Label    lblClans;
        private ListBox  lstArmies;
        private ListBox  lstEvents;
        private Label    lblArmies;
        private Label    lblEvents;
    }
}
