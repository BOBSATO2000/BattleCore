using BattleCore.AI;
using BattleCore.Simulation;

namespace BattleCore.Systems
{
    /// <summary>
    /// 勢力単位の戦略決定システム。
    /// SimpleArmyDecision（Army単位）の上位に位置し、Clan 全体の戦略を担当する。
    /// 
    /// 処理の流れ：
    ///   WorldState の全 Clan を走査
    ///       ↓
    ///   IClanStrategy.Decide(clan, world) で命令リストを取得
    ///       ↓
    ///   CommandQueue に積む
    ///       ↓
    ///   CommandExecutionSystem が実行
    ///       ↓
    ///   MovementSystem が移動
    /// 
    /// DecisionSystem（Army単位）と併用する場合は
    /// ClanDecisionSystem を先に登録することを推奨する。
    /// </summary>
    public class ClanDecisionSystem : ISimulationSystem
    {
        private readonly IClanStrategy strategy;

        public ClanDecisionSystem(IClanStrategy strategy)
        {
            this.strategy = strategy;
        }

        public void Update(SimulationContext context)
        {
            foreach (var clan in context.World.Clans)
            {
                foreach (var command in strategy.Decide(clan, context.World))
                {
                    context.CommandQueue.Enqueue(command);
                }
            }
        }
    }
}
