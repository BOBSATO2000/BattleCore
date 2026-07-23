using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Entities;
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
    /// ClanDecisionSystem と PlayerDecisionSystem を置き換える。
    ///
    /// フロー：
    ///   ICommander.GenerateOrders()
    ///       ↓ Priority 比較（AI補給割り込みなど）
    ///   OfficerDecision（忠誠・性格フィルタ）
    ///       ↓
    ///   CommandQueue
    ///
    /// Priority ルール：
    ///   100 = AI緊急割り込み（Food=0, Morale=0 など）
    ///    50 = AI通常行動
    ///    10 = プレイヤー命令（デフォルト）
    ///     0 = 待機
    /// </summary>
    public sealed class CommanderSystem : ISimulationSystem
    {
        private readonly Dictionary<int, ICommander> _commanders = new();
        private readonly OfficerDecision             _officerDecision;
        private readonly IPathFinder                 _pathFinder = new HexPathFinder();

        public CommanderSystem(OfficerDecision? officerDecision = null)
        {
            _officerDecision = officerDecision ?? new OfficerDecision();
        }

        /// <summary>勢力に Commander を登録する。</summary>
        public void Register(ICommander commander) => _commanders[commander.ClanId] = commander;

        public void Update(SimulationContext context)
        {
            foreach (var clan in context.World.Clans)
            {
                if (!_commanders.TryGetValue(clan.Id, out var commander)) continue;

                var rawOrders = commander.GenerateOrders(clan, context.World).ToList();
                if (rawOrders.Count == 0) continue;

                // AI緊急割り込み：Food=0 / Morale=0 の部隊は補給・撤退を Priority=100 で挿入
                var emergencyOrders = BuildEmergencyOrders(clan, context.World);

                // 部隊ごとに最高 Priority の命令を選ぶ
                var allOrders = rawOrders.Concat(emergencyOrders)
                    .GroupBy(o => GetArmyId(o.Command))
                    .SelectMany<IGrouping<int?, CommanderOrder>, CommanderOrder>(g =>
                    {
                        if (g.Key == null) return g.ToList();
                        var best = g.OrderByDescending(o => o.Priority).First();
                        return new[] { best };
                    })
                    .ToList();

                // OfficerDecision を通す
                var commands = allOrders.Select(o => o.Command).ToList();
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
        /// 緊急状態の部隊に対して AI が自動割り込む命令を生成する。
        /// Food=0 → SupplyOrder(Priority=100)
        /// Morale≤10 → RetreatOrder(Priority=100)
        /// </summary>
        private static IEnumerable<CommanderOrder> BuildEmergencyOrders(Entities.Clan clan, WorldState world)
        {
            foreach (var army in world.Armies.Where(a => a.ClanId == clan.Id && a.Soldiers > 0))
            {
                if (army.Food <= 0)
                    yield return new CommanderOrder(new SupplyOrder(army.Id), priority: 100, OrderLifetime.OneShot);
                else if (army.Morale <= 10)
                    yield return new CommanderOrder(new RetreatOrder(army.Id), priority: 100, OrderLifetime.OneShot);
            }
        }

        private static int? GetArmyId(ICommand cmd) => cmd switch
        {
            MoveArmyCommand m => m.ArmyId,
            MoveOrder       m => m.ArmyId,
            DefendOrder     d => d.ArmyId,
            RetreatOrder    r => r.ArmyId,
            ScoutOrder      s => s.ArmyId,
            SupplyOrder     s => s.ArmyId,
            FortifyOrder    f => f.ArmyId,
            SiegeOrder      s => s.ArmyId,
            WaitOrder       w => w.ArmyId,
            _                 => null,
        };
    }
}
