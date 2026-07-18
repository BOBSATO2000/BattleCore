using BattleCore.AI;
using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Relations;
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

        // Hex描画サイズ
        private const int HexSize = 36;

        public frMain()
        {
            InitializeComponent();
            InitSimulation();
        }

        // -------------------------------------------------------
        // シミュレーション初期化
        // -------------------------------------------------------
        private void InitSimulation()
        {
            world = new WorldState();

            // マップ生成（5×5のHexグリッド）
            int id = 1;
            for (int q = 0; q < 5; q++)
            for (int r = 0; r < 5; r++)
            {
                var terrain = (q == 2 && r == 2) ? TerrainType.Mountain
                            : (q % 3 == 0)       ? TerrainType.Forest
                            : TerrainType.Plain;
                world.Map.AddHex(new Hex(id++, q, r, terrain));
            }

            // 勢力
            var clanA = new Clan(1) { Name = "織田" };
            var clanB = new Clan(2) { Name = "武田" };
            var clanC = new Clan(3) { Name = "上杉" };
            world.Clans.AddRange(new[] { clanA, clanB, clanC });

            // 武将
            var nobunaga  = new Officer(1, "信長") { Leadership = 150, Strategy = 120, Courage = 130, Ambition = 70, Loyalty = 80 };
            var shingen   = new Officer(2, "信玄") { Leadership = 140, Strategy = 150, Courage = 110, Ambition = 50, Loyalty = 90 };
            var kenshin   = new Officer(3, "謙信") { Leadership = 145, Strategy = 140, Courage = 140, Ambition = 30, Loyalty = 95 };
            var mitsuhide = new Officer(4, "光秀") { Leadership = 120, Strategy = 130, Courage = 100, Ambition = 90, Loyalty = 40 };
            world.Officers.AddRange(new[] { nobunaga, shingen, kenshin, mitsuhide });

            world.Memberships.Add(new Membership(1, nobunaga.Id,  clanA.Id) { Loyalty = 80 });
            world.Memberships.Add(new Membership(2, mitsuhide.Id, clanA.Id) { Loyalty = 40 }); // 野心家
            world.Memberships.Add(new Membership(3, shingen.Id,   clanB.Id) { Loyalty = 90 });
            world.Memberships.Add(new Membership(4, kenshin.Id,   clanC.Id) { Loyalty = 95 });

            // 光秀→信長への反感（離反の布石）
            world.Relationships.Add(new Relationship(1, mitsuhide.Id, nobunaga.Id) { Dislike = 60 });

            // 軍隊（左上・右上・下中央に配置）
            var armyA = new Army(1, 1, clanA.Id, 1);   // 織田: q=0,r=0 左上
            armyA.AssignOfficer(nobunaga.Id);

            var armyA2 = new Army(2, 1, clanA.Id, 5);  // 織田第2軍: q=0,r=4 左下
            armyA2.AssignOfficer(mitsuhide.Id);

            var armyB = new Army(3, 2, clanB.Id, 21);  // 武田: q=4,r=0 右上
            armyB.AssignOfficer(shingen.Id);

            var armyC = new Army(4, 3, clanC.Id, 23);  // 上杉: q=4,r=2 右中
            armyC.AssignOfficer(kenshin.Id);

            world.Armies.AddRange(new[] { armyA, armyA2, armyB, armyC });

            // エンジン構築
            engine = new SimulationEngine(world);
            engine.Register(new ClanDecisionSystem(new AggressiveClanStrategy()));
            engine.Register(new CommandExecutionSystem());
            engine.Register(new MovementSystem());
            engine.Register(new BattleSystem());
            engine.Register(new LoyaltySystem());

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

        // -------------------------------------------------------
        // UI更新
        // -------------------------------------------------------
        private void UpdateUI()
        {
            var t = engine.Context.Time;
            lblStatus.Text = $"Tick: {t.Tick}  Year: {t.Year}  {SeasonName(t.Season)}";

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
                if (ev is BetrayalEvent b)
                {
                    var officer = world.Officers.FirstOrDefault(o => o.Id == b.OfficerId);
                    var clan    = world.Clans.FirstOrDefault(c => c.Id == b.FromClanId);
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] {officer?.Name ?? "?"} が {clan?.Name ?? "?"} を離反！");
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
            }

            // 軍隊描画（兵力0は描画しない）
            foreach (var army in world.Armies.Where(a => a.Soldiers > 0))
            {
                var hex = world.Map.GetHexById(army.CurrentHexId);
                if (hex == null) continue;

                var center  = HexToPixel(hex.X, hex.Y);
                var clan    = world.Clans.FirstOrDefault(c => c.Id == army.ClanId);
                var officer = army.OfficerId.HasValue
                    ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId)
                    : null;

                // 勢力色
                var color = army.ClanId switch
                {
                    1 => Color.FromArgb(220, 80,  80),   // 織田: 赤
                    2 => Color.FromArgb(80,  80,  220),  // 武田: 青
                    3 => Color.FromArgb(80,  180, 80),   // 上杉: 緑
                    _ => Color.Gray,                     // 無所属
                };

                // 駒の円
                g.FillEllipse(new SolidBrush(color),
                    center.X - 14, center.Y - 14, 28, 28);
                g.DrawEllipse(new Pen(Color.White, 1f),
                    center.X - 14, center.Y - 14, 28, 28);

                // 武将名
                var officerName = officer?.Name ?? "?";
                var nameSize    = g.MeasureString(officerName, new Font("MS Gothic", 8f));
                g.DrawString(officerName, new Font("MS Gothic", 8f),
                    Brushes.White,
                    center.X - nameSize.Width / 2,
                    center.Y - nameSize.Height / 2);

                // 兵力バー
                const int barW = 28;
                const int barH = 4;
                float barX = center.X - barW / 2;
                float barY = center.Y + 16;
                float fill = Math.Clamp(army.Soldiers / 1000f, 0f, 1f);

                g.FillRectangle(Brushes.DarkGray,  barX, barY, barW, barH);
                g.FillRectangle(new SolidBrush(color), barX, barY, barW * fill, barH);
                g.DrawRectangle(new Pen(Color.White, 0.5f), barX, barY, barW, barH);

                // 兵力数
                g.DrawString(army.Soldiers.ToString(),
                    new Font("Arial", 7f), Brushes.White,
                    center.X - 12, center.Y + 21);
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
