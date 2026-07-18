using BattleCore.Events;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 外交システム。毎Step同盟の残りTickを減らし、期限切れで解消する。
    /// </summary>
    public class DiplomacySystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            var expired = context.World.Alliances
                .Where(a => a.RemainingTicks > 0)
                .ToList();

            foreach (var alliance in expired)
            {
                alliance.RemainingTicks--;
                if (alliance.RemainingTicks <= 0)
                {
                    var c1 = context.World.Clans.FirstOrDefault(c => c.Id == alliance.ClanId1);
                    var c2 = context.World.Clans.FirstOrDefault(c => c.Id == alliance.ClanId2);
                    context.EventQueue.Enqueue(new ScenarioEvent(
                        "alliance_expired",
                        $"【同盟解消】{c1?.Name ?? "?"}と{c2?.Name ?? "?"}の同盟が期限切れとなった。"));
                }
            }

            context.World.Alliances.RemoveAll(a => a.RemainingTicks <= 0);
        }
    }
}
