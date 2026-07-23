using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Battle
{
    /// <summary>
    /// 戦闘ペアを生成する。
    ///
    /// 2種類の戦闘を処理する：
    ///   1. 隣接戦闘（MovementSystem が PendingAttackHexId を設定した場合）
    ///      → 攻撃側が敵Hexへ侵入を試みる。勝者が占領、敗者は押し返される。
    ///   2. 同Hex戦闘（城Hexで複数部隊が混在する場合）
    /// </summary>
    public class BattleFinder
    {
        public IEnumerable<Battle> Find(WorldState world)
        {
            var result    = new List<Battle>();
            var processed = new HashSet<(int, int)>();

            // 1. 隣接戦闘
            foreach (var attacker in world.Armies.Where(a =>
                a.Soldiers > 0 && a.PendingAttackHexId.HasValue))
            {
                int targetHexId = attacker.PendingAttackHexId!.Value;
                var defenders = world.Armies
                    .Where(d => d.Soldiers > 0 &&
                                d.CurrentHexId == targetHexId &&
                                d.ClanId != attacker.ClanId &&
                                !world.AreAllied(attacker.ClanId, d.ClanId) &&
                                !world.IsInCeasefire(attacker.ClanId, d.ClanId))
                    .ToList();

                foreach (var defender in defenders)
                {
                    var key = attacker.Id < defender.Id
                        ? (attacker.Id, defender.Id)
                        : (defender.Id, attacker.Id);
                    if (processed.Add(key))
                        result.Add(new Battle(attacker, defender) { IsAdjacentBattle = true });
                }
            }

            // 2. 同Hex戦闘（城Hexで複数部隊が混在する場合）
            foreach (var hexGroup in world.Armies.GroupBy(x => x.CurrentHexId))
            {
                var armies = hexGroup.ToList();
                for (int i = 0; i < armies.Count; i++)
                {
                    for (int j = i + 1; j < armies.Count; j++)
                    {
                        if (armies[i].ClanId == armies[j].ClanId) continue;
                        if (world.AreAllied(armies[i].ClanId, armies[j].ClanId)) continue;
                        if (world.IsInCeasefire(armies[i].ClanId, armies[j].ClanId)) continue;

                        var key = armies[i].Id < armies[j].Id
                            ? (armies[i].Id, armies[j].Id)
                            : (armies[j].Id, armies[i].Id);
                        if (processed.Add(key))
                            result.Add(new Battle(armies[i], armies[j]));
                    }
                }
            }

            return result;
        }
    }
}
