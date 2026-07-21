namespace BattleCore.AI
{
    /// <summary>
    /// 武将意思決定のパラメータ。ai_params.json から読み込む。
    /// コードを変更せずにAIバランスを調整できる。
    /// </summary>
    public sealed class AiParams
    {
        /// <summary>命令拒否する忠誠閾値（これ以下で拒否）。</summary>
        public int RefusalLoyaltyThreshold      { get; set; } = 20;

        /// <summary>慎重な武将が撤退進言する兵力閾値（これ以下で進言）。</summary>
        public int CautiousRetreatSoldiers      { get; set; } = 300;

        /// <summary>野心的な武将が独断行動する忠誠閾値（これ以下で独断）。</summary>
        public int IndependentActionLoyalty     { get; set; } = 35;

        /// <summary>主君への反感が命令変更を引き起こす閾値（これ以上で変更）。</summary>
        public int DissatisfiedDislikeThreshold { get; set; } = 60;

        public static AiParams Default => new();
    }
}
