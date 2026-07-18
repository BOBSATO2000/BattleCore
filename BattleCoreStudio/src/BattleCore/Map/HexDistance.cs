using System;

namespace BattleCore.Map
{
    /// <summary>
    /// 2つのHex間のマンハッタン距離を計算するユーティリティ。
    /// MovementSystem の経路選択・AI の敵距離判定に使用する。
    /// </summary>
    public class HexDistance
    {
        /// <summary>
        /// 2つのHex間の距離を返す。
        /// X差とY差の絶対値の和（マンハッタン距離）で計算する。
        /// </summary>
        public static int Calculate(Hex a, Hex b)
            => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}
