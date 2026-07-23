using BattleCore.Map;
using System.Collections.Generic;

namespace BattleCore.Navigation
{
    /// <summary>
    /// 地形コスト対応 A* による経路探索実装。
    /// Plain=1, Forest=2, Mountain=通過不可 のコストで最短コスト経路を返す。
    /// </summary>
    public class HexPathFinder : IPathFinder
    {
        private static int TerrainCost(TerrainType terrain) => terrain switch
        {
            TerrainType.Forest => 2,
            TerrainType.River  => 3,
            _ => 1,
        };

        /// <summary>
        /// A* で startHexId から targetHexId への最小コスト経路を返す。
        /// 戻り値は [startHexId, ..., targetHexId] の順のリスト。
        /// 経路が見つからない場合は空リストを返す。
        /// </summary>
        public List<int> FindPath(GameMap map, int startHexId, int targetHexId)
        {
            return FindPathWithCost(map, startHexId, targetHexId).HexIds.ToList();
        }

        /// <summary>
        /// A* でコスト付き経路を返す。
        /// </summary>
        public PathResult FindPathWithCost(GameMap map, int startHexId, int targetHexId)
        {
            var target = map.GetHexById(targetHexId);
            if (target == null) return PathResult.Empty;

            var gCost    = new Dictionary<int, int>  { [startHexId] = 0 };
            var previous = new Dictionary<int, int?> { [startHexId] = null };
            var stepCostMap = new Dictionary<int, int> { [startHexId] = 0 };
            var closed   = new HashSet<int>();

            var open = new SortedSet<(int f, int id)>(Comparer<(int f, int id)>.Create(
                (a, b) => a.f != b.f ? a.f.CompareTo(b.f) : a.id.CompareTo(b.id)));
            open.Add((Heuristic(map.GetHexById(startHexId)!, target), startHexId));

            while (open.Count > 0)
            {
                var (_, current) = open.Min;
                open.Remove(open.Min);

                if (closed.Contains(current)) continue;
                closed.Add(current);

                if (current == targetHexId)
                    return BuildPathResult(previous, stepCostMap, targetHexId);

                foreach (var neighbor in map.GetNeighbors(current))
                {
                    if (neighbor.Terrain == TerrainType.Mountain && neighbor.Id != targetHexId)
                        continue;
                    if (closed.Contains(neighbor.Id)) continue;

                    int step = TerrainCost(neighbor.Terrain);
                    int newG = gCost[current] + step;

                    if (gCost.TryGetValue(neighbor.Id, out int existingG) && newG >= existingG)
                        continue;

                    gCost[neighbor.Id]    = newG;
                    previous[neighbor.Id] = current;
                    stepCostMap[neighbor.Id] = step;
                    int f = newG + Heuristic(neighbor, target);
                    open.Add((f, neighbor.Id));
                }
            }

            return PathResult.Empty;
        }

        /// <summary>ヘックス距離をヒューリスティックとして使用する。</summary>
        private static int Heuristic(Hex a, Hex b) => HexDistance.Calculate(a, b);

        private static PathResult BuildPathResult(
            Dictionary<int, int?> previous,
            Dictionary<int, int>  stepCostMap,
            int target)
        {
            var hexIds    = new List<int>();
            var stepCosts = new List<int>();
            if (!previous.ContainsKey(target)) return PathResult.Empty;

            int? current = target;
            while (current != null)
            {
                hexIds.Add(current.Value);
                stepCosts.Add(stepCostMap.GetValueOrDefault(current.Value, 0));
                current = previous[current.Value];
            }
            hexIds.Reverse();
            stepCosts.Reverse();
            return new PathResult(hexIds, stepCosts);
        }
    }
}
