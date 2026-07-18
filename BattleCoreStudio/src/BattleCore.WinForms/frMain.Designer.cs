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
            btnStep       = new Button();
            btnAuto       = new Button();
            btnStop       = new Button();
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

            // btnStep
            btnStep.Location = new Point(12, 12);
            btnStep.Size     = new Size(100, 32);
            btnStep.Text     = "1ターン進める";
            btnStep.Click   += btnStep_Click;

            // btnAuto
            btnAuto.Location = new Point(120, 12);
            btnAuto.Size     = new Size(80, 32);
            btnAuto.Text     = "▶ オート";
            btnAuto.Click   += btnAuto_Click;

            // btnStop
            btnStop.Location = new Point(208, 12);
            btnStop.Size     = new Size(60, 32);
            btnStop.Text     = "■ 停止";
            btnStop.Enabled  = false;
            btnStop.Click   += btnStop_Click;

            // lblSpeed
            lblSpeed.Location = new Point(280, 18);
            lblSpeed.Size     = new Size(36, 20);
            lblSpeed.Text     = "速度:";

            // cmbSpeed
            cmbSpeed.Location     = new Point(318, 14);
            cmbSpeed.Size         = new Size(80, 24);
            cmbSpeed.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSpeed.Items.AddRange(new object[] { "遅い(2s)", "普通(1s)", "速い(0.5s)" });
            cmbSpeed.SelectedIndex = 1;

            // lblStatus
            lblStatus.Location  = new Point(410, 18);
            lblStatus.Size      = new Size(360, 20);
            lblStatus.Text      = "Tick: 0  Year: 1560  春";

            // pnlMap
            pnlMap.Location    = new Point(12, 56);
            pnlMap.Size        = new Size(560, 500);
            pnlMap.BorderStyle = BorderStyle.FixedSingle;
            pnlMap.BackColor   = Color.FromArgb(30, 30, 50);
            pnlMap.Paint      += pnlMap_Paint;

            // lblClans
            lblClans.Location  = new Point(584, 56);
            lblClans.Size      = new Size(200, 20);
            lblClans.Text      = "勢力概要";

            // pnlClans
            pnlClans.Location    = new Point(584, 78);
            pnlClans.Size        = new Size(200, 100);
            pnlClans.BorderStyle = BorderStyle.FixedSingle;
            pnlClans.BackColor   = Color.FromArgb(20, 20, 40);
            pnlClans.ForeColor   = Color.White;
            pnlClans.Font        = new Font("MS Gothic", 9f);

            // lblArmies
            lblArmies.Location = new Point(584, 190);
            lblArmies.Size     = new Size(200, 20);
            lblArmies.Text     = "軍隊";

            // lstArmies
            lstArmies.Location         = new Point(584, 212);
            lstArmies.Size             = new Size(200, 160);
            lstArmies.Font             = new Font("MS Gothic", 9f);
            lstArmies.SelectedIndexChanged += lstArmies_SelectedIndexChanged;

            // lblEvents
            lblEvents.Location = new Point(584, 384);
            lblEvents.Size     = new Size(200, 20);
            lblEvents.Text     = "イベントログ";

            // lstEvents
            lstEvents.Location  = new Point(584, 406);
            lstEvents.Size      = new Size(200, 150);
            lstEvents.Font      = new Font("MS Gothic", 9f);

            // frMain
            ClientSize = new Size(796, 568);
            Text       = "BattleCoreStudio";
            Controls.AddRange(new Control[]
            {
                btnStep, btnAuto, btnStop, lblSpeed, cmbSpeed,
                lblStatus,
                pnlMap,
                lblClans, pnlClans,
                lblArmies, lstArmies,
                lblEvents, lstEvents
            });

            ResumeLayout(false);
        }

        private Button   btnStep;
        private Button   btnAuto;
        private Button   btnStop;
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
