namespace BattleCore.Entities
{
    /// <summary>
    /// 武将の性格。AI意思決定の基盤となる。
    /// 同じ命令でも性格によって従い方・変更の仕方が異なる。
    /// </summary>
    public enum OfficerPersonality
    {
        /// <summary>勇猛。攻撃命令を好み、撤退進言をしない。</summary>
        Brave,

        /// <summary>慎重。兵力不足時に撤退を進言しやすい。</summary>
        Cautious,

        /// <summary>野心的。忠誠が低いと独断行動を取りやすい。</summary>
        Ambitious,

        /// <summary>忠義。忠誠が低くても命令に従いやすい。</summary>
        Loyal,

        /// <summary>日和見。勝ち馬に乗る。勢力が劣勢だと離反しやすい。</summary>
        Opportunist,
    }
}
