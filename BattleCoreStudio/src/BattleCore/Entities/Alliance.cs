namespace BattleCore.Entities
{
    /// <summary>
    /// 2勢力間の同盟。RemainingTicks が0になると DiplomacySystem が解消する。
    /// </summary>
    public class Alliance : Entity
    {
        public int ClanId1        { get; }
        public int ClanId2        { get; }
        public int RemainingTicks { get; set; }

        public Alliance(int id, int clanId1, int clanId2, int durationTicks)
            : base(id)
        {
            ClanId1        = clanId1;
            ClanId2        = clanId2;
            RemainingTicks = durationTicks;
        }

        public bool Involves(int clanId) => ClanId1 == clanId || ClanId2 == clanId;
    }
}
