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
    /// 兵力が十分な軍は最も近い敵へ進軍する。
    /// 兵力が RetreathThreshold 以下の軍は敵から最も遠いHexへ撤退する。
    /// </summary>
    public class AggressiveClanStrategy : IClanStrategy
    {
        private readonly IPathFinder pathFinder = new HexPathFinder();

        /// <summary>この兵力以下になった軍は撤退する。</summary>
        public int RetreatThreshold { get; }

        public AggressiveClanStrategy(int retreatThreshold = 300)
        {
            RetreatThreshold = retreatThreshold;
        }

        public IEnumerable<ICommand> Decide(Clan clan, WorldState world)
        {
            // この勢力の全 Army を取得（兵力0は除外）
            var myArmies = world.Armies
                .Where(a => a.ClanId == clan.Id && a.Soldiers > 0)
                .ToList();

            // 敵 Army を取得（兵力0は除外・同盟中は除外）
            var enemyArmies = world.Armies
                .Where(a => a.ClanId != clan.Id
                         && a.Soldiers > 0
                         && !world.AreAllied(clan.Id, a.ClanId))
                .ToList();

            if (!enemyArmies.Any())
                yield break;

            foreach (var army in myArmies)
            {
                // 同じ Hex に敵がいる場合は待機（BattleSystem が処理する）
                if (enemyArmies.Any(e => e.CurrentHexId == army.CurrentHexId))
                    continue;

                // 兵力が閾値以下 → 敵から最も遠いHexへ撤退
                if (army.Soldiers <= RetreatThreshold)
                {
                    var retreatHex = world.Map.Hexes
                        .Where(h => h.Terrain != Map.TerrainType.Mountain)
                        .Select(h => new
                        {
                            HexId   = h.Id,
                            MinDist = enemyArmies.Min(e =>
                                HexDistance.Calculate(
                                    world.Map.GetHexById(army.CurrentHexId)!,
                                    world.Map.GetHexById(e.CurrentHexId)!))
                        })
                        .OrderByDescending(x => x.MinDist)
                        .FirstOrDefault();

                    if (retreatHex != null && retreatHex.HexId != army.CurrentHexId)
                        yield return new MoveArmyCommand(army.Id, retreatHex.HexId);
                    continue;
                }

                // 最も近い敵を PathFinder で探す
                var nearest = enemyArmies
                    .Select(e => new
                    {
                        Enemy = e,
                        Path = pathFinder.FindPath(
                            world.Map,
                            army.CurrentHexId,
                            e.CurrentHexId)
                    })
                    .Where(x => x.Path.Count > 1)
                    .OrderBy(x => x.Path.Count)
                    .FirstOrDefault();

                if (nearest == null)
                    continue;

                // 経路の次の Hex へ移動命令を発行
                var nextHexId = nearest.Path[1];
                yield return new MoveArmyCommand(army.Id, nextHexId);
            }
        }
    }
}
