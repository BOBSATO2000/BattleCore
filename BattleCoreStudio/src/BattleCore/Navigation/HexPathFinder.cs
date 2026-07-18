using BattleCore.Map;
using System.Collections.Generic;

namespace BattleCore.Navigation
{
    /// <summary>
    /// BFS（幅優先探索）による経路探索実装。
    /// 地形コストを考慮しない最短ステップ数の経路を返す。
    /// 将来的には地形コスト対応の A* に置き換えることができる。
    /// </summary>
    public class HexPathFinder : IPathFinder
    {
        /// <summary>
        /// BFS で startHexId から targetHexId への最短経路を返す。
        /// 戻り値は [startHexId, ..., targetHexId] の順のリスト。
        /// </summary>
        public List<int> FindPath(GameMap map, int startHexId, int targetHexId)
        {
            var queue = new Queue<int>();
            var previous = new Dictionary<int, int?>();

            queue.Enqueue(startHexId);
            previous[startHexId] = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == targetHexId)
                    break;

                foreach (var next in map.GetNeighbors(current))
                {
                    if (previous.ContainsKey(next.Id))
                        continue;

                    // Mountain は通過不可
                    if (next.Terrain == BattleCore.Map.TerrainType.Mountain && next.Id != targetHexId)
                        continue;

                    previous[next.Id] = current;
                    queue.Enqueue(next.Id);
                }
            }

            return BuildPath(previous, targetHexId);
        }

        /// <summary>previous マップを逆順に辿って経路リストを構築する。</summary>
        private List<int> BuildPath(Dictionary<int, int?> previous, int target)
        {
            var path = new List<int>();

            if (!previous.ContainsKey(target))
                return path;

            int? current = target;

            while (current != null)
            {
                path.Add(current.Value);
                current = previous[current.Value];
            }

            path.Reverse();
            return path;
        }
    }
}
