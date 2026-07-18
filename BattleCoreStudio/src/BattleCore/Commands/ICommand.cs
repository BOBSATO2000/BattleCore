using BattleCore.Simulation;

namespace BattleCore.Commands
{
    /// <summary>
    /// ゲーム内命令の共通インターフェース。
    /// DecisionSystem（AI）が生成し CommandExecutionSystem が実行する。
    /// Commandパターンを採用することで、CPU・人間・ネットワーク・リプレイ全てが
    /// 同じ実行基盤を使えるようになる（会話履歴 7.txt より）。
    /// </summary>
    public interface ICommand
    {
        /// <summary>命令を実行する。SimulationContext 経由で World を変更する。</summary>
        void Execute(SimulationContext context);
    }
}
