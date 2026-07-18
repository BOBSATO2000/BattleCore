using BattleCore.Events;
using BattleCore.Scenario;
using BattleCore.Simulation;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// シナリオイベントトリガーを毎Step評価し、条件を満たしたら一度だけ発火する。
    /// </summary>
    public class EventTriggerSystem : ISimulationSystem
    {
        private readonly List<EventTriggerData> triggers;
        private readonly HashSet<string> fired = new();

        public EventTriggerSystem(IEnumerable<EventTriggerData> triggers)
        {
            this.triggers = triggers.ToList();
        }

        public void Update(SimulationContext context)
        {
            var tick  = context.Time.Tick;
            var world = context.World;

            foreach (var t in triggers)
            {
                if (fired.Contains(t.Id)) continue;
                if (tick < t.MinTick)     continue;

                if (t.OfficerId.HasValue)
                {
                    var officer = world.Officers.FirstOrDefault(o => o.Id == t.OfficerId.Value);
                    if (officer == null) continue;

                    if (t.MinDislike.HasValue)
                    {
                        var totalDislike = world.Relationships
                            .Where(r => r.FromOfficerId == officer.Id)
                            .Sum(r => r.Dislike);
                        if (totalDislike < t.MinDislike.Value) continue;
                    }

                    if (t.MaxLoyalty.HasValue && officer.Loyalty > t.MaxLoyalty.Value)
                        continue;
                }

                fired.Add(t.Id);
                context.EventQueue.Enqueue(new ScenarioEvent(t.Id, t.Message));
            }
        }
    }
}
