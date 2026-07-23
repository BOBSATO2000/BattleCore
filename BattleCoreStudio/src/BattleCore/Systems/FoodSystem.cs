using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 食糧システム。毎Tick実行。
    /// - 毎Tick FoodConsumptionPerTick を消費
    /// - 城のあるHexにいる自軍は補充（ReinforcementPerTick/2）
    /// - Food=0 になると士気 -10、移動AP -1（最低0）
    /// </summary>
    public class FoodSystem : ISimulationSystem
    {
        private const int MoralePenaltyOnStarve = -10;

        public void Update(SimulationContext context)
        {
            var world = context.World;

            // 包囲中の城セットを事前に収集
            var siegedHexes = world.Castles
                .Where(c => c.SiegeTick > 0)
                .Select(c => c.HexId)
                .ToHashSet();

            foreach (var army in world.Armies)
            {
                if (army.Soldiers == 0) continue;

                // 城補充（包囲中は補充なし）
                var castle = world.Castles.FirstOrDefault(c =>
                    c.HexId == army.CurrentHexId && c.OwnerClanId == army.ClanId);
                if (castle != null && castle.SiegeTick == 0)
                {
                    army.Food = System.Math.Min(Army.MaxFood,
                        army.Food + castle.ReinforcementPerTick / 2);
                }

                // Camp 補給：同 Hex に同勢力の Camp があれば食粮+15
                bool hasCamp = world.Structures.Any(s =>
                    s.Type == BattleCore.Entities.StructureType.Camp &&
                    s.HexId == army.CurrentHexId &&
                    s.OwnerClanId == army.ClanId);
                if (hasCamp)
                    army.Food = System.Math.Min(Army.MaxFood, army.Food + 15);

                // 消費量決定
                // ① 守備側：包囲中は通常の2倍消費
                // ② 攻囲側：包囲参加中は追加消費
                // ③ 補給線切断：1.5倍消費（包囲と重複する場合は最大値を適用）
                bool isDefenderUnderSiege = castle != null && castle.SiegeTick > 0;
                bool isBesieger = world.Castles.Any(c =>
                    c.SiegeTick > 0 &&
                    c.OwnerClanId != army.ClanId &&
                    !world.AreAllied(c.OwnerClanId, army.ClanId) &&
                    world.Map.GetNeighbors(c.HexId).Any(h => h.Id == army.CurrentHexId));

                int consumption = Army.FoodConsumptionPerTick;
                if (isDefenderUnderSiege)      consumption *= 2;
                else if (isBesieger)            consumption += consumption / 2;
                else if (!army.IsSupplied)      consumption += consumption / 2; // 補給線切断

                // Well 補正：包囲中に同Hex同勢力の Well があれば消費-50%
                bool hasWell = world.Structures.Any(s =>
                    s.Type == StructureType.Well &&
                    s.HexId == army.CurrentHexId &&
                    s.OwnerClanId == army.ClanId);
                if (hasWell && isDefenderUnderSiege)
                    consumption = System.Math.Max(1, consumption / 2);

                // 消費
                int prev = army.Food;
                army.Food = System.Math.Max(0, army.Food - consumption);

                // 枯渇ペナルティ
                if (army.Food == 0 && prev > 0)
                {
                    army.Morale = System.Math.Max(0, army.Morale + MoralePenaltyOnStarve);
                    var officer = army.OfficerId.HasValue
                        ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value)
                        : null;
                    context.EventQueue.Enqueue(new MoraleEvent(
                        army.Id,
                        officer?.Name ?? $"軍{army.Id}",
                        MoralePenaltyOnStarve,
                        army.Morale,
                        "兵糧切れ"));
                }

                // 兵糧切れ中はAP-1
                if (army.Food == 0 && army.ActionPoints > 0)
                    army.ActionPoints = System.Math.Max(0, army.ActionPoints - 1);
            }
        }
    }
}
