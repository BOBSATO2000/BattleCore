using BattleCore.Map;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 移動システム。Army の DestinationHexId を見て1Hexずつ移動させる。
    /// 地形チェック（Mountain 不可）を行い、目的地到着時に DestinationHexId をクリアする。
    /// CommandExecutionSystem が OrderMove を設定した後に実行される。
    /// </summary>
    public class MovementSystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            foreach (var army in context.World.Armies)
            {
                // 兵力0の軍は移動しない
                if (army.Soldiers <= 0)
                {
                    army.ClearDestination();
                    continue;
                }

                if (army.DestinationHexId == null)
                    continue;

                var next = GetNextStep(context, army);

                if (next == null)
                    continue;

                // Mountain には移動不可
                if (next.Terrain == TerrainType.Mountain)
                    continue;

                army.MoveTo(next.Id);

                if (army.CurrentHexId == army.DestinationHexId)
                    army.ClearDestination();
            }
        }

        /// <summary>
        /// 目的地への次の1Hexを返す。
        /// 目的地が隣接していればそこへ、遠い場合は最も近い隣接Hexへ1歩進む。
        /// </summary>
        private Hex? GetNextStep(SimulationContext context, Entities.Army army)
        {
            var neighbors = context.World.Map.GetNeighbors(army.CurrentHexId);

            // 目的地が隣接していれば直接移動
            var direct = neighbors.FirstOrDefault(x => x.Id == army.DestinationHexId);
            if (direct != null)
                return direct;

            // 目的地が遠い場合は最も近い隣接Hexへ1歩進む
            var destination = context.World.Map.GetHexById(army.DestinationHexId!.Value);
            if (destination == null)
                return null;

            return neighbors
                .Where(x => x.Terrain != TerrainType.Mountain)
                .OrderBy(x => HexDistance.Calculate(x, destination))
                .FirstOrDefault();
        }
    }
}
