using BattleCore.AI;
using BattleCore.Simulation;

namespace BattleCore.Systems
{
    /// <summary>
    /// 勢力単位の戦略決定システム。2層構造。
    ///
    /// Layer 1: IClanStrategy.Decide() — 勢力として何をしたいか
    /// Layer 2: OfficerDecision.Filter() — この武将は従うのか？
    ///
    /// 処理の流れ：
    ///   IClanStrategy → 命令リスト生成
    ///       ↓
    ///   OfficerDecision → 性格・忠誠・人間関係でフィルタ
    ///       ↓
    ///   CommandQueue に積む
    ///       ↓
    ///   EventQueue に武将イベントを積む
    /// </summary>
    public class ClanDecisionSystem : ISimulationSystem
    {
        private readonly IClanStrategy   strategy;
        private readonly OfficerDecision officerDecision;

        public ClanDecisionSystem(IClanStrategy strategy, OfficerDecision? officerDecision = null)
        {
            this.strategy        = strategy;
            this.officerDecision = officerDecision ?? new OfficerDecision();
        }

        public void Update(SimulationContext context)
        {
            foreach (var clan in context.World.Clans)
            {
                if (clan.IsPlayerControlled) continue;

                // Layer 1: 勢力戦略
                var rawCommands = strategy.Decide(clan, context.World);

                // Layer 2: 武将意思決定（DecisionResult）
                foreach (var result in officerDecision.Evaluate(rawCommands, clan, context.World))
                {
                    if (result.Accepted && result.Command != null)
                        context.CommandQueue.Enqueue(result.Command);
                    if (result.Event != null)
                        context.EventQueue.Enqueue(result.Event);
                }
            }
        }
    }
}
