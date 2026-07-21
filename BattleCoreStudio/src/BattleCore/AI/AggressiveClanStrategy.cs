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
    /// 積極攻撃戦略。Fog of War 対応版。
    /// 各 Army の VisionData に映っている敵・城のみを認識して行動する。
    /// 兵力が RetreatThreshold 以下の軍は自勢力の城へ撤退する。
    /// </summary>
    public class AggressiveClanStrategy : IClanStrategy
    {
        private readonly IPathFinder pathFinder = new HexPathFinder();

        /// <summary>撤退閾値（兵力がこの値以下の軍は撤退行動を取る）。</summary>
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

            var myCastles = world.Castles
                .Where(c => c.OwnerClanId == clan.Id)
                .ToList();

            foreach (var army in myArmies)
            {
                var currentHex = world.Map.GetHexById(army.CurrentHexId);
                if (currentHex == null) continue;

                // Fog of War: この Army の視界内にある HexId セット
                var visibleHexes = world.Visions.TryGetValue(army.Id, out var vision)
                    ? vision.VisibleHexes
                    : new HashSet<int>();

                // 視界内の敵軍
                var visibleEnemies = world.Armies
                    .Where(a => a.ClanId != clan.Id
                             && a.Soldiers > 0
                             && !world.AreAllied(clan.Id, a.ClanId)
                             && visibleHexes.Contains(a.CurrentHexId))
                    .ToList();

                // 視界内の敵城
                var visibleEnemyCastles = world.Castles
                    .Where(c => c.OwnerClanId != clan.Id
                             && visibleHexes.Contains(c.HexId))
                    .ToList();

                // 視界内に敵がいなければ待機
                if (!visibleEnemies.Any() && !visibleEnemyCastles.Any())
                    continue;

                // 同Hexに敵がいれば移動命令不要（BattleSystemが処理）
                if (visibleEnemies.Any(e => e.CurrentHexId == army.CurrentHexId))
                    continue;

                // 撤退：自勢力の城へ、なければ最遠Hexへ
                if (army.Soldiers <= RetreatThreshold)
                {
                    var target = GetRetreatTarget(army, currentHex, myCastles, visibleEnemies, world);
                    if (target != null && target != army.CurrentHexId)
                        yield return new MoveArmyCommand(army.Id, target.Value);
                    continue;
                }

                // 敵城への距離
                var nearestCastle = visibleEnemyCastles
                    .Select(c => new
                    {
                        c.HexId,
                        Dist = HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!)
                    })
                    .OrderBy(x => x.Dist)
                    .FirstOrDefault();

                // 最近敵軍への距離
                var nearestEnemy = visibleEnemies
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
                else if (nearestCastle != null)
                    targetHexId = nearestCastle.HexId;
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
            List<Army> visibleEnemies,
            WorldState world)
        {
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

            // 城がなければ視界内の敵から最遠のHexへ
            if (!visibleEnemies.Any()) return null;

            return world.Map.Hexes
                .Where(h => h.Terrain != TerrainType.Mountain)
                .OrderByDescending(h =>
                    visibleEnemies.Min(e =>
                        HexDistance.Calculate(h, world.Map.GetHexById(e.CurrentHexId)!)))
                .FirstOrDefault()?.Id;
        }
    }
}
