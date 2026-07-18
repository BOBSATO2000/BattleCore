using BattleCore.Commands;
using BattleCore.Simulation;

namespace BattleCore.Systems
{
    public class CommandExecutionSystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            while (context.CommandQueue.Count > 0)
            {
                var command = context.CommandQueue.Dequeue();
                command.Execute(context);
            }
        }
    }
}
