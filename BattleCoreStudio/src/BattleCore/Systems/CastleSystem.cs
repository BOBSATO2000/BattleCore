using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 城システム。毎Tick以下を処理する：
    ///   1. 城のHexに敵軍のみいる場合 → 占領（OwnerClanId更新）
    ///   2. 城のHexに占領勢力の軍がいる場合 → ReinforcementPerTick 分補充
    /// </summary>
    public class CastleSystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            var world = context.World;

            foreach (var castle in world.Castles)
            {
                var armiesOnHex = world.Armies
                    .Where(a => a.Soldiers > 0 && a.CurrentHexId == castle.HexId)
                    .ToList();

                if (!armiesOnHex.Any()) continue;

                var clansOnHex = armiesOnHex.Select(a => a.ClanId).Distinct().ToList();

                // 複数勢力が同Hexにいる場合は戦闘中 → 占領処理スキップ
                if (clansOnHex.Count > 1) continue;

                var occupyingClan = clansOnHex[0];

                // 占領：城の所有者が変わった場合
                if (castle.OwnerClanId != occupyingClan)
                {
                    castle.OwnerClanId = occupyingClan;
                    context.EventQueue.Enqueue(new Events.CastleCapturedEvent(castle.Id, castle.Name, occupyingClan));
                }

                // 補充：占領勢力の軍に兵力を補充
                foreach (var army in armiesOnHex.Where(a => a.ClanId == occupyingClan))
                    army.Reinforce(castle.ReinforcementPerTick);
            }
        }
    }
}
