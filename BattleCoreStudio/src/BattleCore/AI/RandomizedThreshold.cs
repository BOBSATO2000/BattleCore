namespace BattleCore.AI
{
    /// <summary>
    /// 閾値に乱数幅を持たせる構造体。
    /// 毎回 Roll() を呼ぶたびに Center ± Spread の範囲でブレた値を返す。
    /// これにより「忠誠21なら絶対従う」のような確定的な判定を防ぐ。
    /// </summary>
    public readonly struct RandomizedThreshold
    {
        /// <summary>閾値の中心値。ai_params.json で設定する基準値。</summary>
        public int Center { get; init; }

        /// <summary>乱数幅。Center ± Spread の範囲でブレる。</summary>
        public int Spread { get; init; }

        /// <summary>
        /// Center ± Spread の範囲でランダムな閾値を返す。
        /// 毎回呼ぶたびに異なる値になるため、判定結果が確定しない。
        /// </summary>
        public int Roll(Random rng) => Center + rng.Next(-Spread, Spread + 1);

        /// <summary>
        /// int から暗黙変換する。Spread=0 の固定閾値として扱う。
        /// 既存の int コンストラクタとの互換性維持に使用する。
        /// </summary>
        public static implicit operator RandomizedThreshold(int value)
            => new() { Center = value, Spread = 0 };
    }
}
