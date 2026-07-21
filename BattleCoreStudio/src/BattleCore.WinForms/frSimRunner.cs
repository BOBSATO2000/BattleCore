using BattleCore.AI;
using BattleCore.Scenario;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.Systems.Battle;
using BattleCore.World;

namespace BattleCoreStudio
{
    /// <summary>
    /// 自動AIシミュレーションダイアログ。
    /// シングル実行（1回）とバッチ実行（N回）の2モードを持つ。
    /// </summary>
    internal sealed class frSimRunner : Form
    {
        private readonly SimulationEngine _engine;
        private readonly WorldState       _world;
        private readonly string           _scenarioPath;
        private SimulationRunner?         _runner;
        private BattleRunSummary?         _lastSummary;

        // シングル実行UI
        private TabControl   tabMain    = null!;
        private TabPage      tabSingle  = null!;
        private TabPage      tabBatch   = null!;

        // シングル
        private NumericUpDown nudTurns   = null!;
        private ComboBox      cmbSpeed   = null!;
        private Button        btnRun     = null!;
        private Button        btnStop    = null!;
        private ProgressBar   pbProgress = null!;
        private Label         lblTurn    = null!;
        private RichTextBox   rtbResult  = null!;

        // バッチ
        private NumericUpDown nudBatchRuns   = null!;
        private NumericUpDown nudBatchTurns  = null!;
        private NumericUpDown nudSeed        = null!;
        private CheckBox      chkUseSeed     = null!;
        private Button        btnBatchRun    = null!;
        private Button        btnBatchStop   = null!;
        private Button        btnExportCsv   = null!;
        private ProgressBar   pbBatch        = null!;
        private Label         lblBatchStatus = null!;
        private RichTextBox   rtbBatch       = null!;

        private Button btnClose = null!;

        public frSimRunner(SimulationEngine engine, WorldState world, string scenarioPath)
        {
            _engine       = engine;
            _world        = world;
            _scenarioPath = scenarioPath;
            InitUI();
        }

        private void InitUI()
        {
            Text            = "Simulation Runner";
            ClientSize      = new Size(480, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = Color.FromArgb(20, 20, 40);
            ForeColor       = Color.White;

            tabMain   = new TabControl { Location = new Point(8, 8), Size = new Size(460, 500) };
            tabSingle = new TabPage("シングル実行");
            tabBatch  = new TabPage("バッチ実行");
            tabMain.TabPages.AddRange([tabSingle, tabBatch]);

            BuildSingleTab();
            BuildBatchTab();

            btnClose = new Button
            {
                Text = "閉じる", Location = new Point(376, 520), Size = new Size(90, 28),
                BackColor = Color.FromArgb(40, 40, 80), ForeColor = Color.White,
            };
            btnClose.Click += (_, _) => Close();

            Controls.AddRange([tabMain, btnClose]);
        }

        // ── シングル実行タブ ──────────────────────────────────────
        private void BuildSingleTab()
        {
            var p = tabSingle;
            p.BackColor = Color.FromArgb(20, 20, 40);
            p.ForeColor = Color.White;

            Add(p, new Label { Text = "ターン数:", Location = new Point(8, 12), Size = new Size(80, 20) });
            nudTurns = new NumericUpDown
            {
                Location = new Point(92, 10), Size = new Size(80, 24),
                Minimum = 1, Maximum = 10000, Value = 100,
                BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White,
            };
            Add(p, nudTurns);

            Add(p, new Label { Text = "速度:", Location = new Point(184, 12), Size = new Size(40, 20) });
            cmbSpeed = new ComboBox
            {
                Location = new Point(228, 10), Size = new Size(100, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White,
            };
            cmbSpeed.Items.AddRange(["×1 (1s)", "×2 (0.5s)", "×5 (0.2s)", "×10 (0.1s)", "MAX"]);
            cmbSpeed.SelectedIndex = 4;
            Add(p, cmbSpeed);

            btnRun = Btn("▶ 実行", new Point(8, 42), Color.FromArgb(40, 100, 40));
            btnRun.Click += BtnRun_Click;
            btnStop = Btn("■ 停止", new Point(106, 42), Color.FromArgb(80, 40, 40));
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;
            Add(p, btnRun); Add(p, btnStop);

            lblTurn = new Label { Text = "Turn: 0", Location = new Point(8, 80), Size = new Size(430, 20), ForeColor = Color.FromArgb(180, 180, 255) };
            Add(p, lblTurn);

            pbProgress = new ProgressBar { Location = new Point(8, 102), Size = new Size(430, 14) };
            Add(p, pbProgress);

            Add(p, new Label { Text = "結果:", Location = new Point(8, 122), Size = new Size(60, 20) });
            rtbResult = Rtb(new Point(8, 144), new Size(430, 300));
            Add(p, rtbResult);
        }

        // ── バッチ実行タブ ──────────────────────────────────────
        private void BuildBatchTab()
        {
            var p = tabBatch;
            p.BackColor = Color.FromArgb(20, 20, 40);
            p.ForeColor = Color.White;

            Add(p, new Label { Text = "実行回数:", Location = new Point(8, 12), Size = new Size(72, 20) });
            nudBatchRuns = new NumericUpDown
            {
                Location = new Point(84, 10), Size = new Size(64, 24),
                Minimum = 1, Maximum = 1000, Value = 10,
                BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White,
            };
            Add(p, nudBatchRuns);

            Add(p, new Label { Text = "最大ターン:", Location = new Point(160, 12), Size = new Size(80, 20) });
            nudBatchTurns = new NumericUpDown
            {
                Location = new Point(244, 10), Size = new Size(72, 24),
                Minimum = 10, Maximum = 10000, Value = 200,
                BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White,
            };
            Add(p, nudBatchTurns);

            chkUseSeed = new CheckBox { Text = "シード固定", Location = new Point(8, 42), Size = new Size(90, 20), ForeColor = Color.White };
            Add(p, chkUseSeed);
            nudSeed = new NumericUpDown
            {
                Location = new Point(104, 40), Size = new Size(80, 24),
                Minimum = 0, Maximum = 999999, Value = 42,
                BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White, Enabled = false,
            };
            chkUseSeed.CheckedChanged += (_, _) => nudSeed.Enabled = chkUseSeed.Checked;
            Add(p, nudSeed);

            btnBatchRun  = Btn("▶ バッチ実行", new Point(8, 70), Color.FromArgb(40, 100, 40));
            btnBatchRun.Click += BtnBatchRun_Click;
            btnBatchStop = Btn("■ 停止", new Point(116, 70), Color.FromArgb(80, 40, 40));
            btnBatchStop.Enabled = false;
            btnBatchStop.Click += BtnBatchStop_Click;
            btnExportCsv = Btn("CSV出力", new Point(224, 70), Color.FromArgb(40, 60, 100));
            btnExportCsv.Enabled = false;
            btnExportCsv.Click += BtnExportCsv_Click;
            Add(p, btnBatchRun); Add(p, btnBatchStop); Add(p, btnExportCsv);

            lblBatchStatus = new Label { Text = "待機中", Location = new Point(8, 106), Size = new Size(430, 20), ForeColor = Color.FromArgb(180, 180, 255) };
            Add(p, lblBatchStatus);

            pbBatch = new ProgressBar { Location = new Point(8, 128), Size = new Size(430, 14) };
            Add(p, pbBatch);

            Add(p, new Label { Text = "集計結果:", Location = new Point(8, 148), Size = new Size(80, 20) });
            rtbBatch = Rtb(new Point(8, 170), new Size(430, 270));
            Add(p, rtbBatch);
        }

        // ── シングル実行 ──────────────────────────────────────────
        private void BtnRun_Click(object? sender, EventArgs e)
        {
            int maxTurns   = (int)nudTurns.Value;
            int intervalMs = cmbSpeed.SelectedIndex switch { 0=>1000, 1=>500, 2=>200, 3=>100, _=>0 };

            pbProgress.Maximum = maxTurns; pbProgress.Value = 0;
            rtbResult.Clear();
            btnRun.Enabled = false; btnStop.Enabled = true;

            _runner = new SimulationRunner(_engine) { IntervalMs = intervalMs };
            _runner.TurnCompleted += s => Invoke(() =>
            {
                lblTurn.Text     = $"Turn: {s.TurnsExecuted} / {maxTurns}";
                pbProgress.Value = Math.Min(s.TurnsExecuted, maxTurns);
            });
            _runner.RunCompleted += s => Invoke(() =>
            {
                btnRun.Enabled = true; btnStop.Enabled = false;
                ShowSingleStats(s, maxTurns);
            });
            _ = _runner.RunAsync(maxTurns);
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            _runner?.Stop();
            btnRun.Enabled = true; btnStop.Enabled = false;
        }

        private void ShowSingleStats(SimulationStats s, int maxTurns)
        {
            rtbResult.Clear();
            AppendTo(rtbResult, "=== Result ===\n", Color.FromArgb(180, 180, 255));
            AppendTo(rtbResult, $"実行ターン : {s.TurnsExecuted} / {maxTurns}\n");
            if (s.WinnerClanId.HasValue)
            {
                var w = _world.Clans.FirstOrDefault(c => c.Id == s.WinnerClanId.Value);
                AppendTo(rtbResult, $"勝利勢力  : {w?.Name ?? "?"}\n", Color.FromArgb(255, 220, 60));
            }
            AppendTo(rtbResult, $"\n戦闘:{s.BattleCount}  補給:{s.SupplyCount}  離反:{s.BetrayalCount}  拒否:{s.RefusalCount}  独断:{s.IndependentCount}\n");
        }

        // ── バッチ実行 ──────────────────────────────────────────
        private void BtnBatchRun_Click(object? sender, EventArgs e)
        {
            int runs     = (int)nudBatchRuns.Value;
            int maxTurns = (int)nudBatchTurns.Value;
            int? seed    = chkUseSeed.Checked ? (int)nudSeed.Value : null;

            pbBatch.Maximum = runs; pbBatch.Value = 0;
            rtbBatch.Clear();
            btnBatchRun.Enabled = false; btnBatchStop.Enabled = true; btnExportCsv.Enabled = false;
            lblBatchStatus.Text = $"実行中... 0 / {runs}";

            _runner = new SimulationRunner(_engine) { IntervalMs = 0 };

            _ = _runner.RunBatchAsync(
                engineFactory: s => BuildBatchEngine(s),
                runs: runs,
                maxTurns: maxTurns,
                baseSeed: seed,
                onRunCompleted: (i, stats) => Invoke(() =>
                {
                    pbBatch.Value       = i;
                    lblBatchStatus.Text = $"実行中... {i} / {runs}";
                }),
                external: default
            ).ContinueWith(t => Invoke(() =>
            {
                _lastSummary = t.Result;
                btnBatchRun.Enabled  = true;
                btnBatchStop.Enabled = false;
                btnExportCsv.Enabled = true;
                lblBatchStatus.Text  = $"完了: {runs}回";
                ShowBatchSummary(_lastSummary, runs);
            }));
        }

        private SimulationEngine BuildBatchEngine(int? seed)
        {
            var (world, _, triggers) = ScenarioLoader.Load(_scenarioPath);
            var aiParams = BattleCore.AI.AiParamsLoader.LoadFromBaseDir();
            var ctx = new SimulationContext(world, new BattleCore.Simulation.GameTime(seed));
            var eng = new SimulationEngine(ctx);
            eng.Register(new VisionSystem());
            eng.Register(new CastleSystem());
            eng.Register(new ClanDecisionSystem(new AggressiveClanStrategy(),
                new BattleCore.AI.OfficerDecision(aiParams)));
            eng.Register(new CommandExecutionSystem());
            eng.Register(new MovementSystem());
            eng.Register(new BattleSystem());
            eng.Register(new LoyaltySystem());
            eng.Register(new RecruitmentSystem());
            eng.Register(new SupplySystem());
            eng.Register(new RelationshipSystem());
            eng.Register(new DiplomacySystem(autoAllianceInterval: 0));
            eng.Register(new EventTriggerSystem(triggers));
            eng.Register(new VictorySystem());
            return eng;
        }

        private void BtnBatchStop_Click(object? sender, EventArgs e)
        {
            _runner?.Stop();
            btnBatchRun.Enabled = true; btnBatchStop.Enabled = false;
        }

        private void BtnExportCsv_Click(object? sender, EventArgs e)
        {
            if (_lastSummary == null) return;
            using var dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv", DefaultExt = "csv",
                FileName = $"battlecore_batch_{DateTime.Now:yyyyMMdd_HHmmss}",
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            File.WriteAllLines(dlg.FileName,
                _lastSummary.ToCsvLines(id => id.HasValue
                    ? _world.Clans.FirstOrDefault(c => c.Id == id.Value)?.Name ?? "?"
                    : "引き分け"),
                System.Text.Encoding.UTF8);
            MessageBox.Show($"CSV出力完了:\n{dlg.FileName}", "完了",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowBatchSummary(BattleRunSummary summary, int runs)
        {
            var r = rtbBatch;
            r.Clear();
            AppendTo(r, $"=== Batch Result ({summary.TotalRuns}回) ===\n", Color.FromArgb(180, 180, 255));

            var turnStats   = summary.CalcStats(x => x.TurnsExecuted);
            var battleStats = summary.CalcStats(x => x.BattleCount);
            AppendTo(r, $"勝利ターン : {turnStats}\n");
            AppendTo(r, $"戦闘回数 : {battleStats}\n\n");

            AppendTo(r, "--- 勢力別勝率 ---\n", Color.FromArgb(100, 200, 255));
            var wins = summary.WinsByClan();
            foreach (var clan in _world.Clans)
            {
                wins.TryGetValue(clan.Id, out int w);
                double pct = summary.TotalRuns > 0 ? w * 100.0 / summary.TotalRuns : 0;
                AppendTo(r, $"  {clan.Name,-8} {w,3}勝  {pct:F1}%\n",
                    pct >= 50 ? Color.FromArgb(100, 255, 150) : Color.White);
            }
            int draws = summary.TotalRuns - wins.Values.Sum();
            if (draws > 0) AppendTo(r, $"  引き分け  {draws,3}回\n", Color.Gray);
            AppendTo(r, "\n");

            AppendTo(r, "--- 性格別統計 ---\n", Color.FromArgb(100, 200, 255));
            foreach (var (_, ps) in summary.StatsByPersonality().OrderBy(x => x.Key))
            {
                double refAvg = summary.TotalRuns > 0 ? ps.RefusalCount     / (double)summary.TotalRuns : 0;
                double indAvg = summary.TotalRuns > 0 ? ps.IndependentCount / (double)summary.TotalRuns : 0;
                AppendTo(r, $"  {ps.Personality,-12} 拒否:{refAvg:F1}回/試行  独断:{indAvg:F1}回/試行\n",
                    Color.FromArgb(200, 160, 255));
            }
            AppendTo(r, "\n");

            // イベント統計
            var evStats = summary.EventStats();
            if (evStats.Count > 0)
            {
                AppendTo(r, "--- イベント統計 ---\n", Color.FromArgb(100, 200, 255));
                foreach (var (id, es) in evStats.OrderBy(x => x.Key))
                {
                    double rate = es.FiredRate(summary.TotalRuns);
                    AppendTo(r, $"  {id}\n", Color.FromArgb(255, 200, 80));
                    AppendTo(r, $"    発火率: {rate:F1}% ({es.FiredCount}/{summary.TotalRuns})\n", Color.FromArgb(180, 180, 180));
                    foreach (var clan in _world.Clans)
                    {
                        es.WinsByClan.TryGetValue(clan.Id, out int cw);
                        if (cw == 0) continue;
                        double cwPct = es.FiredCount > 0 ? cw * 100.0 / es.FiredCount : 0;
                        AppendTo(r, $"    {clan.Name}勝利: {cwPct:F1}% ({cw}回)\n", Color.FromArgb(140, 220, 140));
                    }
                }
                AppendTo(r, "\n");
            }

            // 現在のAIパラメータ表示
            var aiParams = BattleCore.AI.AiParamsLoader.LoadFromBaseDir();
            AppendTo(r, "--- AIパラメータ (ai_params.json) ---\n", Color.FromArgb(100, 200, 255));
            AppendTo(r, $"  拒否忠誠閘値    : {aiParams.RefusalLoyaltyThreshold}\n", Color.FromArgb(140, 140, 140));
            AppendTo(r, $"  慎重撤退兵力閘値  : {aiParams.CautiousRetreatSoldiers}\n", Color.FromArgb(140, 140, 140));
            AppendTo(r, $"  独断行動忠誠閘値  : {aiParams.IndependentActionLoyalty}\n", Color.FromArgb(140, 140, 140));
            AppendTo(r, $"  不満反感閘値    : {aiParams.DissatisfiedDislikeThreshold}\n", Color.FromArgb(140, 140, 140));
        }

        // ── ヘルパー ──────────────────────────────────────────────
        private static void Add(Control parent, Control child) => parent.Controls.Add(child);

        private static Button Btn(string text, Point loc, Color bg) => new()
        {
            Text = text, Location = loc, Size = new Size(100, 28),
            BackColor = bg, ForeColor = Color.White,
        };

        private static RichTextBox Rtb(Point loc, Size size) => new()
        {
            Location = loc, Size = size,
            BackColor = Color.FromArgb(15, 15, 30), ForeColor = Color.White,
            Font = new Font("MS Gothic", 9f), ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle, ScrollBars = RichTextBoxScrollBars.Vertical,
        };

        private static void AppendTo(RichTextBox rtb, string text, Color? color = null)
        {
            rtb.SelectionStart  = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor  = color ?? Color.White;
            rtb.AppendText(text);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _runner?.Stop();
            base.OnFormClosing(e);
        }
    }
}
