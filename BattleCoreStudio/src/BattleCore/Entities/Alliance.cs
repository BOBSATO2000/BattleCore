namespace BattleCore.Entities
{
    /// <summary>
    /// 2勢力間の同盟。
	/// RemainingTicks が0になると DiplomacySystem が解消する。
    /// </summary>
    public class Alliance : Entity
    {
        /// <summary>
		/// 同盟に関与する両勢力ID。
		/// </summary>
        public int ClanId1        { get; }

        /// <summary>
		/// 同盟に関与する両勢力ID。
		/// </summary>
        public int ClanId2        { get; }

        /// <summary>
		/// 同盟の残り有効Tick数。
		/// 0になると DiplomacySystem が解消する。
		/// </summary>
        public int RemainingTicks { get; set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="id">Alliance エンティティ自身のID。</param>
		/// <param name="clanId1">同盟を結ぶ勢力1のID</param>
		/// <param name="clanId2">同盟を結ぶ勢力2のID</param>
		/// <param name="durationTicks">同盟の有効期間。RemainingTicks にセットされ、毎Step DiplomacySystem が -1 していく</param>
		public Alliance(int id, int clanId1, int clanId2, int durationTicks)
            : base(id)
        {
            ClanId1        = clanId1;
            ClanId2        = clanId2;
            RemainingTicks = durationTicks;
        }

        /// <summary>
		/// 同盟に関与する勢力IDかどうかを返す。
		/// </summary>
        public bool Involves(int clanId) => ClanId1 == clanId || ClanId2 == clanId;
    }
}
