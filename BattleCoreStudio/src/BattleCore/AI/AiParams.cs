namespace BattleCore.AI
{
    /// <summary>
    /// 武将意思決定のパラメータ。ai_params.json から読み込む。
    /// コードを変更せずにAIバランスを調整できる。
    /// 各閾値は RandomizedThreshold 型で Center（中心値）と Spread（乱数幅）を持つ。
    /// </summary>
    public sealed class AiParams
    {
        /// <summary>
        /// 命令拒否する忠誠閾値。
        /// Center=20, Spread=8 の場合、12〜28 の範囲でブレる。
        /// </summary>
        public RandomizedThreshold RefusalLoyaltyThreshold      { get; set; } = new() { Center = 20, Spread = 8 };

        /// <summary>
        /// 慎重な武将が撤退進言する兵力閾値。
        /// Center=300, Spread=80 の場合、220〜380 の範囲でブレる。
        /// </summary>
        public RandomizedThreshold CautiousRetreatSoldiers      { get; set; } = new() { Center = 300, Spread = 80 };

        /// <summary>
        /// 野心的な武将が独断行動する忠誠閾値。
        /// Center=35, Spread=10 の場合、25〜45 の範囲でブレる。
        /// </summary>
        public RandomizedThreshold IndependentActionLoyalty     { get; set; } = new() { Center = 35, Spread = 10 };

        /// <summary>
        /// 主君への反感が命令変更を引き起こす閾値。
        /// Center=60, Spread=15 の場合、45〜75 の範囲でブレる。
        /// </summary>
        public RandomizedThreshold DissatisfiedDislikeThreshold { get; set; } = new() { Center = 60, Spread = 15 };

        /// <summary>デフォルト値のインスタンスを返す。</summary>
        public static AiParams Default => new();

        /// <summary>戦術評価パラメータ。TacticalEvaluator が使用する。</summary>
        public TacticalParams Tactical { get; set; } = TacticalParams.Default;
    }
}
