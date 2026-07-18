namespace BattleCore.Events
{
    /// <summary>
    /// 武将の離反イベント。
    /// LoyaltySystem が裏切り判定を行った際に発生し、EventQueue に積まれる。
    /// 
    /// 離反の結果：
    ///   - Officer が元の Clan から離脱する
    ///   - Officer が指揮する Army の ClanId が変わる（将来実装）
    ///   - 他の武将の Loyalty に影響を与える（連鎖離反の基盤）
    /// </summary>
    public class BetrayalEvent : IGameEvent
    {
        /// <summary>離反した武将のID。</summary>
        public int OfficerId { get; }

        /// <summary>離反元の勢力ID。</summary>
        public int FromClanId { get; }

        /// <summary>離反時の裏切りスコア（デバッグ・ログ用）。</summary>
        public int BetrayalScore { get; }

        public BetrayalEvent(int officerId, int fromClanId, int betrayalScore)
        {
            OfficerId     = officerId;
            FromClanId    = fromClanId;
            BetrayalScore = betrayalScore;
        }
    }
}
