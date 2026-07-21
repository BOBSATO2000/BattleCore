using BattleCore.Simulation;

namespace BattleCore.Commands
{
    /// <summary>
    /// 「指定した軍を指定したHexへ移動させる」命令。
    /// DecisionSystem（AI）が生成し、CommandExecutionSystem が Army.OrderMove() を呼ぶ。
    /// 実際の移動は MovementSystem が1Hexずつ行う。
    /// </summary>
    public class MoveArmyCommand : ICommand
    {
        private readonly int armyId;
        private readonly int destinationHexId;

        public int ArmyId           => armyId;
        public int DestinationHexId => destinationHexId;

        /// <summary>命令の判断理由。デバッグ・UI表示・将来のプレイヤー説明に使用。</summary>
        public DecisionReason Reason { get; }

        public MoveArmyCommand(int armyId, int destinationHexId,
            DecisionReason reason = DecisionReason.Advance)
        {
            this.armyId = armyId;
            this.destinationHexId = destinationHexId;
            Reason = reason;
        }

        /// <summary>
        /// WorldState から Army を検索し、移動目標を設定する。
        /// Army が見つからない場合は何もしない。
        /// </summary>
        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(armyId);
            army?.OrderMove(destinationHexId);
        }
    }
}
