namespace BattleCore.AI
{
    /// <summary>
    /// 武将1人分の意思決定結果。
    /// OfficerDecision.Evaluate() が返し、ClanDecisionSystem が CommandQueue/EventQueue に振り分ける。
    /// </summary>
    public sealed class DecisionResult
    {
        /// <summary>実行する命令。null の場合は命令拒否（Accepted=false）。</summary>
        public Commands.ICommand? Command { get; init; }

        /// <summary>判断理由。</summary>
        public Commands.DecisionReason Reason { get; init; }

        /// <summary>命令が受諾されたか。false の場合 Command は null。</summary>
        public bool Accepted { get; init; }

        /// <summary>この判断に伴って発生するイベント。null の場合はイベントなし。</summary>
        public IGameEvent? Event { get; init; }

        /// <summary>命令受諾（通常・変更含む）の結果を生成する。</summary>
        public static DecisionResult Accept(
            Commands.ICommand command,
            Commands.DecisionReason reason = Commands.DecisionReason.Advance,
            IGameEvent? ev = null)
            => new() { Command = command, Reason = reason, Accepted = true, Event = ev };

        /// <summary>命令拒否の結果を生成する。</summary>
        public static DecisionResult Refuse(IGameEvent? ev = null)
            => new() { Command = null, Reason = Commands.DecisionReason.Advance, Accepted = false, Event = ev };
    }
}
