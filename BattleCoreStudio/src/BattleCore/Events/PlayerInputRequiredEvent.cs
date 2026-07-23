namespace BattleCore.Events
{
    /// <summary>
    /// プレイヤー勢力のターンが来たことを通知するイベント。
    /// SimulationEngine が WaitingForPlayer=true になる直前に EventQueue に積む。
    /// UI はこのイベントを受け取ったら命令入力UIを有効化し、
    /// 「命令確定」ボタン押下後に SimulationEngine.ConfirmPlayerInput() を呼ぶ。
    /// </summary>
    public sealed class PlayerInputRequiredEvent : IGameEvent
    {
        /// <summary>入力を求められているプレイヤーの勢力ID。</summary>
        public int PlayerClanId { get; }
        /// <summary>現在のTick。</summary>
        public int Tick         { get; }

        public PlayerInputRequiredEvent(int playerClanId, int tick)
        {
            PlayerClanId = playerClanId;
            Tick         = tick;
        }
    }
}
