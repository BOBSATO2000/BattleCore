namespace BattleCore.Simulation
{
    /// <summary>
    /// 特定の TurnPhase にのみ実行される System のインターフェース。
    /// ISimulationSystem と組み合わせて実装する。
    /// StepPhase() はこのインターフェースを持つ System だけをフェーズ別に実行する。
    /// </summary>
    public interface IPhasedSystem
    {
        /// <summary>このSystemが実行されるフェーズ。</summary>
        TurnPhase Phase { get; }
    }
}
