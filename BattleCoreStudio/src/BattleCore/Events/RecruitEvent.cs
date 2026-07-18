namespace BattleCore.Events
{
    /// <summary>
    /// 武将の仕官イベント。
    /// RecruitmentSystem が無所属武将を勢力へ登用した際に発生する。
    /// </summary>
    public class RecruitEvent : IGameEvent
    {
        /// <summary>仕官した武将のID。</summary>
        public int OfficerId { get; }

        /// <summary>仕官先の勢力ID。</summary>
        public int ToClanId { get; }

        public RecruitEvent(int officerId, int toClanId)
        {
            OfficerId = officerId;
            ToClanId  = toClanId;
        }
    }
}
