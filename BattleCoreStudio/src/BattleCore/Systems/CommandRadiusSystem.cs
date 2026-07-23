using BattleCore.Map;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 指揮範囲システム。毎Tick実行。
    /// 武将（Officer）の Leadership に応じた指揮範囲内の味方軍に士気ボーナスを与える。
    /// 指揮範囲 = Leadership / 30（最低1、最大5）。
    /// 範囲内の味方軍: Morale+2/Tick。
    /// 範囲外の味方軍: Morale-1/Tick（本陣から離れすぎると士気が下がる）。
    /// </summary>
    public class CommandRadiusSystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            var world = context.World;

            foreach (var clan in world.Clans)
            {
                // 君主（DaimyoOfficerId）を持つ勢力のみ処理
                if (!clan.DaimyoOfficerId.HasValue) continue;

                var daimyo = world.Officers.FirstOrDefault(o => o.Id == clan.DaimyoOfficerId.Value);
                if (daimyo == null) continue;

                // 君主が指揮する軍を探す
                var commandArmy = world.Armies.FirstOrDefault(a =>
                    a.OfficerId == daimyo.Id && a.Soldiers > 0);
                if (commandArmy == null) continue;

                var commandHex = world.Map.GetHexById(commandArmy.CurrentHexId);
                if (commandHex == null) continue;

                int radius = System.Math.Clamp(daimyo.Leadership / 30, 1, 5);

                foreach (var army in world.Armies.Where(a => a.ClanId == clan.Id && a.Soldiers > 0))
                {
                    var armyHex = world.Map.GetHexById(army.CurrentHexId);
                    if (armyHex == null) continue;

                    int dist = HexDistance.Calculate(commandHex, armyHex);
                    if (dist <= radius)
                        army.Morale = System.Math.Min(100, army.Morale + 2);
                    else
                        army.Morale = System.Math.Max(0, army.Morale - 1);
                }
            }
        }
    }
}
