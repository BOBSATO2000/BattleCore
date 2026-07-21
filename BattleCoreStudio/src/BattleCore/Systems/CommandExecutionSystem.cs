using BattleCore.Commands;
using BattleCore.Simulation;

namespace BattleCore.Systems
{
    /// <summary>
    /// 命令実行システム。
    /// CommandQueue に積まれた命令を全て Dequeue して実行する。
    /// ClanDecisionSystem / DecisionSystem の後、MovementSystem の前に登録すること。
    /// </summary>
    public class CommandExecutionSystem : ISimulationSystem
    {
        /// <summary>CommandQueue を全て消化し、各命令を実行する。</summary>
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
