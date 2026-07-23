using BattleCore.Map;

namespace BattleCore.Entities
{
    /// <summary>
    /// Facing の方向差と AP コストを計算するユーティリティ。
    /// Hex は6方向なので60°単位（0〜3ステップ）で表現する。
    ///
    /// コスト表：
    ///   0ステップ（0°）   → 0.0 AP
    ///   1ステップ（60°）  → 0.0 AP
    ///   2ステップ（120°） → 0.5 AP（2回で1消費）
    ///   3ステップ（180°） → 1.0 AP
    /// </summary>
    public static class FacingHelper
    {
        /// <summary>
        /// 現在の Facing から移動方向への最短ステップ数（0〜3）を返す。
        /// </summary>
        public static int StepDiff(FacingDirection current, HexDirection movingTo)
        {
            int diff = System.Math.Abs((int)current - (int)movingTo) % 6;
            return diff > 3 ? 6 - diff : diff;
        }

        /// <summary>
        /// ステップ数から AP コストを返す。
        /// 0〜1: 0.0 / 2: 0.5 / 3: 1.0
        /// </summary>
        public static double TurnCost(int steps) => steps switch
        {
            3 => 1.0,
            2 => 0.5,
            _ => 0.0,
        };
    }
}
