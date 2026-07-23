using BattleCore.Commands;

namespace BattleCore.AI
{
    /// <summary>
    /// AI判断の説明。デバッグUI・ログ・将来のプレイヤー表示に共通利用する。
    /// </summary>
    public sealed class DecisionExplanation
    {
        /// <summary>判断の理由分類。</summary>
        public DecisionReason        Reason  { get; init; }

        /// <summary>判断の要約文。UI表示・ログに使用する。</summary>
        public string                Summary { get; init; } = "";

        /// <summary>判断に影響した要因のリスト。デバッグパネルに表示する。</summary>
        public IReadOnlyList<string> Factors { get; init; } = [];

        public static DecisionExplanation Create(
            DecisionReason reason,
            string summary,
            params string[] factors)
            => new() { Reason = reason, Summary = summary, Factors = factors };
    }
}
