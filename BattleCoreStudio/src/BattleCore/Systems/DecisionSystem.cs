using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Simulation;

namespace BattleCore.Systems
{
    /// <summary>
    /// AI判断システム。各 Army に対して IArmyDecision.Decide() を呼び、
    /// 返ってきた命令を CommandQueue に積む。
    /// 
    /// 「考えるターン」を担当し、実際の実行は CommandExecutionSystem が行う。
    /// IArmyDecision を差し替えることで CPU・人間・ネットワーク全てに対応できる。
    /// </summary>
    public class DecisionSystem : ISimulationSystem
    {
        private readonly IArmyDecision decision;

        public DecisionSystem(IArmyDecision decision)
        {
            this.decision = decision;
        }

        public void Update(SimulationContext context)
        {
            foreach (var army in context.World.Armies)
            {
                var command = decision.Decide(army, context.World);

                if (command != null)
                    context.CommandQueue.Enqueue(command);
            }
        }
    }
}
