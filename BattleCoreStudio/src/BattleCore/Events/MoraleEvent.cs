namespace BattleCore.Events
{
    /// <summary>士気が大きく変化したときに発火するイベント。</summary>
    public class MoraleEvent : IGameEvent
    {
        public int    ArmyId      { get; }
        public string OfficerName { get; }
        public int    Delta       { get; }   // 正=上昇 負=低下
        public int    NewMorale   { get; }
        public string Reason      { get; }

        public MoraleEvent(int armyId, string officerName, int delta, int newMorale, string reason)
        {
            ArmyId      = armyId;
            OfficerName = officerName;
            Delta       = delta;
            NewMorale   = newMorale;
            Reason      = reason;
        }
    }
}
