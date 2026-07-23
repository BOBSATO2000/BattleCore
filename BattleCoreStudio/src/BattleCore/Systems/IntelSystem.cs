using BattleCore.Events;
using BattleCore.Simulation;
using BattleCore.Vision;
using System;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 諜報システム。毎Tick実行。
    /// - 各勢力が敵勢力1つをランダムに諜報（成功率: Intelligence/200）
    /// - 成功すると WorldState.Intel を更新し IntelEvent を発火
    /// - 霧の中（Fog天気）は成功率半減
    /// </summary>
    public class IntelSystem : ISimulationSystem
    {
        private readonly Random rng = new();

        public void Update(SimulationContext context)
        {
            var world = context.World;

            foreach (var clan in world.Clans)
            {
                var spyOfficer = world.Officers
                    .Where(o => world.Memberships.Any(m => m.ClanId == clan.Id && m.OfficerId == o.Id))
                    .OrderByDescending(o => o.Intelligence)
                    .FirstOrDefault();
                if (spyOfficer == null) continue;

                double successRate = spyOfficer.Intelligence / 200.0;
                if (context.Time.Weather == Simulation.Weather.Fog)
                    successRate /= 2.0;

                // 偵察命令中の軍があれば成功率+50%
                bool hasScouter = world.Armies.Any(a =>
                    a.ClanId == clan.Id && a.ScoutingBonus);
                if (hasScouter) successRate = System.Math.Min(1.0, successRate + 0.50);

                if (rng.NextDouble() > successRate) continue;

                var enemies = world.Clans
                    .Where(c => c.Id != clan.Id && !world.AreAllied(clan.Id, c.Id))
                    .ToList();
                if (!enemies.Any()) continue;

                var target = enemies[rng.Next(enemies.Count)];

                var targetArmies = world.Armies
                    .Where(a => a.ClanId == target.Id && a.Soldiers > 0)
                    .ToList();
                if (!targetArmies.Any()) continue;

                // WorldState.Intel を更新（Vision とは別の情報層）
                foreach (var enemyArmy in targetArmies)
                {
                    var key = (clan.Id, enemyArmy.Id);
                    world.Intel[key] = new IntelData(
                        ownerClanId:    clan.Id,
                        enemyArmyId:    enemyArmy.Id,
                        lastKnownHexId: enemyArmy.CurrentHexId,
                        acquiredTick:   context.Time.Tick);
                }

                var info = string.Join(", ", targetArmies.Select(a =>
                {
                    var off = a.OfficerId.HasValue
                        ? world.Officers.FirstOrDefault(o => o.Id == a.OfficerId.Value)
                        : null;
                    return $"{off?.Name ?? "?"}隊 兵:{a.Soldiers}";
                }));

                context.EventQueue.Enqueue(new IntelEvent(clan.Name, target.Name, info));
            }
        }
    }
}
