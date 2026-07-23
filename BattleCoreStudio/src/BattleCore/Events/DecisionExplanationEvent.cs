namespace BattleCore.Events
{
    /// <summary>
    /// AI判断の説明イベント。ClanDecisionSystem が発火し、UIがデバッグ表示に使用する。
    /// </summary>
    public class DecisionExplanationEvent : IGameEvent
    {
        /// <summary>
		/// 判断を行った武将のID。
		/// </summary>
        public int    OfficerId   { get; init; }

        /// <summary>
		/// 判断を行った武将の名前。
		/// </summary>
        public string OfficerName { get; init; } = "";

        /// <summary>
		/// 判断の要約文。UI表示・ログに使用する。
		/// </summary>
        public string Summary     { get; init; } = "";

        /// <summary>
		/// 判断に影響した要因のリスト。
		/// </summary>
        public IReadOnlyList<string> Factors { get; init; } = [];
    }
}
