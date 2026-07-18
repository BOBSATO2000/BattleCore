using BattleCore.Simulation;

namespace BattleCore.Commands
{
    public sealed class WaitOrder : ICommand
    {
        public int ArmyId { get; }

        public WaitOrder(int armyId)
        {
            ArmyId = armyId;
        }

        public void Execute(SimulationContext context)
        {
            // 待機命令：何もしない
        }
    }
}
