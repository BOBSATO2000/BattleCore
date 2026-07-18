using BattleCore.AI;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Scenario;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.Systems.Battle;
using BattleCore.World;

namespace BattleCoreStudio
{
    public partial class frMain : Form
    {
        private SimulationEngine engine = null!;
        private WorldState       world  = null!;
        private readonly System.Windows.Forms.Timer autoTimer = new();

        // Hex描画サイズ
        private const int HexSize = 36;

        public frMain()
        {
            InitializeComponent();
            InitSimulation();

            autoTimer.Tick += (s, e) => { engine.Step(); UpdateUI(); };
        }

        // -------------------------------------------------------
        // シミュレーション初期化
        // -------------------------------------------------------
        private void InitSimulation()
        {
            var scenariosFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "scenarios");

            using var dlg = new frScenarioSelect(scenariosFolder);
            if (dlg.ShowDialog() != DialogResult.OK || dlg.SelectedPath == null)
            {
                Application.Exit();
                return;
            }

            (world, var title, var triggers) = ScenarioLoader.Load(dlg.SelectedPath);
            Text = $"BattleCoreStudio - {title}";

            engine = new SimulationEngine(world);
            engine.Register(new CastleSystem());
            engine.Register(new ClanDecisionSystem(new AggressiveClanStrategy()));
            engine.Register(new CommandExecutionSystem());
            engine.Register(new MovementSystem());
            engine.Register(new BattleSystem());
            engine.Register(new LoyaltySystem());
            engine.Register(new RecruitmentSystem());
            engine.Register(new SupplySystem());
            engine.Register(new RelationshipSystem());
            engine.Register(new DiplomacySystem());
            engine.Register(new EventTriggerSystem(triggers));
            engine.Register(new VictorySystem());

            UpdateUI();
        }

        // -------------------------------------------------------
        // 1ターン進める
        // -------------------------------------------------------
        private void btnStep_Click(object sender, EventArgs e)
        {
            engine.Step();
            UpdateUI();
        }

        private void btnAuto_Click(object sender, EventArgs e)
        {
            autoTimer.Interval = cmbSpeed.SelectedIndex switch
            {
                0 => 2000,
                2 => 500,
                _ => 1000,
            };
            autoTimer.Start();
            btnAuto.Enabled = false;
            btnStop.Enabled = true;
            btnStep.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            autoTimer.Stop();
            btnAuto.Enabled = true;
            btnStop.Enabled = false;
            btnStep.Enabled = true;
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            autoTimer.Stop();
            lstEvents.Items.Clear();
            btnRestart.Enabled = false;
            btnStep.Enabled    = true;
            btnAuto.Enabled    = true;
            btnStop.Enabled    = false;
            InitSimulation();
        }

        // -------------------------------------------------------
        // 武将選択 → 関係値表示
        // -------------------------------------------------------
        private void lstArmies_SelectedIndexChanged(object sender, EventArgs e)
        {
            var idx = lstArmies.SelectedIndex;
            if (idx < 0) return;

            var armies = world.Armies.OrderBy(a => a.ClanId).ToList();
            if (idx >= armies.Count) return;

            var army = armies[idx];
            if (!army.OfficerId.HasValue) return;

            var officer = world.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value);
            if (officer == null) return;

            var rels = world.Relationships
                .Where(r => r.FromOfficerId == officer.Id)
                .ToList();

            lstEvents.Items.Insert(0, $"── {officer.Name} の関係値 ──");
            if (!rels.Any())
            {
                lstEvents.Items.Insert(1, "  (関係なし)");
                return;
            }
            foreach (var rel in rels)
            {
                var target = world.Officers.FirstOrDefault(o => o.Id == rel.ToOfficerId);
                lstEvents.Items.Insert(1,
                    $"  →{target?.Name ?? "?"}  T:{rel.Trust} R:{rel.Respect} D:{rel.Dislike}");
            }
        }

        // -------------------------------------------------------
        // 武将ダブルクリック → 詳細ポップアップ
        // -------------------------------------------------------
        private void lstArmies_DoubleClick(object sender, EventArgs e)
        {
            var idx = lstArmies.SelectedIndex;
            if (idx < 0) return;

            var armies = world.Armies.OrderBy(a => a.ClanId).ToList();
            if (idx >= armies.Count) return;

            var army    = armies[idx];
            var officer = army.OfficerId.HasValue
                ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value)
                : null;
            var clan = world.Clans.FirstOrDefault(c => c.Id == army.ClanId);
            if (officer == null) return;

            var rels = world.Relationships
                .Where(r => r.FromOfficerId == officer.Id)
                .Select(r =>
                {
                    var t = world.Officers.FirstOrDefault(o => o.Id == r.ToOfficerId);
                    return $"  →{t?.Name ?? "?"}  信頼:{r.Trust} 尊敬:{r.Respect} 反感:{r.Dislike}";
                }).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"【{clan?.Name ?? "無所属"}】{officer.Name}");
            sb.AppendLine();
            sb.AppendLine($"統率:{officer.Leadership,3}  戦術:{officer.Strategy,3}  武勇:{officer.Courage,3}");
            sb.AppendLine($"知略:{officer.Intelligence,3}  野心:{officer.Ambition,3}  忠誠:{officer.Loyalty,3}");
            sb.AppendLine();
            sb.AppendLine($"兵力: {army.Soldiers} / 1000");
            sb.AppendLine();
            sb.AppendLine(rels.Any() ? "関係値:" : "関係値: なし");
            rels.ForEach(l => sb.AppendLine(l));

            MessageBox.Show(sb.ToString(), $"{officer.Name} の詳細",
                MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        // -------------------------------------------------------
        // UI更新
        // -------------------------------------------------------
        private void UpdateUI()
        {
            var t = engine.Context.Time;
            var weatherText = t.Weather switch
            {
                BattleCore.Simulation.Weather.Rain => " 🌧雨",
                BattleCore.Simulation.Weather.Fog  => " 🌫霧",
                _                                  => " ☀晴",
            };
            lblStatus.Text = $"Tick: {t.Tick}  Year: {t.Year}  {SeasonName(t.Season)}{weatherText}";

            // 勢力概要パネル
            pnlClans.Controls.Clear();
            int py = 4;
            foreach (var clan in world.Clans)
            {
                var armies      = world.Armies.Where(a => a.ClanId == clan.Id).ToList();
                var totalSoldiers = armies.Sum(a => a.Soldiers);
                var activeArmies  = armies.Count(a => a.Soldiers > 0);
                var color = clan.Id switch
                {
                    1 => Color.FromArgb(220, 80,  80),
                    2 => Color.FromArgb(80,  80,  220),
                    3 => Color.FromArgb(80,  180, 80),
                    _ => Color.Gray,
                };
                var allies = world.Alliances
                    .Where(a => a.Involves(clan.Id))
                    .Select(a =>
                    {
                        var allyClanId = a.ClanId1 == clan.Id ? a.ClanId2 : a.ClanId1;
                        return world.Clans.FirstOrDefault(c => c.Id == allyClanId)?.Name ?? "?";
                    })
                    .ToList();
                var allyText = allies.Any() ? $" [{string.Join(",", allies)}と同盟]" : "";
                var lbl = new Label
                {
                    Text      = $"■ {clan.Name}  兵:{totalSoldiers:#,0}  軍:{activeArmies}{allyText}",
                    ForeColor = color,
                    Location  = new Point(4, py),
                    Size      = new Size(190, 20),
                    Font      = new Font("MS Gothic", 9f),
                };
                pnlClans.Controls.Add(lbl);
                py += 22;
            }
            // 軍隊リスト（兵力0は[全滅]と表示）
            lstArmies.Items.Clear();
            foreach (var army in world.Armies.OrderBy(a => a.ClanId))
            {
                var clan    = world.Clans.FirstOrDefault(c => c.Id == army.ClanId);
                var officer = army.OfficerId.HasValue
                    ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId)
                    : null;

                var clanName    = clan?.Name    ?? "無所属";
                var officerName = officer?.Name ?? "-";
                var status      = army.Soldiers == 0 ? " [全滅]" : "";
                lstArmies.Items.Add($"[{clanName}] {officerName} 兵:{army.Soldiers} Hex:{army.CurrentHexId}{status}");
            }

            // イベントログ
            while (engine.Context.EventQueue.Count > 0)
            {
                var ev = engine.Context.EventQueue.Dequeue();
                if (ev is BattleLogEvent bl)
                {
                    var terrainTag = bl.TerrainBonus
                        ? bl.Terrain == TerrainType.Forest ? "[森-20%]" : "[山-30%]"
                        : "";
                    var weatherTag = bl.RainPenalty  ? "[雨-10%]" : "";
                    var castleTag  = bl.CastleBonus  ? "[城-20%]"  : "";
                    var growthTag  = bl.GrowthDetail != null ? $" ★{bl.GrowthDetail}" : "";
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] {bl.WinnerName}勝 損:{bl.WinnerLosses} / {bl.LoserName}敗 損:{bl.LoserLosses}{terrainTag}{weatherTag}{castleTag}{growthTag}");
                }
                else if (ev is CastleCapturedEvent cc)
                {
                    var clan = world.Clans.FirstOrDefault(c => c.Id == cc.NewOwnerClanId);
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] 「{cc.CastleName}」を {clan?.Name ?? "?"} が占領！");
                }
                else if (ev is BetrayalEvent b)
                {
                    var officer = world.Officers.FirstOrDefault(o => o.Id == b.OfficerId);
                    var clan    = world.Clans.FirstOrDefault(c => c.Id == b.FromClanId);
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] {officer?.Name ?? "?"} が {clan?.Name ?? "?"} を離反！");
                }
                else if (ev is RecruitEvent r)
                {
                    var officer = world.Officers.FirstOrDefault(o => o.Id == r.OfficerId);
                    var clan    = world.Clans.FirstOrDefault(c => c.Id == r.ToClanId);
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] {officer?.Name ?? "?"} が {clan?.Name ?? "?"} に仕官！");
                }
                else if (ev is ScenarioEvent se)
                {
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] {se.Message}");
                }
                else if (ev is GameOverEvent go)
                {
                    autoTimer.Stop();
                    btnAuto.Enabled    = false;
                    btnStop.Enabled    = false;
                    btnStep.Enabled    = false;
                    btnRestart.Enabled = true;
                    lstEvents.Items.Insert(0, $"[Tick{t.Tick}] 【ゲーム終了】{go.Reason}");
                    MessageBox.Show(go.Reason, "ゲーム終了",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            pnlMap.Invalidate();
        }

        // -------------------------------------------------------
        // Hexマップ描画
        // -------------------------------------------------------
        private void pnlMap_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var hex in world.Map.Hexes)
            {
                var center = HexToPixel(hex.X, hex.Y);
                var points = HexCorners(center);

                // 地形色
                var fillColor = hex.Terrain switch
                {
                    TerrainType.Mountain => Color.FromArgb(100, 100, 120),
                    TerrainType.Forest   => Color.FromArgb(40,  100,  60),
                    _                    => Color.FromArgb(80,  120,  80),
                };

                g.FillPolygon(new SolidBrush(fillColor), points);
                g.DrawPolygon(new Pen(Color.FromArgb(60, 60, 80), 1.5f), points);

                // Hex ID
                g.DrawString(hex.Id.ToString(), new Font("Arial", 7f),
                    Brushes.White, center.X - 6, center.Y - 6);

                // 城アイコン
                var castle = world.Castles.FirstOrDefault(c => c.HexId == hex.Id);
                if (castle != null)
                {
                    var castleColor = castle.OwnerClanId switch
                    {
                        1 => Color.FromArgb(220, 80,  80),
                        2 => Color.FromArgb(80,  80,  220),
                        3 => Color.FromArgb(80,  180, 80),
                        4 => Color.FromArgb(200, 160, 40),
                        _ => Color.White,
                    };
                    g.DrawString("⛩", new Font("Segoe UI Emoji", 11f),
                        new SolidBrush(castleColor),
                        center.X - 9, center.Y + 8);
                }
            }

            // 移動先矢印（DestinationHexId がある軍）
            var arrowPen = new Pen(Color.FromArgb(200, 255, 255, 100), 1.5f)
            {
                CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 4)
            };
            foreach (var army in world.Armies.Where(a => a.Soldiers > 0 && a.DestinationHexId != null))
            {
                var fromHex = world.Map.GetHexById(army.CurrentHexId);
                var toHex   = world.Map.GetHexById(army.DestinationHexId!.Value);
                if (fromHex == null || toHex == null) continue;
                var from = HexToPixel(fromHex.X, fromHex.Y);
                var to   = HexToPixel(toHex.X,   toHex.Y);
                g.DrawLine(arrowPen, from, to);
            }

            // 軍隊描画（同Hexに複数いる場合はオフセット）
            var armiesByHex = world.Armies
                .Where(a => a.Soldiers > 0)
                .GroupBy(a => a.CurrentHexId);

            foreach (var group in armiesByHex)
            {
                var hex = world.Map.GetHexById(group.Key);
                if (hex == null) continue;
                var baseCenter = HexToPixel(hex.X, hex.Y);

                var list = group.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var army    = list[i];
                    var clan    = world.Clans.FirstOrDefault(c => c.Id == army.ClanId);
                    var officer = army.OfficerId.HasValue
                        ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId)
                        : null;

                    // 複数駒のオフセット（最大4つ想定）
                    PointF center = list.Count == 1 ? baseCenter : i switch
                    {
                        0 => new PointF(baseCenter.X - 10, baseCenter.Y - 8),
                        1 => new PointF(baseCenter.X + 10, baseCenter.Y - 8),
                        2 => new PointF(baseCenter.X - 10, baseCenter.Y + 8),
                        _ => new PointF(baseCenter.X + 10, baseCenter.Y + 8),
                    };

                    // 勢力色
                    var color = army.ClanId switch
                    {
                        1 => Color.FromArgb(220, 80,  80),
                        2 => Color.FromArgb(80,  80,  220),
                        3 => Color.FromArgb(80,  180, 80),
                        4 => Color.FromArgb(200, 160, 40),
                        _ => Color.Gray,
                    };

                    // 駒の円
                    g.FillEllipse(new SolidBrush(color),
                        center.X - 14, center.Y - 14, 28, 28);
                    g.DrawEllipse(new Pen(Color.White, 1f),
                        center.X - 14, center.Y - 14, 28, 28);

                    // 武将名
                    var officerName = officer?.Name ?? "?";
                    var nameFont    = new Font("MS Gothic", 8f);
                    var nameSize    = g.MeasureString(officerName, nameFont);
                    g.DrawString(officerName, nameFont, Brushes.White,
                        center.X - nameSize.Width / 2,
                        center.Y - nameSize.Height / 2);

                    // 兵力バー
                    const int barW = 28;
                    const int barH = 4;
                    float barX = center.X - barW / 2;
                    float barY = center.Y + 16;
                    int   maxSoldiers = Math.Max(army.Soldiers, 1000);
                    float fill = Math.Clamp(army.Soldiers / (float)maxSoldiers, 0f, 1f);

                    g.FillRectangle(Brushes.DarkGray, barX, barY, barW, barH);
                    g.FillRectangle(new SolidBrush(color), barX, barY, barW * fill, barH);
                    g.DrawRectangle(new Pen(Color.White, 0.5f), barX, barY, barW, barH);

                    // 兵力数
                    g.DrawString(army.Soldiers.ToString(),
                        new Font("Arial", 7f), Brushes.White,
                        center.X - 12, center.Y + 21);
                }
            }

            // 凡例
            DrawLegend(g);
        }

        // -------------------------------------------------------
        // 凡例描画
        // -------------------------------------------------------
        private static void DrawLegend(Graphics g)
        {
            var items = new[]
            {
                (Color.FromArgb(80,  120,  80), "平地"),
                (Color.FromArgb(40,  100,  60), "森 (移動+1Tick)"),
                (Color.FromArgb(100, 100, 120), "山 (移動不可)"),
            };
            float lx = 8, ly = 8;
            foreach (var (color, label) in items)
            {
                g.FillRectangle(new SolidBrush(color), lx, ly, 12, 12);
                g.DrawRectangle(new Pen(Color.Gray, 0.5f), lx, ly, 12, 12);
                g.DrawString(label, new Font("MS Gothic", 7.5f), Brushes.White, lx + 15, ly);
                ly += 16;
            }
        }

        // -------------------------------------------------------
        // Hex座標 → ピクセル座標（オフセット座標系）
        // -------------------------------------------------------
        private static PointF HexToPixel(int q, int r)
        {
            float x = HexSize * 1.5f * q + 50;
            float y = HexSize * MathF.Sqrt(3) * (r + q * 0.5f) + 50;
            return new PointF(x, y);
        }

        private static PointF[] HexCorners(PointF center)
        {
            var pts = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = MathF.PI / 180f * (60f * i - 30f);
                pts[i] = new PointF(
                    center.X + HexSize * MathF.Cos(angle),
                    center.Y + HexSize * MathF.Sin(angle));
            }
            return pts;
        }

        private static string SeasonName(BattleCore.Simulation.Season s) => s switch
        {
            BattleCore.Simulation.Season.Spring => "春",
            BattleCore.Simulation.Season.Summer => "夏",
            BattleCore.Simulation.Season.Autumn => "秋",
            BattleCore.Simulation.Season.Winter => "冬",
            _ => ""
        };
    }
}
