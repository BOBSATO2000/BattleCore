using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Player;
using BattleCore.Save;
using BattleCore.Scenario;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.Systems.Battle;
using BattleCore.World;

namespace BattleCoreStudio
{
    public partial class frMain : Form
    {
        private SimulationEngine engine          = null!;
        private WorldState       world           = null!;
        private PlayerCommander? playerCommander = null;   // null = 観戦モード
        private readonly System.Windows.Forms.Timer autoTimer = new();

        private string _scenarioId      = "";
        private string? _currentSavePath = null;
        private string  _scenarioPath    = "";
        private int?   _selectedArmyId  = null;
        private int    _playerClanId    = 0;   // 0=観戦モード
        private readonly Dictionary<int, (string Summary, IReadOnlyList<string> Factors)> _lastDecisions = new();
        private readonly DebugOverlay _overlay = new();

        private const string SaveFilter = "BattleCore Save (*.bcsave)|*.bcsave|All files (*.*)|*.*";

        // Hex描画サイズ
        private const int HexSize = 36;

        public frMain()
        {
            InitializeComponent();
            InitSimulation();

            autoTimer.Tick += (s, e) => { engine.Step(); UpdateUI(); };
            KeyPreview = true;
            KeyDown   += frMain_KeyDown;

            pnlMap.MouseClick += PnlMap_MouseClick;
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

            _scenarioId      = Path.GetFileNameWithoutExtension(dlg.SelectedPath);
            _scenarioPath    = dlg.SelectedPath;
            _currentSavePath = null;
            _playerClanId    = dlg.PlayerClanId;

            // プレイヤー勢力を IsPlayerControlled に設定
            foreach (var clan in world.Clans)
                clan.IsPlayerControlled = (_playerClanId != 0 && clan.Id == _playerClanId);

            playerCommander = _playerClanId != 0 ? new PlayerCommander(_playerClanId) : null;
            engine = BuildEngine(new SimulationContext(world), triggers);

            UpdateUI();
        }

        /// <summary>
        /// SimulationEngine を構築する。InitSimulation / LoadFromFile の共通処理。
        /// CommanderSystem が AICommander / PlayerCommander を統一管理する。
        /// </summary>
        private SimulationEngine BuildEngine(
            SimulationContext context,
            System.Collections.Generic.List<BattleCore.Scenario.EventTriggerData>? triggers = null)
        {
            var aiParams = BattleCore.AI.AiParamsLoader.LoadFromBaseDir();
            var strategy = new AggressiveClanStrategy();
            var officerDecision = new BattleCore.AI.OfficerDecision(aiParams);

            // CommanderSystem に各勢力の Commander を登録
            var commanderSystem = new CommanderSystem(officerDecision);
            foreach (var clan in context.World.Clans)
            {
                if (playerCommander != null && clan.Id == playerCommander.ClanId)
                    commanderSystem.Register(playerCommander);
                else
                    commanderSystem.Register(new AICommander(clan.Id, strategy));
            }

            var eng = new SimulationEngine(context);
            eng.Register(new VisionSystem());
            eng.Register(new CastleSystem());
            eng.Register(new SupplyLineSystem());
            eng.Register(new MoraleSystem());
            eng.Register(new FoodSystem());
            eng.Register(new FatigueSystem());
            eng.Register(new SiegeSystem());
            eng.Register(new IntelSystem());
            eng.Register(commanderSystem);          // ClanDecisionSystem の代替
            eng.Register(new CommandExecutionSystem());
            eng.Register(new MovementSystem());
            eng.Register(new BattleSystem());
            eng.Register(new LoyaltySystem());
            eng.Register(new RecruitmentSystem());
            eng.Register(new SupplySystem(baseReplenishment: 20, springBonus: 10));
            eng.Register(new RelationshipSystem());
            eng.Register(new DiplomacySystem(autoAllianceInterval: 0));
            eng.Register(new EventTriggerSystem(
                triggers ?? new System.Collections.Generic.List<BattleCore.Scenario.EventTriggerData>()));
            eng.Register(new VictorySystem());
            return eng;
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
        // メニューハンドラ
        // -------------------------------------------------------
        private void menuNew_Click(object sender, EventArgs e)
        {
            autoTimer.Stop();
            lstEvents.Items.Clear();
            btnRestart.Enabled = false;
            btnStep.Enabled    = true;
            btnAuto.Enabled    = true;
            btnStop.Enabled    = false;
            InitSimulation();
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            if (_currentSavePath == null)
            {
                menuSaveAs_Click(sender, e);
                return;
            }
            SaveToFile(_currentSavePath);
        }

        private void menuSaveAs_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Filter      = SaveFilter,
                DefaultExt  = "bcsave",
                FileName    = $"{_scenarioId}_turn{engine.Context.Time.Tick}",
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _currentSavePath = dlg.FileName;
            SaveToFile(_currentSavePath);
        }

        private void menuLoad_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = SaveFilter };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            LoadFromFile(dlg.FileName);
        }

        private void menuExit_Click(object sender, EventArgs e) => Application.Exit();

        private void menuSimRun_Click(object sender, EventArgs e)
        {
            autoTimer.Stop();
            btnAuto.Enabled = true;
            btnStop.Enabled = false;
            btnStep.Enabled = true;
            using var dlg = new frSimRunner(engine, world, _scenarioPath);
            dlg.ShowDialog(this);
            UpdateUI();
        }

        private void SaveToFile(string path)
        {
            try
            {
                SaveSystem.Save(engine.Context, _scenarioId, path);
                lstEvents.Items.Insert(0,
                    $"[Tick{engine.Context.Time.Tick}] セーブ完了: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"セーブに失敗しました。\n{ex.Message}",
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFromFile(string path)
        {
            try
            {
                autoTimer.Stop();
                btnAuto.Enabled = true;
                btnStop.Enabled = false;
                btnStep.Enabled = true;

                var (context, scenarioId) = SaveSystem.Load(path);
                _scenarioId      = scenarioId;
                _currentSavePath = path;
                world            = context.World;

                foreach (var clan in world.Clans)
                    clan.IsPlayerControlled = (_playerClanId != 0 && clan.Id == _playerClanId);

                playerCommander = _playerClanId != 0 ? new PlayerCommander(_playerClanId) : null;
                engine = BuildEngine(context);

                var meta = SaveSystem.LoadMetadata(path);
                Text = $"BattleCoreStudio - {meta.ScenarioId} [Turn {meta.Turn} 読込]「{Path.GetFileName(path)}」";

                lstEvents.Items.Clear();
                lstEvents.Items.Insert(0,
                    $"[Tick{context.Time.Tick}] ロード完了: {Path.GetFileName(path)}");

                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ロードに失敗しました。\n{ex.Message}",
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -------------------------------------------------------
        // Hexマップ クリック → 部隊選択 / 右クリックメニュー
        // -------------------------------------------------------
        private void PnlMap_MouseClick(object? sender, MouseEventArgs e)
        {
            // クリック座標からHexを特定
            var clickedHex = HitTestHex(e.Location);
            if (clickedHex == null) return;

            // そのHexにいる部隊を取得（プレイヤー勢力優先）
            var armiesOnHex = world.Armies
                .Where(a => a.Soldiers > 0 && a.CurrentHexId == clickedHex.Id)
                .OrderByDescending(a => a.ClanId == _playerClanId ? 1 : 0)
                .ToList();

            if (e.Button == MouseButtons.Left)
            {
                // 左クリック：部隊選択
                if (armiesOnHex.Count > 0)
                {
                    _selectedArmyId = armiesOnHex[0].Id;
                    // lstArmiesの選択も同期
                    var armies = world.Armies.OrderBy(a => a.ClanId).ToList();
                    var idx = armies.FindIndex(a => a.Id == _selectedArmyId);
                    if (idx >= 0) lstArmies.SelectedIndex = idx;
                    pnlMap.Invalidate();
                    UpdateDebugPanel();
                }
                else
                {
                    // 空Hexクリック：選択中プレイヤー部隊があれば移動命令
                    if (_selectedArmyId.HasValue && _playerClanId != 0)
                    {
                        var selArmy = world.GetArmyById(_selectedArmyId.Value);
                        if (selArmy != null && selArmy.ClanId == _playerClanId && selArmy.Soldiers > 0
                            && playerCommander != null)
                        {
                            playerCommander.EnqueueIntent(new Intent(
                                selArmy.Id, IntentType.MoveTo,
                                priority: 10, OrderLifetime.Persistent,
                                targetHexId: clickedHex.Id));
                            lstEvents.Items.Insert(0,
                                $"[命令] {GetOfficerName(selArmy)} → Hex{clickedHex.Id} へ移動");
                        }
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // 右クリック：プレイヤー部隊への命令メニュー
                var playerArmy = armiesOnHex.FirstOrDefault(a => a.ClanId == _playerClanId);
                if (playerArmy == null && _selectedArmyId.HasValue)
                {
                    var sel = world.GetArmyById(_selectedArmyId.Value);
                    if (sel?.ClanId == _playerClanId) playerArmy = sel;
                }
                if (playerArmy == null || _playerClanId == 0 || playerCommander == null) return;

                ShowOrderMenu(playerArmy, clickedHex.Id, e.Location);
            }
        }

        private void ShowOrderMenu(BattleCore.Entities.Army army, int targetHexId, Point screenPos)
        {
            if (playerCommander == null) return;
            var menu = new ContextMenuStrip();
            var officerName = GetOfficerName(army);

            menu.Items.Add($"【{officerName}】への命令").Enabled = false;
            menu.Items.Add(new ToolStripSeparator());

            void Issue(string label, IntentType type,
                int priority = 10, OrderLifetime lifetime = OrderLifetime.OneShot,
                int? hexId = null, int? castleId = null)
            {
                var item = menu.Items.Add(label);
                item.Click += (_, _) =>
                {
                    playerCommander.EnqueueIntent(
                        new Intent(army.Id, type, priority, lifetime, hexId, castleId));
                    lstEvents.Items.Insert(0, $"[命令] {officerName}：{label}");
                };
            }

            // 直接命令
            if (targetHexId != army.CurrentHexId)
                Issue($"Hex{targetHexId} へ移動",
                    IntentType.MoveTo, priority: 10, lifetime: OrderLifetime.Persistent, hexId: targetHexId);
            Issue("待機", IntentType.Wait);

            menu.Items.Add(new ToolStripSeparator());

            // 方針命令（具体行動はCommanderSystemが決定）
            Issue("⚔ 攻撃（最寄り敵城へ）",  IntentType.Attack);
            Issue("🛡 防御態勢",              IntentType.Defend);
            Issue("🏃 撤退（最寄り自城へ）",  IntentType.Retreat);
            Issue("🔭 偵察",                  IntentType.Scout);
            Issue("🌾 補給優先",              IntentType.Supply);
            Issue("🏗 築城",                  IntentType.Fortify);

            var nearbyCastle = world.Castles
                .Where(c => c.OwnerClanId != army.ClanId)
                .OrderBy(c =>
                {
                    var h  = world.Map.GetHexById(army.CurrentHexId);
                    var ch = world.Map.GetHexById(c.HexId);
                    return (h == null || ch == null) ? int.MaxValue : HexDistance.Calculate(h, ch);
                })
                .FirstOrDefault();
            if (nearbyCastle != null)
                Issue($"🏯 「{nearbyCastle.Name}」を包囲",
                    IntentType.Siege, castleId: nearbyCastle.Id);

            // 継続命令キャンセル
            if (playerCommander.HasPersistentIntent(army.Id))
            {
                menu.Items.Add(new ToolStripSeparator());
                var cancel = menu.Items.Add("❌ 継続命令をキャンセル");
                cancel.Click += (_, _) =>
                {
                    playerCommander.CancelIntent(army.Id);
                    lstEvents.Items.Insert(0, $"[命令取消] {officerName}の継続命令を解除");
                };
            }

            menu.Show(pnlMap, screenPos);
        }

        private string GetOfficerName(BattleCore.Entities.Army army)
        {
            if (!army.OfficerId.HasValue) return $"軍{army.Id}";
            return world.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value)?.Name ?? $"軍{army.Id}";
        }

        /// <summary>ピクセル座標からHexを逆引きする。</summary>
        private BattleCore.Map.Hex? HitTestHex(Point pt)
        {
            BattleCore.Map.Hex? best = null;
            float bestDist = float.MaxValue;
            foreach (var hex in world.Map.Hexes)
            {
                var center = HexToPixel(hex.X, hex.Y);
                float dx = pt.X - center.X;
                float dy = pt.Y - center.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist < HexSize && dist < bestDist)
                {
                    bestDist = dist;
                    best = hex;
                }
            }
            return best;
        }

        // -------------------------------------------------------
        // F1-F5 オーバーレイ切替
        // -------------------------------------------------------
        private void frMain_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_overlay.HandleKey(e.KeyCode)) return;
            rtbDebug.Visible  = _overlay.DebugConsole;
            //lblDebug.Visible  = _overlay.DebugConsole;
            lblStatus.Text    = _overlay.StatusText;
            pnlMap.Invalidate();
            e.Handled = true;
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
            _selectedArmyId = army.Id;

            if (!army.OfficerId.HasValue) return;
            var officer = world.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value);
            if (officer == null) return;

            var rels = world.Relationships.Where(r => r.FromOfficerId == officer.Id).ToList();
            lstEvents.Items.Insert(0, $"── {officer.Name} の関係値 ──");
            if (!rels.Any()) { lstEvents.Items.Insert(1, "  (関係なし)"); return; }
            foreach (var rel in rels)
            {
                var target = world.Officers.FirstOrDefault(o => o.Id == rel.ToOfficerId);
                lstEvents.Items.Insert(1, $"  →{target?.Name ?? "?"}  T:{rel.Trust} R:{rel.Respect} D:{rel.Dislike}");
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
            sb.AppendLine($"性格: {PersonalityName(officer.Personality)}");
            sb.AppendLine();
            sb.AppendLine($"統率:{officer.Leadership,3}  戦術:{officer.Strategy,3}  武勇:{officer.Courage,3}");
            sb.AppendLine($"知略:{officer.Intelligence,3}  野心:{officer.Ambition,3}  忠誠:{officer.Loyalty,3}");
            sb.AppendLine();
            sb.AppendLine($"兵力: {army.Soldiers} / 1000  AP: {army.ActionPoints}/{BattleCore.Entities.Army.MaxActionPoints}");
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
            var phaseText = engine.Context.CurrentPhase switch
            {
                TurnPhase.PlayerPhase => "[入力]",
                TurnPhase.AIPhase     => "[AI]",
                TurnPhase.Movement    => "[移動]",
                TurnPhase.Battle      => "[戦闘]",
                TurnPhase.Supply      => "[補給]",
                TurnPhase.Victory     => "[勝利判定]",
                _                     => "",
            };
            var playerText = _playerClanId != 0
                ? $"  ★{world.Clans.FirstOrDefault(c => c.Id == _playerClanId)?.Name ?? "?"}"
                : "  [観戦]";
            lblStatus.Text = $"Tick:{t.Tick}  {t.Year}年 {SeasonName(t.Season)}{weatherText}  {phaseText}{playerText}";

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
                var castleCount = world.Castles.Count(c => c.OwnerClanId == clan.Id);
                var castleText  = castleCount > 0 ? $" 城:{castleCount}" : "";
                var allyText = allies.Any() ? $" [{string.Join(",", allies)}と同盟]" : "";
                var lbl = new Label
                {
                    Text      = $"■ {clan.Name}  兵:{totalSoldiers:#,0}  軍:{activeArmies}{castleText}{allyText}",
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
                    lstEvents.Items.Insert(0, $"[Tick{t.Tick}] {bl.ToLogLine()}");
                }
                else if (ev is CastleCapturedEvent cc)
                {
                    var clan = world.Clans.FirstOrDefault(c => c.Id == cc.NewOwnerClanId);
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] 「{cc.CastleName}」を {clan?.Name ?? "?"} が占領！");
                }
                else if (ev is MovementEvent mv)
                {
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] 🚶 {mv.OfficerName} が Hex{mv.HexId} に到着");
                }
                else if (ev is SupplyEvent sv)
                {
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] 🌾 {sv.OfficerName} 補充+{sv.Amount} → {sv.NewSoldiers}兵");
                }
                else if (ev is OfficerRefusedOrderEvent rf)
                {
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] ⚠ {rf.OfficerName}は命令を拒否した（{rf.Reason}）");
                }
                else if (ev is OfficerRequestedRetreatEvent rr)
                {
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] ⚠ {rr.OfficerName}は撤退を進言した（兵力:{rr.Soldiers}）");
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
                else if (ev is BattleCore.Events.DecisionExplanationEvent de)
                {
                    // _lastDecisionsキャッシュ更新（部隊 IDが必要なので武将IDから引く）
                    var deArmy = world.Armies.FirstOrDefault(a =>
                        a.OfficerId.HasValue && a.OfficerId.Value == de.OfficerId);
                    if (deArmy != null)
                        _lastDecisions[deArmy.Id] = (de.Summary, de.Factors);

                    var factors = string.Join(" / ", de.Factors.Where(f => !string.IsNullOrEmpty(f)));
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] 🧠 {de.OfficerName}「{de.Summary}」 {factors}");
                }
                else if (ev is DiplomacyEvent dip)
                {
                    var icon = dip.Type switch
                    {
                        BattleCore.Events.DiplomacyEventType.CeasefireAccepted  => "⚔️停戦",
                        BattleCore.Events.DiplomacyEventType.CeasefireExpired   => "⚠️停戦終了",
                        BattleCore.Events.DiplomacyEventType.ReinforcementSent  => "🚩援軍",
                        BattleCore.Events.DiplomacyEventType.AllianceBetrayed   => "🗡️裏切り",
                        _                                                        => "📜外交",
                    };
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] {icon} {dip.ClanName}→{dip.TargetName} {dip.Detail}");
                }
                else if (ev is ScenarioEvent se)
                {
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] {se.Message}");
                }
                else if (ev is MoraleEvent me)
                {
                    var sign = me.Delta >= 0 ? "+" : "";
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] 💢 {me.OfficerName} 士気{sign}{me.Delta}→{me.NewMorale}（{me.Reason}）");
                }
                else if (ev is SiegeEvent sge)
                {
                    var typeText = sge.Type switch
                    {
                        BattleCore.Events.SiegeEventType.SiegeStarted => "包囲開始",
                        BattleCore.Events.SiegeEventType.SiegeLifted  => "包囲解除",
                        _                                              => "降伏",
                    };
                    var clan = world.Clans.FirstOrDefault(c => c.Id == sge.OwnerClanId);
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] 🏯 「{sge.CastleName}」{typeText}（{clan?.Name ?? "?"}）");
                }
                else if (ev is IntelEvent ie)
                {
                    lstEvents.Items.Insert(0,
                        $"[Tick{t.Tick}] 🕵 {ie.SpyClanName}が{ie.TargetClanName}の情報入手: {ie.Info}");
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
            UpdateDebugPanel();
        }

        // -------------------------------------------------------
        // デバッグコンソール更新（UpdateUI毎に呼ばれる）
        // -------------------------------------------------------
        private void UpdateDebugPanel()
        {
            // DecisionExplanationEventからキャッシュ更新は不要（EventQueueは既にUpdateUIで消費済み）
            // _lastDecisionsはClanDecisionSystemのイベントをUpdateUIで受け取り更新する

            if (_selectedArmyId == null)
            {
                rtbDebug.Clear();
                AppendDebug("=== BattleCore Debug ===", Color.FromArgb(180, 180, 255));
                AppendDebug($"Turn:{engine.Context.Time.Tick}  Phase:{engine.Context.CurrentPhase}", Color.Gray);
                AppendDebug("");
                AppendDebug("部隊を選択してください", Color.Gray);
                return;
            }

            var lines = BattleCore.Debug.DebugPanelBuilder.Build(
                _selectedArmyId.Value, world, engine.Context, _lastDecisions);

            rtbDebug.Clear();
            foreach (var line in lines)
            {
                var color = line.Color switch
                {
                    BattleCore.Debug.DebugPanelBuilder.DebugColor.Header => Color.FromArgb(180, 180, 255),
                    BattleCore.Debug.DebugPanelBuilder.DebugColor.Info   => Color.FromArgb(100, 200, 255),
                    BattleCore.Debug.DebugPanelBuilder.DebugColor.Good   => Color.FromArgb(100, 255, 150),
                    BattleCore.Debug.DebugPanelBuilder.DebugColor.Warn   => Color.FromArgb(255, 180,  60),
                    BattleCore.Debug.DebugPanelBuilder.DebugColor.Dim    => Color.FromArgb(140, 140, 140),
                    BattleCore.Debug.DebugPanelBuilder.DebugColor.Path   => Color.FromArgb(255, 220, 100),
                    BattleCore.Debug.DebugPanelBuilder.DebugColor.AI     => Color.FromArgb(200, 160, 255),
                    _                                                     => Color.White,
                };
                AppendDebug(line.Text, color);
            }
        }

        private void AppendDebug(string text, Color? color = null)
        {
            rtbDebug.SelectionStart  = rtbDebug.TextLength;
            rtbDebug.SelectionLength = 0;
            rtbDebug.SelectionColor  = color ?? Color.White;
            rtbDebug.AppendText(text + "\n");
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

                    // 駒の円（プレイヤー勢力は太枠でハイライト）
                    g.FillEllipse(new SolidBrush(color),
                        center.X - 14, center.Y - 14, 28, 28);
                    bool isPlayer   = army.ClanId == _playerClanId && _playerClanId != 0;
                    bool isSelected = army.Id == _selectedArmyId;
                    var  rimPen     = isSelected ? new Pen(Color.Yellow, 3f)
                                    : isPlayer   ? new Pen(Color.White,  2f)
                                    :              new Pen(Color.FromArgb(160, 160, 160), 1f);
                    g.DrawEllipse(rimPen, center.X - 14, center.Y - 14, 28, 28);
                    // 選択中は外側にリング
                    if (isSelected)
                        g.DrawEllipse(new Pen(Color.Yellow, 1.5f), center.X - 18, center.Y - 18, 36, 36);

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

            // デバッグレイヤー
            if (_overlay.Path)     DrawPathLayer(g);
            if (_overlay.Vision)   DrawVisionLayer(g);
            if (_overlay.FogOfWar) DrawFogLayer(g);
        }

        // -------------------------------------------------------
        // Pathレイヤー（選択部隊のA*経路 + コスト）
        // -------------------------------------------------------
        private void DrawPathLayer(Graphics g)
        {
            if (_selectedArmyId == null) return;
            var army = world.GetArmyById(_selectedArmyId.Value);
            if (army?.DestinationHexId == null || army.DestinationHexId.Value == army.CurrentHexId) return;

            var result = new BattleCore.Navigation.HexPathFinder()
                .FindPathWithCost(world.Map, army.CurrentHexId, army.DestinationHexId.Value);
            if (result.HexIds.Count < 2) return;

            using var pathPen = new Pen(Color.FromArgb(220, 255, 220, 60), 3f);
            pathPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            for (int i = 0; i < result.HexIds.Count - 1; i++)
            {
                var fh = world.Map.GetHexById(result.HexIds[i]);
                var th = world.Map.GetHexById(result.HexIds[i + 1]);
                if (fh == null || th == null) continue;
                g.DrawLine(pathPen, HexToPixel(fh.X, fh.Y), HexToPixel(th.X, th.Y));
            }
            // 各Hexにコスト表示
            for (int i = 1; i < result.HexIds.Count; i++)
            {
                var hex = world.Map.GetHexById(result.HexIds[i]);
                if (hex == null) continue;
                var c = HexToPixel(hex.X, hex.Y);
                g.DrawString($"({result.StepCosts[i]})",
                    new Font("Arial", 7f, FontStyle.Bold), Brushes.Yellow, c.X - 8, c.Y - 20);
            }
            // Total Cost
            var sh = world.Map.GetHexById(result.HexIds[0]);
            if (sh != null)
            {
                var sc = HexToPixel(sh.X, sh.Y);
                g.DrawString($"Total:{result.TotalCost}",
                    new Font("Arial", 7f, FontStyle.Bold), Brushes.Yellow, sc.X - 16, sc.Y - 28);
            }
        }

        // -------------------------------------------------------
        // Visionレイヤー（選択部隊の可視範囲ハイライト）
        // -------------------------------------------------------
        private void DrawVisionLayer(Graphics g)
        {
            if (_selectedArmyId == null) return;
            var army = world.GetArmyById(_selectedArmyId.Value);
            if (army == null) return;

            var visible = world.Visions.TryGetValue(army.Id, out var vd)
                ? vd.VisibleHexes : new HashSet<int>();
            using var brush = new SolidBrush(Color.FromArgb(40, 100, 255, 100));
            foreach (var hexId in visible)
            {
                var hex = world.Map.GetHexById(hexId);
                if (hex == null) continue;
                g.FillPolygon(brush, HexCorners(HexToPixel(hex.X, hex.Y)));
            }
        }

        // -------------------------------------------------------
        // FogOfWarレイヤー（非可視エリアを暗転）
        // -------------------------------------------------------
        private void DrawFogLayer(Graphics g)
        {
            if (_selectedArmyId == null) return;
            var army = world.GetArmyById(_selectedArmyId.Value);
            if (army == null) return;

            var visible = world.Visions.TryGetValue(army.Id, out var vd)
                ? vd.VisibleHexes : new HashSet<int>();
            using var fog = new SolidBrush(Color.FromArgb(160, 10, 10, 20));
            foreach (var hex in world.Map.Hexes)
            {
                if (visible.Contains(hex.Id)) continue;
                g.FillPolygon(fog, HexCorners(HexToPixel(hex.X, hex.Y)));
            }
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

        private static string PersonalityName(BattleCore.Entities.OfficerPersonality p) => p switch
        {
            BattleCore.Entities.OfficerPersonality.Brave       => "勇猛",
            BattleCore.Entities.OfficerPersonality.Cautious    => "慎重",
            BattleCore.Entities.OfficerPersonality.Ambitious   => "野心的",
            BattleCore.Entities.OfficerPersonality.Loyal       => "忠義",
            BattleCore.Entities.OfficerPersonality.Opportunist => "日和見",
            _ => ""
        };

        // -------------------------------------------------------
        // イベントログ 色分け描画
        // -------------------------------------------------------
        private void lstEvents_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstEvents.Items.Count) return;
            var text = lstEvents.Items[e.Index]?.ToString() ?? "";

            var fg = text switch
            {
                var s when s.Contains("包囲") || s.Contains("降伏")        => Color.FromArgb(255, 140,  40), // 包囲=橙
                var s when s.Contains("🕵")                        => Color.FromArgb(160, 255, 220), // 謀報=水緑
                var s when s.Contains("💢")                        => Color.FromArgb(255, 255, 120), // 士気=黄
                var s when s.Contains("勝") || s.Contains("敗")   => Color.FromArgb(255, 120, 120), // 戦闘=赤
                var s when s.Contains("占領")                    => Color.FromArgb(255, 200,  80), // 城占領=黄
                var s when s.Contains("拒否") || s.Contains("進言") => Color.FromArgb(255, 220,  60), // 武将=黄
                var s when s.Contains("離反") || s.Contains("仕官") => Color.FromArgb(255, 160,  40), // 離反=橙
                var s when s.Contains("🧠")                        => Color.FromArgb(180, 140, 255), // AI判断=紫
                var s when s.Contains("「") || s.Contains("『")        => Color.FromArgb(100, 220, 255), // シナリオ=水色
                var s when s.Contains("到着")                    => Color.FromArgb(160, 220, 160), // 移動=緑
                var s when s.Contains("補充")                    => Color.FromArgb(180, 255, 180), // 補給=淡緑
                var s when s.Contains("セーブ") || s.Contains("ロード") => Color.FromArgb(180, 180, 255), // 保存=紫
                _                                                  => Color.White,
            };

            e.DrawBackground();
            using var brush = new SolidBrush(fg);
            e.Graphics.DrawString(text, e.Font ?? lstEvents.Font, brush,
                e.Bounds.X + 2, e.Bounds.Y + 1);
        }
    }
}
