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
            _ => 1,
        };

        /// <summary>
        /// A* で startHexId から targetHexId への最小コスト経路を返す。
        /// 戻り値は [startHexId, ..., targetHexId] の順のリスト。
        /// 経路が見つからない場合は空リストを返す。
        /// </summary>
        public List<int> FindPath(GameMap map, int startHexId, int targetHexId)
        {
            var target = map.GetHexById(targetHexId);
            if (target == null) return new List<int>();

            // g: 開始からのコスト, f: g + ヒューリスティック
            var gCost = new Dictionary<int, int> { [startHexId] = 0 };
            var previous = new Dictionary<int, int?> { [startHexId] = null };

            // 優先度キュー（f値昇順）: (f, hexId)
            var open = new SortedSet<(int f, int id)>(Comparer<(int f, int id)>.Create(
                (a, b) => a.f != b.f ? a.f.CompareTo(b.f) : a.id.CompareTo(b.id)));
            open.Add((Heuristic(map.GetHexById(startHexId)!, target), startHexId));

            while (open.Count > 0)
            {
                var (_, current) = open.Min;
                open.Remove(open.Min);

                if (current == targetHexId)
                    return BuildPath(previous, targetHexId);

                foreach (var neighbor in map.GetNeighbors(current))
                {
                    // Mountain は目的地でない限り通過不可
                    if (neighbor.Terrain == TerrainType.Mountain && neighbor.Id != targetHexId)
                        continue;

                    int newG = gCost[current] + TerrainCost(neighbor.Terrain);

                    if (gCost.TryGetValue(neighbor.Id, out int existingG) && newG >= existingG)
                        continue;

                    gCost[neighbor.Id] = newG;
                    previous[neighbor.Id] = current;
                    int f = newG + Heuristic(neighbor, target);
                    open.Add((f, neighbor.Id));
                }
            }

            return new List<int>();
        }

        /// <summary>ヘックス距離をヒューリスティックとして使用する。</summary>
        private static int Heuristic(Hex a, Hex b) => HexDistance.Calculate(a, b);

        private static List<int> BuildPath(Dictionary<int, int?> previous, int target)
        {
            var path = new List<int>();
            if (!previous.ContainsKey(target)) return path;

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
