using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCore.Simulation
{
    /// <summary>
    /// ゲーム内の季節。GameTime が毎4ステップで循環させる。
    /// 季節は忠誠変動（春ボーナス）・補給量（将来実装）に影響する。
    /// </summary>
    public enum Season
    {
        /// <summary>春。忠誠ボーナスあり。補給量増加（将来実装）。</summary>
        Spring,
        /// <summary>夏。標準。</summary>
        Summer,
        /// <summary>秋。標準。</summary>
        Autumn,
        /// <summary>冬。補給量減少（将来実装）。</summary>
        Winter
    }
}
