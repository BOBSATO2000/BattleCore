using BattleCore.Simulation;
using BattleCore.World;

namespace BattleCoreStudio
{
    /// <summary>
    /// 自動AIシミュレーションダイアログ。
    /// SimulationRunner を使って指定ターン数を自動実行し、統計を表示する。
    /// </summary>
    internal sealed class frSimRunner : Form
    {
        private readonly SimulationEngine _engine;
        private readonly WorldState       _world;
        private SimulationRunner?         _runner;

        private NumericUpDown nudTurns   = null!;
        private ComboBox      cmbSpeed   = null!;
        private Button        btnRun     = null!;
        private Button        btnStop    = null!;
        private Button        btnClose   = null!;
        private ProgressBar   pbProgress = null!;
        private RichTextBox   rtbResult  = null!;
        private Label         lblTurn    = null!;

        public frSimRunner(SimulationEngine engine, WorldState world)
        {
            _engine = engine;
            _world  = world;
            InitUI();
        }

        private void InitUI()
        {
            Text          = "Auto Simulation";
            ClientSize    = new Size(420, 480);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox   = false;
            BackColor     = Color.FromArgb(20, 20, 40);
            ForeColor     = Color.White;

            var lblT = new Label { Text = "実行ターン数:", Location = new Point(12, 16), Size = new Size(100, 20) };
            nudTurns = new NumericUpDown
            {
                Location = new Point(120, 14), Size = new Size(80, 24),
                Minimum = 1, Maximum = 10000, Value = 100,
                BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White,
            };

            var lblS = new Label { Text = "速度:", Location = new Point(220, 16), Size = new Size(40, 20) };
            cmbSpeed = new ComboBox
            {
                Location = new Point(264, 14), Size = new Size(100, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White,
            };
            cmbSpeed.Items.AddRange(["×1 (1s)", "×2 (0.5s)", "×5 (0.2s)", "×10 (0.1s)", "MAX"]);
            cmbSpeed.SelectedIndex = 4; // デフォルトMAX

            btnRun = new Button
            {
                Text = "▶ 実行", Location = new Point(12, 48), Size = new Size(90, 30),
                BackColor = Color.FromArgb(40, 100, 40), ForeColor = Color.White,
            };
            btnStop = new Button
            {
                Text = "■ 停止", Location = new Point(110, 48), Size = new Size(90, 30),
                Enabled = false,
                BackColor = Color.FromArgb(80, 40, 40), ForeColor = Color.White,
            };
            btnClose = new Button
            {
                Text = "閉じる", Location = new Point(316, 48), Size = new Size(90, 30),
                BackColor = Color.FromArgb(40, 40, 80), ForeColor = Color.White,
            };

            lblTurn = new Label
            {
                Text = "Turn: 0", Location = new Point(12, 88),
                Size = new Size(390, 20), ForeColor = Color.FromArgb(180, 180, 255),
            };

            pbProgress = new ProgressBar
            {
                Location = new Point(12, 110), Size = new Size(390, 16),
                Minimum = 0, Maximum = 100, Value = 0,
            };

            var lblR = new Label { Text = "結果:", Location = new Point(12, 136), Size = new Size(60, 20) };
            rtbResult = new RichTextBox
            {
                Location = new Point(12, 158), Size = new Size(390, 300),
                BackColor = Color.FromArgb(15, 15, 30), ForeColor = Color.White,
                Font = new Font("MS Gothic", 9f), ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
            };

            Controls.AddRange([lblT, nudTurns, lblS, cmbSpeed,
                btnRun, btnStop, btnClose, lblTurn, pbProgress, lblR, rtbResult]);

            btnRun.Click   += BtnRun_Click;
            btnStop.Click  += BtnStop_Click;
            btnClose.Click += (_, _) => Close();
        }

        private void BtnRun_Click(object? sender, EventArgs e)
        {
            int maxTurns  = (int)nudTurns.Value;
            int intervalMs = cmbSpeed.SelectedIndex switch
            {
                0 => 1000,
                1 => 500,
                2 => 200,
                3 => 100,
                _ => 0,   // MAX
            };

            pbProgress.Maximum = maxTurns;
            pbProgress.Value   = 0;
            rtbResult.Clear();
            btnRun.Enabled  = false;
            btnStop.Enabled = true;

            _runner = new SimulationRunner(_engine) { IntervalMs = intervalMs };

            _runner.TurnCompleted += stats => Invoke(() =>
            {
                lblTurn.Text      = $"Turn: {stats.TurnsExecuted} / {maxTurns}";
                pbProgress.Value  = Math.Min(stats.TurnsExecuted, maxTurns);
            });

            _runner.RunCompleted += stats => Invoke(() =>
            {
                btnRun.Enabled  = true;
                btnStop.Enabled = false;
                ShowStats(stats, maxTurns);
            });

            _ = _runner.RunAsync(maxTurns);
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            _runner?.Stop();
            btnRun.Enabled  = true;
            btnStop.Enabled = false;
        }

        private void ShowStats(SimulationStats stats, int maxTurns)
        {
            rtbResult.Clear();

            Append("=== Simulation Result ===\n", Color.FromArgb(180, 180, 255));
            Append($"実行ターン : {stats.TurnsExecuted} / {maxTurns}\n");
            Append($"総イベント : {stats.TotalEvents}\n");
            Append("\n");

            // 勝利結果
            if (stats.WinnerClanId.HasValue)
            {
                var winner = _world.Clans.FirstOrDefault(c => c.Id == stats.WinnerClanId.Value);
                Append($"勝利勢力  : {winner?.Name ?? "?"}\n", Color.FromArgb(255, 220, 60));
                Append($"理由      : {stats.WinReason}\n", Color.FromArgb(200, 200, 100));
            }
            else if (!string.IsNullOrEmpty(stats.WinReason))
            {
                Append($"終了理由  : {stats.WinReason}\n", Color.FromArgb(200, 200, 100));
            }
            else
            {
                Append($"結果      : ターン上限到達\n", Color.Gray);
            }
            Append("\n");

            // 勢力別兵力
            Append("--- 勢力別現況 ---\n", Color.FromArgb(100, 200, 255));
            foreach (var clan in _world.Clans)
            {
                var total   = _world.Armies.Where(a => a.ClanId == clan.Id).Sum(a => a.Soldiers);
                var castles = _world.Castles.Count(c => c.OwnerClanId == clan.Id);
                Append($"  {clan.Name,-8} 兵:{total,6:#,0}  城:{castles}\n");
            }
            Append("\n");

            // 統計
            Append("--- Statistics ---\n", Color.FromArgb(100, 200, 255));
            Append($"  戦闘回数   : {stats.BattleCount}\n");
            Append($"  補給回数   : {stats.SupplyCount}\n");
            Append($"  離反回数   : {stats.BetrayalCount}\n",
                stats.BetrayalCount > 0 ? Color.FromArgb(255, 160, 40) : Color.White);
            Append($"  命令拒否   : {stats.RefusalCount}\n",
                stats.RefusalCount > 0 ? Color.FromArgb(255, 220, 60) : Color.White);
            Append($"  独断行動   : {stats.IndependentCount}\n",
                stats.IndependentCount > 0 ? Color.FromArgb(200, 160, 255) : Color.White);
        }

        private void Append(string text, Color? color = null)
        {
            rtbResult.SelectionStart  = rtbResult.TextLength;
            rtbResult.SelectionLength = 0;
            rtbResult.SelectionColor  = color ?? Color.White;
            rtbResult.AppendText(text);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _runner?.Stop();
            base.OnFormClosing(e);
        }
    }
}
