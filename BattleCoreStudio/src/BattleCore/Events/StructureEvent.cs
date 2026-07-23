namespace BattleCore.Events
{
    /// <summary>構造物の建設・破壊イベント。</summary>
    public class StructureEvent : IGameEvent
    {
        public int           ArmyId        { get; }
        public string        ArmyName      { get; }
        public StructureEventType EventType { get; }
        public string        StructureName { get; }
        public int           HexId         { get; }

        public StructureEvent(int armyId, string armyName, StructureEventType eventType, string structureName, int hexId)
        {
            ArmyId        = armyId;
            ArmyName      = armyName;
            EventType     = eventType;
            StructureName = structureName;
            HexId         = hexId;
        }
    }

    public enum StructureEventType { Built, Destroyed }
}
