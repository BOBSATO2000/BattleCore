using BattleCore.Events;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 毎Step勝利条件を評価し、条件を満たしたら GameOverEvent を発火する。
    ///
    /// 勝利条件：兵力が残っている勢力が1つだけになった（天下統一）
    /// 引き分け条件：兵力が残っている勢力が0になった（全滅）
    /// </summary>
    public class VictorySystem : ISimulationSystem
    {
        private bool gameOver = false;

        public void Update(SimulationContext context)
        {
            if (gameOver) return;

            // 兵力が残っている勢力（ClanId=0の無所属は除外）
            var activeClanIds = context.World.Armies
                .Where(a => a.Soldiers > 0 && a.ClanId != 0)
                .Select(a => a.ClanId)
                .Distinct()
                .ToList();

            if (activeClanIds.Count == 1)
            {
                gameOver = true;
                var winnerClan = context.World.Clans
                    .FirstOrDefault(c => c.Id == activeClanIds[0]);
                context.EventQueue.Enqueue(new GameOverEvent(
                    activeClanIds[0],
                    $"{winnerClan?.Name ?? "?"} が天下を統一した！"));
            }
            else if (activeClanIds.Count == 0)
            {
                gameOver = true;
                context.EventQueue.Enqueue(new GameOverEvent(null, "全勢力が滅亡した。"));
            }
        }
    }
}
