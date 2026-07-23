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
        /// <summary>発火条件を持つトリガーリスト。ScenarioLoader から渡される。</summary>
        private readonly List<EventTriggerData> triggers;

        /// <summary>発火済みトリガーIDのセット。同じイベントの二重発火を防ぐ。</summary>
        private readonly HashSet<string> fired = new();

        /// <summary>triggers リストを注入するコンストラクタ。ScenarioLoader から渡される。</summary>
        public EventTriggerSystem(IEnumerable<EventTriggerData> triggers)
        {
            this.triggers = triggers.ToList();
        }

        /// <summary>
        /// 全トリガーの条件を評価し、条件を満たしたものを一度だけ発火する。
        /// 発火済みトリガーは fired セットで管理し二重発火を防ぐ。
        /// </summary>
        public void Update(SimulationContext context)
        {
            var world = context.World;
            var tick = context.Time.Tick;

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

                    // 隣接条件: 指定武将の軍が別の武将の軍と隣接Hexにいるか
                    if (t.AdjacentToOfficerId.HasValue)
                    {
                        var armyA = world.Armies.FirstOrDefault(a => a.OfficerId == t.OfficerId.Value);
                        var armyB = world.Armies.FirstOrDefault(a => a.OfficerId == t.AdjacentToOfficerId.Value);
                        if (armyA == null || armyB == null) continue;

                        var neighbors = world.Map.GetNeighbors(armyA.CurrentHexId);
                        bool adjacent = armyA.CurrentHexId == armyB.CurrentHexId
                            || neighbors.Any(h => h.Id == armyB.CurrentHexId);
                        if (!adjacent) continue;
                    }
                }

                fired.Add(t.Id);
                context.EventQueue.Enqueue(new ScenarioEvent(t.Id, t.Message));
            }
        }
    }
}
