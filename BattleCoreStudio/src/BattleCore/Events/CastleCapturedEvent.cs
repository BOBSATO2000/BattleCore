namespace BattleCore.Events
{
    /// <summary>城が占領されたときに発火するイベント。</summary>
    public class CastleCapturedEvent : IGameEvent
    {
        /// <summary>
		/// 占領された城のID。
		/// </summary>
        public int    CastleId    { get; }

        /// <summary>
		/// 占領された城の名前。
		/// </summary>
        public string CastleName  { get; }

        /// <summary>
		/// 新たに占領した勢力ID。
		/// </summary>
        public int    NewOwnerClanId { get; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="castleId">占領された城のID</param>
		/// <param name="castleName">占領された城の名前</param>
		/// <param name="newOwnerClanId">新たに占領した勢力ID</param>
		public CastleCapturedEvent(int castleId, string castleName, int newOwnerClanId)
        {
            CastleId       = castleId;
            CastleName     = castleName;
            NewOwnerClanId = newOwnerClanId;
        }
    }
}
