namespace BattleCore.Events
{
    /// <summary>
    /// 武将が命令を拒否したイベント。
    /// 忠誠が低い・性格が合わない場合に OfficerDecision が発生させる。
    /// </summary>
    public class OfficerRefusedOrderEvent : IGameEvent
    {
        public int    OfficerId   { get; }
        public string OfficerName { get; }
        public string Reason      { get; }

        public OfficerRefusedOrderEvent(int officerId, string officerName, string reason)
        {
            OfficerId   = officerId;
            OfficerName = officerName;
            Reason      = reason;
        }
    }

    /// <summary>
    /// 武将が撤退を進言したイベント。
    /// 慎重な性格・兵力不足の武将が命令を撤退に変更した際に発生する。
    /// </summary>
    public class OfficerRequestedRetreatEvent : IGameEvent
    {
        public int    OfficerId   { get; }
        public string OfficerName { get; }
        public int    Soldiers    { get; }

        public OfficerRequestedRetreatEvent(int officerId, string officerName, int soldiers)
        {
            OfficerId   = officerId;
            OfficerName = officerName;
            Soldiers    = soldiers;
        }
    }
}
