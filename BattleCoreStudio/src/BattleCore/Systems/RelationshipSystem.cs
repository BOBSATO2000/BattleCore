using BattleCore.Events;
using BattleCore.Simulation;
using System;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 武将間の関係変動システム。
    /// 毎Step実行され、Army の位置・勢力関係・戦闘結果に応じて Relationship を更新する。
    ///
    /// 変動ルール：
    ///   +Trust   : 同じ勢力・同じHexにいる（共闘）
    ///   +Dislike : 異なる勢力・同じHexにいる（敵対）
    ///   +Respect : 同じ勢力で兵力が多い方の武将（強い味方への尊敬）
    ///   -Respect : 同じ勢力なのに遠く離れている（放置・無関心）
    ///   +Trust   : 戦闘勝利した武将（BattleLogEvent 参照）
    ///   +Dislike : 戦闘敗北した武将（主君への不満）
    /// </summary>
    public class RelationshipSystem : ISimulationSystem
    {
        public int AllyTrustGain      { get; }
        public int EnemyDislikeGain   { get; }
        public int RespectGain        { get; }
        public int NeglectRespectLoss { get; }
        public int BattleWinTrustGain { get; }
        public int BattleLossDislike  { get; }

        /// <summary>「放置」と判定するHex距離の閾値。</summary>
        public int NeglectDistanceThreshold { get; }

        public RelationshipSystem(
            int allyTrustGain          = 1,
            int enemyDislikeGain       = 2,
            int respectGain            = 1,
            int neglectRespectLoss     = 1,
            int battleWinTrustGain     = 3,
            int battleLossDislike      = 2,
            int neglectDistanceThreshold = 4)
        {
            AllyTrustGain            = allyTrustGain;
            EnemyDislikeGain         = enemyDislikeGain;
            RespectGain              = respectGain;
            NeglectRespectLoss       = neglectRespectLoss;
            BattleWinTrustGain       = battleWinTrustGain;
            BattleLossDislike        = battleLossDislike;
            NeglectDistanceThreshold = neglectDistanceThreshold;
        }

        public void Update(SimulationContext context)
        {
            var world = context.World;

            // ① 同Hex共闘・敵対による変動
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

                    var aId = a.OfficerId.Value;
                    var bId = b.OfficerId.Value;

                    if (a.ClanId == b.ClanId)
                    {
                        var relAB = world.GetOrCreateRelationship(aId, bId);
                        var relBA = world.GetOrCreateRelationship(bId, aId);
                        relAB.Trust = Math.Min(100, relAB.Trust + AllyTrustGain);
                        relBA.Trust = Math.Min(100, relBA.Trust + AllyTrustGain);

                        if (a.Soldiers > b.Soldiers)
                            relBA.Respect = Math.Min(100, relBA.Respect + RespectGain);
                        else if (b.Soldiers > a.Soldiers)
                            relAB.Respect = Math.Min(100, relAB.Respect + RespectGain);
                    }
                    else
                    {
                        var relAB = world.GetOrCreateRelationship(aId, bId);
                        var relBA = world.GetOrCreateRelationship(bId, aId);
                        relAB.Dislike = Math.Min(100, relAB.Dislike + EnemyDislikeGain);
                        relBA.Dislike = Math.Min(100, relBA.Dislike + EnemyDislikeGain);
                    }
                }
            }

            // ② 放置（同勢力なのに遠い）→ Respect 低下
            foreach (var clan in world.Clans)
            {
                var clanArmies = world.Armies
                    .Where(a => a.ClanId == clan.Id && a.Soldiers > 0 && a.OfficerId.HasValue)
                    .ToList();

                for (int i = 0; i < clanArmies.Count; i++)
                for (int j = i + 1; j < clanArmies.Count; j++)
                {
                    var a = clanArmies[i];
                    var b = clanArmies[j];
                    var hexA = world.Map.GetHexById(a.CurrentHexId);
                    var hexB = world.Map.GetHexById(b.CurrentHexId);
                    if (hexA == null || hexB == null) continue;

                    if (Map.HexDistance.Calculate(hexA, hexB) >= NeglectDistanceThreshold)
                    {
                        var relAB = world.GetOrCreateRelationship(a.OfficerId!.Value, b.OfficerId!.Value);
                        var relBA = world.GetOrCreateRelationship(b.OfficerId!.Value, a.OfficerId!.Value);
                        relAB.Respect = Math.Max(0, relAB.Respect - NeglectRespectLoss);
                        relBA.Respect = Math.Max(0, relBA.Respect - NeglectRespectLoss);
                    }
                }
            }

            // ③ 戦闘結果による変動（EventQueue の BattleLogEvent を参照）
            // EventQueue は消費しないため、ピーク読み取りで処理する
            foreach (var ev in context.EventQueue)
            {
                if (ev is not BattleLogEvent bl) continue;

                // 勝者の同勢力武将への Trust 上昇
                var winnerArmy = world.Armies
                    .FirstOrDefault(a => a.OfficerId.HasValue &&
                        world.Officers.Any(o => o.Id == a.OfficerId && o.Name == bl.WinnerName));
                var loserArmy = world.Armies
                    .FirstOrDefault(a => a.OfficerId.HasValue &&
                        world.Officers.Any(o => o.Id == a.OfficerId && o.Name == bl.LoserName));

                if (winnerArmy?.OfficerId.HasValue == true && loserArmy?.OfficerId.HasValue == true)
                {
                    // 勝者→敗者の Dislike 上昇（戦場での恨み）
                    var relWL = world.GetOrCreateRelationship(
                        winnerArmy.OfficerId!.Value, loserArmy.OfficerId!.Value);
                    relWL.Dislike = Math.Min(100, relWL.Dislike + BattleWinTrustGain);

                    // 敗者→主君の Dislike 上昇（敗戦の不満）
                    var loserClan = world.Clans.FirstOrDefault(c => c.Id == loserArmy.ClanId);
                    if (loserClan?.DaimyoOfficerId.HasValue == true)
                    {
                        var relLD = world.GetOrCreateRelationship(
                            loserArmy.OfficerId!.Value, loserClan.DaimyoOfficerId!.Value);
                        relLD.Dislike = Math.Min(100, relLD.Dislike + BattleLossDislike);
                    }
                }
            }
        }
    }
}
