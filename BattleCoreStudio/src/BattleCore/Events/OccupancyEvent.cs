namespace BattleCore.Events
{
    public enum OccupancyEventType
    {
        /// <summary>移動先が満員で移動ブロックされた。</summary>
        Blocked,
        /// <summary>移動先に敵がいて戦闘が発生した。</summary>
        Combat,
    }

    /// <summary>占有ルールによる移動ブロック・戦闘トリガーイベント。</summary>
    public class OccupancyEvent : IGameEvent
    {
        public int              ArmyId    { get; }
        public string           ArmyName  { get; }
        public int              TargetHex { get; }
        public OccupancyEventType Type    { get; }

        public OccupancyEvent(int armyId, string armyName, int targetHex, OccupancyEventType type)
        {
            ArmyId    = armyId;
            ArmyName  = armyName;
            TargetHex = targetHex;
            Type      = type;
        }

        public string ToLogLine() => Type switch
        {
            OccupancyEventType.Blocked => $"{ArmyName}の進軍がHex{TargetHex}で阻まれた",
            OccupancyEventType.Combat  => $"{ArmyName}がHex{TargetHex}へ突入",
            _ => ""
        };
    }
}
