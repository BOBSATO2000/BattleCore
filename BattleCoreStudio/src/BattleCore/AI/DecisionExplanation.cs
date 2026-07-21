using BattleCore.Commands;

namespace BattleCore.AI
{
    /// <summary>
    /// AI判断の説明。デバッグUI・ログ・将来のプレイヤー表示に共通利用する。
    /// </summary>
    public sealed class DecisionExplanation
    {
        public DecisionReason        Reason  { get; init; }
        public string                Summary { get; init; } = "";
        public IReadOnlyList<string> Factors { get; init; } = [];

        public static DecisionExplanation Create(
            DecisionReason reason,
            string summary,
            params string[] factors)
            => new() { Reason = reason, Summary = summary, Factors = factors };
    }
}
