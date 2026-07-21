using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Vision;
using System.Collections.Generic;

namespace BattleCore.Systems
{
    /// <summary>
    /// 索敵システム。各 Army の視界範囲内にある Hex を計算し WorldState.Visions を更新する。
    /// AI は Visions を参照することで「見えている敵」だけを認識できる（霧の戦争の基盤）。
    /// 毎Step実行され、DecisionSystem より前に登録することを推奨する。
    /// </summary>
    public class VisionSystem : ISimulationSystem
    {
        /// <summary>視界範囲（Hex数）。この距離以内の Hex が見える。</summary>
        private const int VisionRange = 2;

        public void Update(SimulationContext context)
        {
            foreach (var army in context.World.Armies)
            {
                var vision = new VisionData(army.Id);

                foreach (var hexId in FindVisibleHexes(context, army.CurrentHexId))
                    vision.VisibleHexes.Add(hexId);

                context.World.Visions[army.Id] = vision;
            }
        }

        /// <summary>
        /// BFS で VisionRange 以内の全 Hex を探索して返す。
        /// Mountain は視界を遮らない（将来的に遮蔽ルールを追加できる）。
        /// </summary>
        private IEnumerable<int> FindVisibleHexes(SimulationContext context, int startHexId)
        {
            var visited = new HashSet<int> { startHexId };
            var queue = new Queue<(int hexId, int distance)>();
            queue.Enqueue((startHexId, 0));

            while (queue.Count > 0)
            {
                var (current, distance) = queue.Dequeue();
                yield return current;

                if (distance >= VisionRange)
                    continue;

                foreach (var neighbor in context.World.Map.GetNeighbors(current))
                {
                    if (visited.Add(neighbor.Id))
                        queue.Enqueue((neighbor.Id, distance + 1));
                }
            }
        }
    }
}
