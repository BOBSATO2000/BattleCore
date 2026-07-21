namespace BattleCore.Events
{
    /// <summary>
    /// 兵力補充イベント。
    /// SupplySystem が一定量以上の補充を行った際に発生する。
    /// 毎Tickの微量補充はノイズになるため、閾値以上の場合のみ発生させる。
    /// </summary>
    public class SupplyEvent : IGameEvent
    {
        /// <summary>補充を受けた軍のID。</summary>
        public int ArmyId { get; }

        /// <summary>補充を受けた軍の指揮官名。</summary>
        public string OfficerName { get; }

        /// <summary>補充量。</summary>
        public int Amount { get; }

        /// <summary>補充後の兵力。</summary>
        public int NewSoldiers { get; }

        public SupplyEvent(int armyId, string officerName, int amount, int newSoldiers)
        {
            ArmyId      = armyId;
            OfficerName = officerName;
            Amount      = amount;
            NewSoldiers = newSoldiers;
        }
    }
}
