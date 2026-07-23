using BattleCore.AI;
using BattleCore.Events;
using BattleCore.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 武将間の関係変動システム。
    /// 毎Step実行され、Army の位置・勢力関係・戦闘結果に応じて Relationship を更新する。
    /// RelationTrigger が登録されている場合、関係値変動後に突発イベントも評価する。
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
        /// <summary>同勢力共闘時の Trust 上昇量。</summary>
        public int AllyTrustGain      { get; }

        /// <summary>異勢力敵対時の Dislike 上昇量。</summary>
        public int EnemyDislikeGain   { get; }

        /// <summary>強い味方への Respect 上昇量。</summary>
        public int RespectGain        { get; }

        /// <summary>放置時の Respect 低下量。</summary>
        public int NeglectRespectLoss { get; }

        /// <summary>戦闘勝利時の Trust 上昇量。</summary>
        public int BattleWinTrustGain { get; }

        /// <summary>戦闘敗北時の主君への Dislike 上昇量。</summary>
        public int BattleLossDislike  { get; }

        /// <summary>「放置」と判定するHex距離の閾値。</summary>
        public int NeglectDistanceThreshold { get; }

        /// <summary>
        /// 2武将間の関係値条件で突発イベントを発火するトリガーリスト。
        /// null または空の場合は突発イベントなし。
        /// </summary>
        private readonly IReadOnlyList<RelationTrigger> _relationTriggers;

        /// <summary>突発イベントの確率判定に使用する乱数インスタンス。</summary>
        private readonly Random _rng;

        /// <summary>
        /// 既存テストとの後方互換用コンストラクタ。RelationTrigger なし・乱数はデフォルト。
        /// </summary>
        public RelationshipSystem(
            int allyTrustGain            = 1,
            int enemyDislikeGain         = 2,
            int respectGain              = 1,
            int neglectRespectLoss       = 1,
            int battleWinTrustGain       = 3,
            int battleLossDislike        = 2,
            int neglectDistanceThreshold = 4)
            : this(allyTrustGain, enemyDislikeGain, respectGain, neglectRespectLoss,
                   battleWinTrustGain, battleLossDislike, neglectDistanceThreshold,
                   triggers: null, seed: null)
        { }

        /// <summary>
        /// RelationTrigger と乱数シードを指定するコンストラクタ。
        /// seed を固定すると再現性のあるテストが可能になる。
        /// </summary>
        public RelationshipSystem(
            int allyTrustGain,
            int enemyDislikeGain,
            int respectGain,
            int neglectRespectLoss,
            int battleWinTrustGain,
            int battleLossDislike,
            int neglectDistanceThreshold,
            IEnumerable<RelationTrigger>? triggers,
            int? seed)
        {
            AllyTrustGain            = allyTrustGain;
            EnemyDislikeGain         = enemyDislikeGain;
            RespectGain              = respectGain;
            NeglectRespectLoss       = neglectRespectLoss;
            BattleWinTrustGain       = battleWinTrustGain;
            BattleLossDislike        = battleLossDislike;
            NeglectDistanceThreshold = neglectDistanceThreshold;
            _relationTriggers        = triggers?.ToList() ?? new List<RelationTrigger>();
            _rng                     = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// 関係値変動を適用し、RelationTrigger の突発イベントを評価する。
        /// </summary>
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
            foreach (var ev in context.EventQueue)
            {
                if (ev is not BattleLogEvent bl) continue;

                var winnerArmy = world.Armies
                    .FirstOrDefault(a => a.OfficerId.HasValue &&
                        world.Officers.Any(o => o.Id == a.OfficerId && o.Name == bl.WinnerName));
                var loserArmy = world.Armies
                    .FirstOrDefault(a => a.OfficerId.HasValue &&
                        world.Officers.Any(o => o.Id == a.OfficerId && o.Name == bl.LoserName));

                if (winnerArmy?.OfficerId.HasValue == true && loserArmy?.OfficerId.HasValue == true)
                {
                    var relWL = world.GetOrCreateRelationship(
                        winnerArmy.OfficerId!.Value, loserArmy.OfficerId!.Value);
                    relWL.Dislike = Math.Min(100, relWL.Dislike + BattleWinTrustGain);

                    var loserClan = world.Clans.FirstOrDefault(c => c.Id == loserArmy.ClanId);
                    if (loserClan?.DaimyoOfficerId.HasValue == true)
                    {
                        var relLD = world.GetOrCreateRelationship(
                            loserArmy.OfficerId!.Value, loserClan.DaimyoOfficerId!.Value);
                        relLD.Dislike = Math.Min(100, relLD.Dislike + BattleLossDislike);
                    }
                }
            }

            // ④ RelationTrigger による突発イベント評価
            if (_relationTriggers.Count == 0) return;

            foreach (var rel in world.Relationships)
            {
                foreach (var trigger in _relationTriggers)
                {
                    if (!trigger.Condition(rel)) continue;
                    if (_rng.NextSingle() >= trigger.Probability) continue;

                    var fromOfficer = world.Officers.FirstOrDefault(o => o.Id == rel.FromOfficerId);
                    var toOfficer   = world.Officers.FirstOrDefault(o => o.Id == rel.ToOfficerId);
                    if (fromOfficer == null || toOfficer == null) continue;

                    context.EventQueue.Enqueue(trigger.EventFactory(fromOfficer, toOfficer));
                }
            }
        }
    }
}
