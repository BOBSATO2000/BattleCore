using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Map
{
    /// <summary>
    /// ゲーム世界の地理ルールを管理するクラス。
    /// Hex の追加・検索・隣接取得を担当する。
    /// WorldState が保持し、MovementSystem / AI / PathFinder から参照される。
    /// </summary>
    public class GameMap
    {
        private readonly List<Hex> hexes = new();

        /// <summary>マップ上の全Hexの読み取り専用リスト。</summary>
        public IReadOnlyList<Hex> Hexes => hexes;

        /// <summary>Hexをマップに追加する。</summary>
        public void AddHex(Hex hex) => hexes.Add(hex);

        /// <summary>IDでHexを検索する。見つからない場合は null。</summary>
        public Hex? GetHexById(int id)
            => hexes.FirstOrDefault(h => h.Id == id);

        /// <summary>座標でHexを検索する。見つからない場合は null。</summary>
        public Hex? GetHex(int x, int y)
            => hexes.FirstOrDefault(h => h.X == x && h.Y == y);

        /// <summary>
        /// 指定HexIDに隣接する全Hexを返す。
        /// 6方向（East/West/NorthEast/NorthWest/SouthEast/SouthWest）を検索する。
        /// </summary>
        public List<Hex> GetNeighbors(int hexId)
        {
            var result = new List<Hex>();
            var hex = GetHexById(hexId);

            if (hex == null)
                return result;

            foreach (HexDirection direction in Enum.GetValues<HexDirection>())
            {
                var neighbor = GetNeighbor(hex, direction);
                if (neighbor != null)
                    result.Add(neighbor);
            }

            return result;
        }

        private Hex? GetNeighbor(Hex hex, HexDirection direction)
        {
            int x = hex.X;
            int y = hex.Y;

            switch (direction)
            {
                case HexDirection.East:      x += 1; break;
                case HexDirection.West:      x -= 1; break;
                case HexDirection.NorthEast: x += 1; y -= 1; break;
                case HexDirection.NorthWest: x -= 1; y -= 1; break;
                case HexDirection.SouthEast: x += 1; y += 1; break;
                case HexDirection.SouthWest: x -= 1; y += 1; break;
            }

            return GetHex(x, y);
        }
    }
}
