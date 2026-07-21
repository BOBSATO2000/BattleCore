namespace BattleCore.Events
{
    /// <summary>
    /// AI判断の説明イベント。ClanDecisionSystem が発火し、UIがデバッグ表示に使用する。
    /// </summary>
    public class DecisionExplanationEvent : IGameEvent
    {
        public int    OfficerId   { get; init; }
        public string OfficerName { get; init; } = "";
        public string Summary     { get; init; } = "";
        public IReadOnlyList<string> Factors { get; init; } = [];
    }
}
