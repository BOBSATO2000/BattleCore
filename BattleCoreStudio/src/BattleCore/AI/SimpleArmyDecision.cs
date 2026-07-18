using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Navigation;
using BattleCore.World;
using System.Linq;

namespace BattleCore.AI
{
    /// <summary>
    /// 最初の簡易AI実装。
    /// 会話履歴（7.txt）の設計：
    ///   1. 同じHexに敵がいる → 動かない（BattleSystem に任せる）
    ///   2. 隣接Hexに敵がいる → 敵Hexへ移動命令
    ///   3. 敵が遠い         → PathFinder で最短経路を計算し次のHexへ移動命令
    ///   4. 敵がいない       → 待機（null を返す）
    /// </summary>
    public class SimpleArmyDecision : IArmyDecision
    {
        private readonly IPathFinder pathFinder = new HexPathFinder();

        public ICommand? Decide(Army army, WorldState world)
        {
            // 敵軍を全て取得
            var enemies = world.Armies
                .Where(x => x.ClanId != army.ClanId)
                .ToList();

            if (!enemies.Any())
                return null;

            // 同じHexに敵がいる場合は待機（BattleSystem が処理する）
            if (enemies.Any(x => x.CurrentHexId == army.CurrentHexId))
                return null;

            // 最も近い敵を探す
            var nearestEnemy = enemies
                .Select(e => new
                {
                    Enemy = e,
                    Path = pathFinder.FindPath(world.Map, army.CurrentHexId, e.CurrentHexId)
                })
                .Where(x => x.Path.Count > 1)
                .OrderBy(x => x.Path.Count)
                .FirstOrDefault();

            if (nearestEnemy == null)
                return null;

            // 経路の次のHex（インデックス1）へ移動命令を出す
            var nextHexId = nearestEnemy.Path[1];
            return new MoveArmyCommand(army.Id, nextHexId);
        }
    }
}
