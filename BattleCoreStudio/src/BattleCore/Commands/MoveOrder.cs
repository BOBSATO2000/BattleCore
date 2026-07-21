using BattleCore.Simulation;

namespace BattleCore.Commands
{
    /// <summary>
    /// 「指定した軍を指定したHexへ移動させる」命令。MoveArmyCommand の別名。
    /// DecisionSystem（AI）が生成し、CommandExecutionSystem が Army.OrderMove() を呼ぶ。
    /// </summary>
    public sealed class MoveOrder : ICommand
    {
        /// <summary>移動させる軍のID。</summary>
        public int ArmyId { get; }

        /// <summary>移動先HexID。</summary>
        public int DestinationHexId { get; }

        public MoveOrder(int armyId, int destinationHexId)
        {
            ArmyId = armyId;
            DestinationHexId = destinationHexId;
        }

        /// <summary>WorldState から Army を検索し、移動目標を設定する。Army が見つからない場合は何もしない。</summary>
        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            army?.OrderMove(DestinationHexId);
        }
    }
}
