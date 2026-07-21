using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 移動システム。Army の DestinationHexId を見て1Hexずつ移動させる。
    /// 地形チェック（Mountain 不可）・AP消費・MoveCooldown を処理する。
    /// 目的地到着時に MovementEvent を EventQueue に積む。
    /// </summary>
    public class MovementSystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            foreach (var army in context.World.Armies)
            {
                if (army.Soldiers <= 0)
                {
                    army.ClearDestination();
                    continue;
                }

                if (army.ActionPoints <= 0)
                    continue;

                if (army.MoveCooldown > 0)
                {
                    army.MoveCooldown--;
                    continue;
                }

                if (army.DestinationHexId == null)
                    continue;

                var next = GetNextStep(context, army);
                if (next == null) continue;

                if (next.Terrain == TerrainType.Mountain)
                    continue;

                army.MoveTo(next.Id);
                army.ActionPoints--;

                if (next.Terrain == TerrainType.Forest)
                    army.MoveCooldown = context.World.Weather == Weather.Rain ? 2 : 1;

                // 目的地到着
                if (army.CurrentHexId == army.DestinationHexId)
                {
                    army.ClearDestination();

                    var officer = army.OfficerId.HasValue
                        ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value)
                        : null;
                    var name = officer?.Name ?? $"軍{army.Id}";
                    context.EventQueue.Enqueue(new MovementEvent(army.Id, name, army.CurrentHexId));
                }
            }
        }

        private Hex? GetNextStep(SimulationContext context, Entities.Army army)
        {
            var neighbors = context.World.Map.GetNeighbors(army.CurrentHexId);

            var direct = neighbors.FirstOrDefault(x => x.Id == army.DestinationHexId);
            if (direct != null) return direct;

            var destination = context.World.Map.GetHexById(army.DestinationHexId!.Value);
            if (destination == null) return null;

            return neighbors
                .Where(x => x.Terrain != TerrainType.Mountain)
                .OrderBy(x => HexDistance.Calculate(x, destination))
                .FirstOrDefault();
        }
    }
}
