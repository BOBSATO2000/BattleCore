using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Map;
using BattleCore.Navigation;
using BattleCore.Simulation;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Player
{
    /// <summary>
    /// プレイヤー（大名）の命令を処理するSystem。
    /// ClanDecisionSystem の代わりに IsPlayerControlled な勢力に対して動作する。
    ///
    /// フロー：
    ///   PlayerOrder（方針/直接）
    ///       ↓ ToCommand() で ICommand に変換
    ///   OfficerDecision（忠誠・性格フィルタ）
    ///       ↓
    ///   CommandQueue
    /// </summary>
    public class PlayerDecisionSystem : ISimulationSystem
    {
        private readonly Queue<PlayerOrder>  _orders = new();
        private readonly OfficerDecision     _officerDecision;
        private readonly IPathFinder         _pathFinder = new HexPathFinder();

        public PlayerDecisionSystem(OfficerDecision? officerDecision = null)
        {
            _officerDecision = officerDecision ?? new OfficerDecision();
        }

        /// <summary>UIから呼ぶ。プレイヤーの命令をキューに積む。</summary>
        public void EnqueueOrder(PlayerOrder order) => _orders.Enqueue(order);

        /// <summary>未処理命令数（UI表示用）。</summary>
        public int PendingCount => _orders.Count;

        public void Update(SimulationContext context)
        {
            while (_orders.Count > 0)
            {
                var order = _orders.Dequeue();
                var army  = context.World.GetArmyById(order.ArmyId);
                if (army == null || army.Soldiers == 0) continue;

                var clan = context.World.Clans.FirstOrDefault(c => c.Id == army.ClanId);
                if (clan == null || !clan.IsPlayerControlled) continue;

                var cmd = ToCommand(order, context);
                if (cmd == null) continue;

                // OfficerDecision を通す（忠誠・性格フィルタ）
                foreach (var result in _officerDecision.Evaluate(new[] { cmd }, clan, context.World))
                {
                    if (result.Accepted && result.Command != null)
                        context.CommandQueue.Enqueue(result.Command);
                    if (result.Event != null)
                        context.EventQueue.Enqueue(result.Event);
                    if (result.Explanation != null && result.Command is MoveArmyCommand move)
                    {
                        var a = context.World.GetArmyById(move.ArmyId);
                        var o = a?.OfficerId.HasValue == true
                            ? context.World.Officers.FirstOrDefault(x => x.Id == a.OfficerId!.Value)
                            : null;
                        if (o != null)
                            context.EventQueue.Enqueue(new BattleCore.Events.DecisionExplanationEvent
                            {
                                OfficerId   = o.Id,
                                OfficerName = o.Name,
                                Summary     = result.Explanation.Summary,
                                Factors     = result.Explanation.Factors,
                            });
                    }
                }
            }
        }

        private ICommand? ToCommand(PlayerOrder order, SimulationContext context)
        {
            var army = context.World.GetArmyById(order.ArmyId)!;

            return order.OrderType switch
            {
                PlayerOrderType.DirectMove when order.TargetHexId.HasValue
                    => new MoveArmyCommand(army.Id, order.TargetHexId.Value, DecisionReason.Advance),

                PlayerOrderType.DirectWait
                    => new WaitOrder(army.Id),

                PlayerOrderType.Attack
                    => BuildAttackCommand(army, context),

                PlayerOrderType.Defend
                    => new DefendOrder(army.Id),

                PlayerOrderType.Retreat
                    => new RetreatOrder(army.Id),

                PlayerOrderType.Siege when order.TargetCastleId.HasValue
                    => new SiegeOrder(army.Id, order.TargetCastleId.Value),

                PlayerOrderType.Scout
                    => new ScoutOrder(army.Id),

                PlayerOrderType.Supply
                    => new SupplyOrder(army.Id),

                PlayerOrderType.Fortify
                    => new FortifyOrder(army.Id),

                _ => null,
            };
        }

        private ICommand? BuildAttackCommand(BattleCore.Entities.Army army, SimulationContext context)
        {
            var currentHex = context.World.Map.GetHexById(army.CurrentHexId);
            if (currentHex == null) return null;

            // 最寄りの敵城を目標にする
            var target = context.World.Castles
                .Where(c => c.OwnerClanId != army.ClanId)
                .Select(c => new { c.HexId, Hex = context.World.Map.GetHexById(c.HexId) })
                .Where(x => x.Hex != null)
                .OrderBy(x => HexDistance.Calculate(currentHex, x.Hex!))
                .FirstOrDefault();

            if (target == null) return null;
            var path = _pathFinder.FindPath(context.World.Map, army.CurrentHexId, target.HexId);
            if (path.Count < 2) return null;
            return new MoveArmyCommand(army.Id, path[1], DecisionReason.TargetCastle);
        }
    }
}
