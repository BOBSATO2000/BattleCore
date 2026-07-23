using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// ZOC（Zone of Control）システム。毎Tick実行。
    /// 敵軍の隣接Hexにいる軍は InZoc=true になる。
    /// MovementSystem が InZoc=true の軍の移動コスト+1 を適用する。
    /// これにより「前線を突破するには戦うか回り込むしかない」状況が生まれる。
    /// </summary>
    public class ZocSystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            var world = context.World;

            // 全軍の InZoc をリセット
            foreach (var army in world.Armies)
                army.InZoc = false;

            foreach (var army in world.Armies)
            {
                if (army.Soldiers == 0) continue;

                var enemyNeighborHexes = world.Armies
                    .Where(e => e.Soldiers > 0 &&
                                e.ClanId != army.ClanId &&
                                !world.AreAllied(army.ClanId, e.ClanId))
                    .SelectMany(e => world.Map.GetNeighbors(e.CurrentHexId))
                    .Select(h => h.Id)
                    .ToHashSet();

                if (enemyNeighborHexes.Contains(army.CurrentHexId))
                    army.InZoc = true;
            }
        }
    }
}
