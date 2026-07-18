using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 武将間の関係変動システム。
    /// 毎Step実行され、Army の位置・勢力関係に応じて Relationship を更新する。
    ///
    /// 変動ルール：
    ///   +Trust   : 同じ勢力・同じHexにいる（共闘）
    ///   +Dislike : 異なる勢力・同じHexにいる（敵対）
    ///   +Respect : 同じ勢力で兵力が多い方の武将（強い味方への尊敬）
    ///
    /// 値の上限/下限：0〜100
    /// </summary>
    public class RelationshipSystem : ISimulationSystem
    {
        public int AllyTrustGain   { get; }
        public int EnemyDislikeGain { get; }
        public int RespectGain     { get; }

        public RelationshipSystem(
            int allyTrustGain    = 1,
            int enemyDislikeGain = 2,
            int respectGain      = 1)
        {
            AllyTrustGain    = allyTrustGain;
            EnemyDislikeGain = enemyDislikeGain;
            RespectGain      = respectGain;
        }

        public void Update(SimulationContext context)
        {
            var world = context.World;

            // 同じHexにいるArmyのペアを処理
            foreach (var hexGroup in world.Armies
                .Where(a => a.Soldiers > 0 && a.OfficerId.HasValue)
                .GroupBy(a => a.CurrentHexId))
            {
                var armies = hexGroup.ToList();

                for (int i = 0; i < armies.Count; i++)
                for (int j = i + 1; j < armies.Count; j++)
                {
                    var a = armies[i];
                    var b = armies[j];

                    if (!a.OfficerId.HasValue || !b.OfficerId.HasValue) continue;

                    var aOfficerId = a.OfficerId.Value;
                    var bOfficerId = b.OfficerId.Value;

                    if (a.ClanId == b.ClanId)
                    {
                        // 同じ勢力：Trust上昇
                        var relAB = world.GetOrCreateRelationship(aOfficerId, bOfficerId);
                        var relBA = world.GetOrCreateRelationship(bOfficerId, aOfficerId);

                        relAB.Trust = System.Math.Min(100, relAB.Trust + AllyTrustGain);
                        relBA.Trust = System.Math.Min(100, relBA.Trust + AllyTrustGain);

                        // 兵力が多い方への Respect 上昇
                        if (a.Soldiers > b.Soldiers)
                            relBA.Respect = System.Math.Min(100, relBA.Respect + RespectGain);
                        else if (b.Soldiers > a.Soldiers)
                            relAB.Respect = System.Math.Min(100, relAB.Respect + RespectGain);
                    }
                    else
                    {
                        // 異なる勢力：Dislike上昇
                        var relAB = world.GetOrCreateRelationship(aOfficerId, bOfficerId);
                        var relBA = world.GetOrCreateRelationship(bOfficerId, aOfficerId);

                        relAB.Dislike = System.Math.Min(100, relAB.Dislike + EnemyDislikeGain);
                        relBA.Dislike = System.Math.Min(100, relBA.Dislike + EnemyDislikeGain);
                    }
                }
            }
        }
    }
}
