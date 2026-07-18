using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Battle
{
    /// <summary>
    /// 同じHexに存在する異なる勢力の軍を探し、戦闘ペアを生成する。
    /// BattleSystem から利用され、「誰と誰が戦うか」の判定を担当する。
    /// 将来的には挟撃・援軍・多数対多数にも対応できるよう拡張する。
    /// </summary>
    public class BattleFinder
    {
        /// <summary>
        /// WorldState を走査し、戦闘が発生する全ペアを返す。
        /// 同じHexにいる異なる ClanId の軍の全組み合わせを生成する。
        /// </summary>
        public IEnumerable<Battle> Find(WorldState world)
        {
            var result = new List<Battle>();

            foreach (var hexGroup in world.Armies.GroupBy(x => x.CurrentHexId))
            {
                var armies = hexGroup.ToList();

                for (int i = 0; i < armies.Count; i++)
                {
                    for (int j = i + 1; j < armies.Count; j++)
                    {
                        if (armies[i].ClanId != armies[j].ClanId
                            && !world.AreAllied(armies[i].ClanId, armies[j].ClanId))
                            result.Add(new Battle(armies[i], armies[j]));
                    }
                }
            }

            return result;
        }
    }
}
