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
            lblStatus     = new Label();
            pnlMap        = new Panel();
            lstArmies     = new ListBox();
            lstEvents     = new ListBox();
            lblArmies     = new Label();
            lblEvents     = new Label();

            SuspendLayout();

            // btnStep
            btnStep.Location = new Point(12, 12);
            btnStep.Size     = new Size(120, 32);
            btnStep.Text     = "1ターン進める";
            btnStep.Click   += btnStep_Click;

            // lblStatus
            lblStatus.Location  = new Point(144, 18);
            lblStatus.Size      = new Size(400, 20);
            lblStatus.Text      = "Tick: 0  Year: 1560  春";

            // pnlMap
            pnlMap.Location    = new Point(12, 56);
            pnlMap.Size        = new Size(560, 500);
            pnlMap.BorderStyle = BorderStyle.FixedSingle;
            pnlMap.BackColor   = Color.FromArgb(30, 30, 50);
            pnlMap.Paint      += pnlMap_Paint;

            // lblArmies
            lblArmies.Location = new Point(584, 56);
            lblArmies.Size     = new Size(200, 20);
            lblArmies.Text     = "軍隊";

            // lstArmies
            lstArmies.Location  = new Point(584, 78);
            lstArmies.Size      = new Size(200, 220);
            lstArmies.Font      = new Font("MS Gothic", 9f);

            // lblEvents
            lblEvents.Location = new Point(584, 310);
            lblEvents.Size     = new Size(200, 20);
            lblEvents.Text     = "イベントログ";

            // lstEvents
            lstEvents.Location  = new Point(584, 332);
            lstEvents.Size      = new Size(200, 224);
            lstEvents.Font      = new Font("MS Gothic", 9f);

            // frMain
            ClientSize = new Size(796, 568);
            Text       = "BattleCoreStudio";
            Controls.AddRange(new Control[]
            {
                btnStep, lblStatus,
                pnlMap,
                lblArmies, lstArmies,
                lblEvents, lstEvents
            });

            ResumeLayout(false);
        }

        private Button  btnStep;
        private Label   lblStatus;
        private Panel   pnlMap;
        private ListBox lstArmies;
        private ListBox lstEvents;
        private Label   lblArmies;
        private Label   lblEvents;
    }
}
