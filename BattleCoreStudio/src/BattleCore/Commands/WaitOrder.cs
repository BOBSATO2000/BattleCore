using BattleCore.Simulation;

namespace BattleCore.Commands
{
    /// <summary>
    /// 「指定した軍を待機させる」命令。
    /// DecisionSystem が「今は何もしない」と判断した際に生成する。
    /// </summary>
    public sealed class WaitOrder : ICommand
    {
        /// <summary>待機させる軍のID。</summary>
        public int ArmyId { get; }

        public WaitOrder(int armyId)
        {
            ArmyId = armyId;
        }

        /// <summary>待機命令：何もしない。</summary>
        public void Execute(SimulationContext context)
        {
            // 待機命令：何もしない
        }
    }
}
