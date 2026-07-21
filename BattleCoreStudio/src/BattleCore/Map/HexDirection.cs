using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCore.Map
{
    /// <summary>
    /// Hexマップ上の6方向。GameMap.GetNeighbors() で隣接Hex探索に使用する。
    /// オフセット座標系（偶数列基準）を採用している。
    /// </summary>
    public enum HexDirection
    {
        /// <summary>東（X+1）。</summary>
        East,
        /// <summary>西（X-1）。</summary>
        West,
        /// <summary>北東（X+1, Y-1）。</summary>
        NorthEast,
        /// <summary>北西（X-1, Y-1）。</summary>
        NorthWest,
        /// <summary>南東（X+1, Y+1）。</summary>
        SouthEast,
        /// <summary>南西（X-1, Y+1）。</summary>
        SouthWest
    }
}
