using BattleCore.Simulation;

namespace BattleCore.Commands
{
    public sealed class MoveOrder : ICommand
    {
        public int ArmyId { get; }
        public int DestinationHexId { get; }

        public MoveOrder(int armyId, int destinationHexId)
        {
            ArmyId = armyId;
            DestinationHexId = destinationHexId;
        }

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            army?.OrderMove(DestinationHexId);
        }
    }
}
