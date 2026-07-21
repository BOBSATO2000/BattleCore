namespace BattleCore.Events
{
    /// <summary>
    /// 軍が目的地に到着したイベント。
    /// MovementSystem が DestinationHexId に到達した際に発生する。
    /// </summary>
    public class MovementEvent : IGameEvent
    {
        /// <summary>到着した軍のID。</summary>
        public int ArmyId { get; }

        /// <summary>到着した軍の指揮官名。</summary>
        public string OfficerName { get; }

        /// <summary>到着先のHexID。</summary>
        public int HexId { get; }

        public MovementEvent(int armyId, string officerName, int hexId)
        {
            ArmyId      = armyId;
            OfficerName = officerName;
            HexId       = hexId;
        }
    }
}
