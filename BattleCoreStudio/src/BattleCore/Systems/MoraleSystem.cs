using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 士気システム。毎Tick実行。
    /// - 同Hexに敵がいる → -5
    /// - 同盟軍が隣接Hex → +3
    /// - 兵力が MaxSoldiers の50%未満 → -3
    /// - 士気が20以下になったらイベント発火
    /// - 騎馬が平地到着（MovementEvent）→ +5（MovementSystemから疎結合で受け取る）
    /// </summary>
    public class MoraleSystem : ISimulationSystem
    {
        private const int EnemyPenalty       = -5;
        private const int AllyBonus          =  3;
        private const int LowStrengthPenalty = -3;
        private const int EventThreshold     = 20;
        private const int CavalryChargeBonus =  5;

        public void Update(SimulationContext context)
        {
            var world = context.World;

            // MovementEvent から騎馬平地到着ボーナスを適用
            var cavalryArrivals = context.EventQueue
                .OfType<MovementEvent>()
                .Where(e => e.UnitType == UnitType.Cavalry && e.Terrain == TerrainType.Plain)
                .Select(e => e.ArmyId)
                .ToHashSet();

            foreach (var army in world.Armies)
            {
                if (army.Soldiers == 0) continue;

                int delta = 0;

                // 同Hexに敵がいる
                bool hasEnemy = world.Armies.Any(a =>
                    a.Id != army.Id &&
                    a.Soldiers > 0 &&
                    a.CurrentHexId == army.CurrentHexId &&
                    a.ClanId != army.ClanId &&
                    !world.AreAllied(army.ClanId, a.ClanId));
                if (hasEnemy) delta += EnemyPenalty;

                // 同盟軍が隣接Hexにいる
                var neighbors = world.Map.GetNeighbors(army.CurrentHexId).Select(h => h.Id).ToHashSet();
                bool hasAllyNearby = world.Armies.Any(a =>
                    a.Id != army.Id &&
                    a.Soldiers > 0 &&
                    neighbors.Contains(a.CurrentHexId) &&
                    (a.ClanId == army.ClanId || world.AreAllied(army.ClanId, a.ClanId)));
                if (hasAllyNearby) delta += AllyBonus;

                // 兵力が半分未満
                if (army.Soldiers < army.MaxSoldiers / 2) delta += LowStrengthPenalty;

                // 騎馬が平地に到着（MovementEvent 経由）
                if (cavalryArrivals.Contains(army.Id)) delta += CavalryChargeBonus;

                if (delta == 0) continue;

                int prev = army.Morale;
                army.Morale = System.Math.Clamp(army.Morale + delta, 0, 100);

                if (prev > EventThreshold && army.Morale <= EventThreshold)
                {
                    var officer = army.OfficerId.HasValue
                        ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value)
                        : null;
                    context.EventQueue.Enqueue(new MoraleEvent(
                        army.Id,
                        officer?.Name ?? $"軍{army.Id}",
                        army.Morale - prev,
                        army.Morale,
                        "士気低下"));
                }
            }
        }
    }
}
