namespace BattleCore.Events
{
    /// <summary>
    /// ゲーム終了イベント。VictorySystem が発火する。
    /// WinnerClanId が null の場合は引き分け（全滅など）。
    /// </summary>
    public class GameOverEvent : IGameEvent
    {
        public int?   WinnerClanId { get; }
        public string Reason       { get; }

        public GameOverEvent(int? winnerClanId, string reason)
        {
            WinnerClanId = winnerClanId;
            Reason       = reason;
        }
    }
}
