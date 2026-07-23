namespace BattleCore.Events
{
    public enum SiegeEventType { SiegeStarted, SiegeLifted, Surrendered }

    /// <summary>包囲開始・解除・降伏時に発火するイベント。</summary>
    public class SiegeEvent : IGameEvent
    {
        public SiegeEventType Type        { get; }
        public string         CastleName  { get; }
        public int            OwnerClanId { get; }

        public SiegeEvent(SiegeEventType type, string castleName, int ownerClanId)
        {
            Type        = type;
            CastleName  = castleName;
            OwnerClanId = ownerClanId;
        }
    }
}
