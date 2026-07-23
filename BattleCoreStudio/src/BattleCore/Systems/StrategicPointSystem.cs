using BattleCore.Entities;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 戦略資源システム。毎Tick実行。
    /// Shrine: 同Hex自軍の士気+3/Tick。
    /// </summary>
    public class StrategicPointSystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            var world = context.World;

            foreach (var shrine in world.Structures.Where(s => s.Type == StructureType.Shrine))
            {
                foreach (var army in world.Armies.Where(a =>
                    a.Soldiers > 0 &&
                    a.CurrentHexId == shrine.HexId &&
                    a.ClanId == shrine.OwnerClanId))
                {
                    army.Morale = System.Math.Min(100, army.Morale + 3);
                }
            }
        }
    }
}
