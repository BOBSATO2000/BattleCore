using BattleCore.Events;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 一騎討ちシステム。毎Tick実行。
    /// 同Hexに敵軍がいる場合、一定確率で一騎討ちが発生する。
    /// 発生確率: 3%/Tick（両軍に武将がいる場合のみ）。
    /// 勝敗判定: Courage の比較（乱数あり）。
    /// 勝者: 敵軍士気-20。敗者: 自軍士気-10。
    /// </summary>
    public class DuelSystem : ISimulationSystem
    {
        private const double DuelChance = 0.03;
        private readonly System.Random rng = new();

        public void Update(SimulationContext context)
        {
            var world = context.World;
            var processed = new System.Collections.Generic.HashSet<(int, int)>();

            foreach (var army in world.Armies.Where(a => a.Soldiers > 0 && a.OfficerId.HasValue))
            {
                var enemies = world.Armies
                    .Where(e => e.Soldiers > 0 &&
                                e.ClanId != army.ClanId &&
                                !world.AreAllied(army.ClanId, e.ClanId) &&
                                e.CurrentHexId == army.CurrentHexId &&
                                e.OfficerId.HasValue)
                    .ToList();

                foreach (var enemy in enemies)
                {
                    var key = army.Id < enemy.Id ? (army.Id, enemy.Id) : (enemy.Id, army.Id);
                    if (!processed.Add(key)) continue;
                    if (rng.NextDouble() >= DuelChance) continue;

                    var challenger = world.Officers.FirstOrDefault(o => o.Id == army.OfficerId!.Value);
                    var defender   = world.Officers.FirstOrDefault(o => o.Id == enemy.OfficerId!.Value);
                    if (challenger == null || defender == null) continue;

                    double challengerScore = challenger.Courage * (0.8 + rng.NextDouble() * 0.4);
                    double defenderScore   = defender.Courage   * (0.8 + rng.NextDouble() * 0.4);
                    bool challengerWon = challengerScore >= defenderScore;

                    if (challengerWon)
                    {
                        enemy.Morale  = System.Math.Max(0, enemy.Morale - 20);
                        army.Morale   = System.Math.Min(100, army.Morale + 5);
                        context.EventQueue.Enqueue(new DuelEvent(challenger.Name, defender.Name, true, -20));
                    }
                    else
                    {
                        army.Morale   = System.Math.Max(0, army.Morale - 10);
                        enemy.Morale  = System.Math.Min(100, enemy.Morale + 5);
                        context.EventQueue.Enqueue(new DuelEvent(challenger.Name, defender.Name, false, -10));
                    }
                }
            }
        }
    }
}
