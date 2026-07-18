namespace BattleCore.Events
{
    /// <summary>城が占領されたときに発火するイベント。</summary>
    public class CastleCapturedEvent : IGameEvent
    {
        public int    CastleId    { get; }
        public string CastleName  { get; }
        public int    NewOwnerClanId { get; }

        public CastleCapturedEvent(int castleId, string castleName, int newOwnerClanId)
        {
            CastleId       = castleId;
            CastleName     = castleName;
            NewOwnerClanId = newOwnerClanId;
        }
    }
}
