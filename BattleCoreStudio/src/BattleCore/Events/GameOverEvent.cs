namespace BattleCore.Events
{
    /// <summary>
    /// ゲーム終了イベント。VictorySystem が発火する。
    /// WinnerClanId が null の場合は引き分け（全滅など）。
    /// </summary>
    public class GameOverEvent : IGameEvent
    {
        /// <summary>
		/// 勝利した勢力ID。
		/// null の場合は引き分け（全滅など）。
		/// </summary>
        public int?   WinnerClanId { get; }

        /// <summary>
		/// ゲーム終了理由のメッセージ。UIログに表示する。
		/// </summary>
        public string Reason       { get; }

        /// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="winnerClanId">勝利した勢力ID。nullの場合は引き分け。</param>
		/// <param name="reason">ゲーム終了理由のメッセージ。</param>
        public GameOverEvent(int? winnerClanId, string reason)
        {
            WinnerClanId = winnerClanId;
            Reason       = reason;
        }
    }
}
