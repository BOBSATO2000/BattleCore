using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Navigation;
using BattleCore.Player;
using BattleCore.Simulation;
using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// AICommander / PlayerCommander を統一処理するSystem。
    ///
    /// フロー：
    ///   ICommander.GenerateIntents()  ← 「何をしたいか」だけ
    ///       ↓ Priority 比較（AI緊急割り込みなど）
    ///   IntentToCommand()             ← 「どう実行するか」をここで決定
    ///       ↓
    ///   OfficerDecision               ← 忠誠・性格フィルタ
    ///       ↓
    ///   CommandQueue
    ///
    /// Priority ルール：
    ///   100 = AI緊急割り込み（Food=0, Morale≤10）
    ///    50 = AI通常行動
    ///    10 = プレイヤー命令（デフォルト）
    ///     0 = 待機
    /// </summary>
    public sealed class CommanderSystem : ISimulationSystem
    {
        private readonly Dictionary<int, ICommander> _commanders = new();
        private readonly OfficerDecision             _officerDecision;
        private readonly IPathFinder                 _pathFinder = new HexPathFinder();

        /// <summary>PlayerCommander が登録されているか。SimulationEngine が参照する。</summary>
        public bool HasPlayerCommander { get; private set; } = false;

        public CommanderSystem(OfficerDecision? officerDecision = null)
        {
            _officerDecision = officerDecision ?? new OfficerDecision();
        }

        /// <summary>勢力に Commander を登録する。</summary>
        public void Register(ICommander commander)
        {
            _commanders[commander.ClanId] = commander;
            if (commander is PlayerCommander)
                HasPlayerCommander = true;
        }

        public void Update(SimulationContext context)
        {
            foreach (var clan in context.World.Clans)
            {
                if (!_commanders.TryGetValue(clan.Id, out var commander)) continue;

                var intents = commander.GenerateIntents(clan, context.World).ToList();

                // AI緊急割り込み（Priority=100）を追加
                var emergency = BuildEmergencyIntents(clan, context.World);

                // 部隊ごとに最高 Priority の意図を選ぶ
                var resolved = intents.Concat(emergency)
                    .GroupBy(i => i.ArmyId)
                    .Select(g => g.OrderByDescending(i => i.Priority).First())
                    .ToList();

                if (resolved.Count == 0) continue;

                // Intent → ICommand に変換
                var commands = resolved
                    .Select(i => IntentToCommand(i, context.World))
                    .Where(c => c != null)
                    .Cast<ICommand>()
                    .ToList();

                // OfficerDecision を通す（忠誠・性格フィルタ）
                foreach (var result in _officerDecision.Evaluate(commands, clan, context.World))
                {
                    if (result.Accepted && result.Command != null)
                        context.CommandQueue.Enqueue(result.Command);
                    if (result.Event != null)
                        context.EventQueue.Enqueue(result.Event);

                    if (result.Explanation != null && result.Command is MoveArmyCommand move)
                    {
                        var army    = context.World.GetArmyById(move.ArmyId);
                        var officer = army?.OfficerId.HasValue == true
                            ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId!.Value)
                            : null;
                        if (officer != null)
                            context.EventQueue.Enqueue(new BattleCore.Events.DecisionExplanationEvent
                            {
                                OfficerId   = officer.Id,
                                OfficerName = officer.Name,
                                Summary     = result.Explanation.Summary,
                                Factors     = result.Explanation.Factors,
                            });
                    }
                }
            }
        }

        /// <summary>
        /// Intent（何をしたいか）→ ICommand（どう実行するか）に変換する。
        /// ゲームルール（経路探索・最寄り城など）はここで解決する。
        /// </summary>
        private ICommand? IntentToCommand(Intent intent, WorldState world)
        {
            var army = world.GetArmyById(intent.ArmyId);
            if (army == null || army.Soldiers == 0) return null;

            return intent.Type switch
            {
                IntentType.MoveTo when intent.TargetHexId.HasValue
                    => new MoveArmyCommand(army.Id, intent.TargetHexId.Value, DecisionReason.Advance),

                IntentType.Attack
                    => BuildAttackCommand(army, world),

                IntentType.Defend
                    => new DefendOrder(army.Id),

                IntentType.Retreat
                    => new RetreatOrder(army.Id),

                IntentType.Siege when intent.TargetCastleId.HasValue
                    => new SiegeOrder(army.Id, intent.TargetCastleId.Value),

                IntentType.Scout
                    => new ScoutOrder(army.Id),

                IntentType.Supply
                    => new SupplyOrder(army.Id),

                IntentType.Fortify
                    => new FortifyOrder(army.Id),

                IntentType.Wait
                    => new WaitOrder(army.Id),

                _ => null,
            };
        }

        private ICommand? BuildAttackCommand(Army army, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId);
            if (currentHex == null) return null;

            var target = world.Castles
                .Where(c => c.OwnerClanId != army.ClanId)
                .Select(c => new { c.HexId, Hex = world.Map.GetHexById(c.HexId) })
                .Where(x => x.Hex != null)
                .OrderBy(x => HexDistance.Calculate(currentHex, x.Hex!))
                .FirstOrDefault();

            if (target == null) return null;
            var path = _pathFinder.FindPath(world.Map, army.CurrentHexId, target.HexId);
            return path.Count > 1
                ? new MoveArmyCommand(army.Id, path[1], DecisionReason.TargetCastle)
                : null;
        }

        /// <summary>
        /// 緊急状態の部隊に対してAIが自動割り込む意図を生成する。
        /// Food=0 → Supply(Priority=100)
        /// Morale≤10 → Retreat(Priority=100)
        /// </summary>
        private static IEnumerable<Intent> BuildEmergencyIntents(Clan clan, WorldState world)
        {
            foreach (var army in world.Armies.Where(a => a.ClanId == clan.Id && a.Soldiers > 0))
            {
                if (army.Food <= 0)
                    yield return new Intent(army.Id, IntentType.Supply,  priority: 100);
                else if (army.Morale <= 10)
                    yield return new Intent(army.Id, IntentType.Retreat, priority: 100);
            }
        }
    }
}
