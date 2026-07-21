namespace BattleCore.Simulation
{
    /// <summary>
    /// ターンのフェーズ。
    /// SimulationEngine.StepPhase() で1フェーズずつ進める。
    /// PlayerPhase でプレイヤーが命令を入力し、AIPhase でAIが命令を決定、
    /// 以降のフェーズで順次解決する。
    /// </summary>
    public enum TurnPhase
    {
        /// <summary>プレイヤー命令入力フェーズ。PlayerController が CommandQueue に積む。</summary>
        PlayerPhase,

        /// <summary>AI命令決定フェーズ。ClanDecisionSystem が CommandQueue に積む。</summary>
        AIPhase,

        /// <summary>移動解決フェーズ。CommandExecutionSystem + MovementSystem が実行。</summary>
        Movement,

        /// <summary>戦闘解決フェーズ。BattleSystem が実行。</summary>
        Battle,

        /// <summary>補給・徴兵フェーズ。SupplySystem + RecruitmentSystem が実行。</summary>
        Supply,

        /// <summary>勝利判定フェーズ。VictorySystem が実行。</summary>
        Victory,
    }
}
