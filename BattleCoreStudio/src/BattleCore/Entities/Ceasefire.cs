namespace BattleCore.Entities
{
    /// <summary>
    /// 2勢力間の停戦協定。
    /// RemainingTicks が0になると DiplomacySystem が解消する。
    /// 停戦中は互いに攻撃しない（BattleFinder がスキップ）。
    /// </summary>
    public class Ceasefire : Entity
    {
        public int ClanId1        { get; }
        public int ClanId2        { get; }
        public int RemainingTicks { get; set; }

        public Ceasefire(int id, int clanId1, int clanId2, int durationTicks) : base(id)
        {
            ClanId1        = clanId1;
            ClanId2        = clanId2;
            RemainingTicks = durationTicks;
        }

        public bool Involves(int clanId) => ClanId1 == clanId || ClanId2 == clanId;
    }
}
