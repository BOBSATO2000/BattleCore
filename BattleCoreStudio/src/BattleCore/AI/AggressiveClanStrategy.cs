using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Navigation;
using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.AI
{
    /// <summary>
    /// 積極攻撃戦略。
    /// 勢力が保有する全 Army を、最も近い敵 Army へ向かわせる。
    /// 
    /// ルール：
    ///   1. この Clan に所属する全 Army を取得する
    ///   2. 各 Army に対して最も近い敵 Army を PathFinder で探す
    ///   3. 敵への経路の次の Hex へ MoveArmyCommand を発行する
    ///   4. 同じ Hex に敵がいる場合は移動しない（BattleSystem に任せる）
    /// </summary>
    public class AggressiveClanStrategy : IClanStrategy
    {
        private readonly IPathFinder pathFinder = new HexPathFinder();

        public IEnumerable<ICommand> Decide(Clan clan, WorldState world)
        {
            // この勢力の全 Army を取得（兵力0は除外）
            var myArmies = world.Armies
                .Where(a => a.ClanId == clan.Id && a.Soldiers > 0)
                .ToList();

            // 敵 Army を取得（兵力0は除外）
            var enemyArmies = world.Armies
                .Where(a => a.ClanId != clan.Id && a.Soldiers > 0)
                .ToList();

            if (!enemyArmies.Any())
                yield break;

            foreach (var army in myArmies)
            {
                // 同じ Hex に敵がいる場合は待機（BattleSystem が処理する）
                if (enemyArmies.Any(e => e.CurrentHexId == army.CurrentHexId))
                    continue;

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
