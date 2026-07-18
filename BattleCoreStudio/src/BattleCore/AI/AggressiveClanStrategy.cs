using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Navigation;
using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.AI
{
    /// <summary>
    /// 積極攻撃戦略。
    /// 敵城が敵軍より近い場合は城を優先して攻略する。
    /// 兵力が RetreatThreshold 以下の軍は自勢力の城へ撤退する（城がなければ最遠Hexへ）。
    /// </summary>
    public class AggressiveClanStrategy : IClanStrategy
    {
        private readonly IPathFinder pathFinder = new HexPathFinder();

        public int RetreatThreshold { get; }

        public AggressiveClanStrategy(int retreatThreshold = 300)
        {
            RetreatThreshold = retreatThreshold;
        }

        public IEnumerable<ICommand> Decide(Clan clan, WorldState world)
        {
            var myArmies = world.Armies
                .Where(a => a.ClanId == clan.Id && a.Soldiers > 0)
                .ToList();

            var enemyArmies = world.Armies
                .Where(a => a.ClanId != clan.Id
                         && a.Soldiers > 0
                         && !world.AreAllied(clan.Id, a.ClanId))
                .ToList();

            if (!enemyArmies.Any())
                yield break;

            var enemyCastles = world.Castles
                .Where(c => c.OwnerClanId != clan.Id)
                .ToList();

            var myCastles = world.Castles
                .Where(c => c.OwnerClanId == clan.Id)
                .ToList();

            foreach (var army in myArmies)
            {
                var currentHex = world.Map.GetHexById(army.CurrentHexId);
                if (currentHex == null) continue;

                if (enemyArmies.Any(e => e.CurrentHexId == army.CurrentHexId))
                    continue;

                // 撤退：自勢力の城へ、なければ最遠Hexへ
                if (army.Soldiers <= RetreatThreshold)
                {
                    var target = GetRetreatTarget(army, currentHex, myCastles, enemyArmies, world);
                    if (target != null && target != army.CurrentHexId)
                        yield return new MoveArmyCommand(army.Id, target.Value);
                    continue;
                }

                // 敵城への距離
                var nearestCastle = enemyCastles
                    .Select(c => new
                    {
                        c.HexId,
                        Dist = HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!)
                    })
                    .OrderBy(x => x.Dist)
                    .FirstOrDefault();

                // 最近敵軍への距離
                var nearestEnemy = enemyArmies
                    .Select(e => new
                    {
                        e.CurrentHexId,
                        Dist = HexDistance.Calculate(currentHex, world.Map.GetHexById(e.CurrentHexId)!)
                    })
                    .OrderBy(x => x.Dist)
                    .FirstOrDefault();

                // 敵城が敵軍より近い（×0.8）場合は城を優先
                int targetHexId;
                if (nearestCastle != null && nearestEnemy != null
                    && nearestCastle.Dist <= nearestEnemy.Dist * 0.8)
                    targetHexId = nearestCastle.HexId;
                else if (nearestEnemy != null)
                    targetHexId = nearestEnemy.CurrentHexId;
                else
                    continue;

                var path = pathFinder.FindPath(world.Map, army.CurrentHexId, targetHexId);
                if (path.Count > 1)
                    yield return new MoveArmyCommand(army.Id, path[1]);
            }
        }

        private int? GetRetreatTarget(
            Army army, Hex currentHex,
            List<Castle> myCastles,
            List<Army> enemyArmies,
            WorldState world)
        {
            // 自勢力の城があれば最近の城へ
            if (myCastles.Any())
            {
                var nearest = myCastles
                    .Select(c => new
                    {
                        c.HexId,
                        Dist = HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!)
                    })
                    .OrderBy(x => x.Dist)
                    .First();
                if (nearest.HexId != army.CurrentHexId)
                    return nearest.HexId;
            }

            // 城がなければ敵から最遠のHexへ
            return world.Map.Hexes
                .Where(h => h.Terrain != TerrainType.Mountain)
                .OrderByDescending(h =>
                    enemyArmies.Min(e =>
                        HexDistance.Calculate(h, world.Map.GetHexById(e.CurrentHexId)!)))
                .FirstOrDefault()?.Id;
        }
    }
}
