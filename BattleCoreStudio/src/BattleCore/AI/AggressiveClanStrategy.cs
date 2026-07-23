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
    /// 視界内に敵がいなければ敵城方向へ前進（索敵行動）する。
    /// 兵力が RetreatThreshold 以下の軍は自勢力の城へ撤退する。
    /// </summary>
    public class AggressiveClanStrategy : IClanStrategy
    {
        private readonly IPathFinder        pathFinder = new HexPathFinder();
        private readonly TacticalEvaluator? evaluator;
        private readonly StrategyEvaluator? strategyEvaluator;

        public int RetreatThreshold { get; }

        /// <summary>
        /// 標準コンストラクタ。TacticalEvaluator/StrategyEvaluator は無効（既存テスト互換）。
        /// </summary>
        public AggressiveClanStrategy(int retreatThreshold = 300)
        {
            RetreatThreshold  = retreatThreshold;
            evaluator         = null;
            strategyEvaluator = null;
        }

        /// <summary>
        /// TacticalEvaluator + StrategyEvaluator を有効にするコンストラクタ。
        /// </summary>
        public AggressiveClanStrategy(int retreatThreshold, TacticalParams tacticalParams)
        {
            RetreatThreshold  = retreatThreshold;
            evaluator         = new TacticalEvaluator(tacticalParams);
            strategyEvaluator = new StrategyEvaluator();
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

                // Layer -1: 戦略評価（有効な場合のみ）
                // TacticalLayer より先に Plan を更新するが、TacticalLayer が割り込める
                CampaignPlan? plan = null;
                if (strategyEvaluator != null)
                    plan = strategyEvaluator.Evaluate(army, clan, world);

                // Layer 0: 戦術評価（有効な場合のみ）
                // TacticalLayer は StrategyLayer の Plan より優先される（食糧切れ・壊滅等）
                if (evaluator != null)
                {
                    var tacticalOrder = evaluator.Evaluate(army, clan, world);
                    if (tacticalOrder != null)
                    {
                        yield return tacticalOrder;
                        continue;
                    }
                }

                // Layer 1: 戦略計画に沿った移動
                if (plan != null)
                {
                    var planPath = pathFinder.FindPath(world.Map, army.CurrentHexId, plan.TargetHexId);
                    if (planPath.Count > 1)
                    {
                        yield return new MoveArmyCommand(army.Id, planPath[1], DecisionReason.Advance);
                        continue;
                    }
                }

                var visibleHexes = world.Visions.TryGetValue(army.Id, out var vision)
                    ? vision.VisibleHexes
                    : new HashSet<int>();

                var visibleEnemies = world.Armies
                    .Where(a => a.ClanId != clan.Id
                             && a.Soldiers > 0
                             && !world.AreAllied(clan.Id, a.ClanId)
                             && visibleHexes.Contains(a.CurrentHexId))
                    .ToList();

                // IsDecoy=true の敵軍を優先ターゲットにする（陽動効果）
                var decoys = visibleEnemies.Where(e => e.IsDecoy).ToList();
                if (decoys.Any()) visibleEnemies = decoys;

                var visibleEnemyCastles = world.Castles
                    .Where(c => c.OwnerClanId != clan.Id
                             && visibleHexes.Contains(c.HexId))
                    .ToList();

                // 視界内に敵がいなければIntelを参照し、最後に判明した敵位置へ向かう
                if (!visibleEnemies.Any() && !visibleEnemyCastles.Any())
                {
                    var intelTarget = GetIntelTarget(army, clan, world, currentHex);
                    if (intelTarget.HasValue)
                    {
                        var intelPath = pathFinder.FindPath(world.Map, army.CurrentHexId, intelTarget.Value);
                        if (intelPath.Count > 1)
                        {
                            yield return new MoveArmyCommand(army.Id, intelPath[1], DecisionReason.Advance);
                            continue;
                        }
                    }

                    // Intelもなければ敵城方向へ前進（索敵行動）
                    var enemyCastles = world.Castles
                        .Where(c => c.OwnerClanId != clan.Id
                                 && !world.AreAllied(clan.Id, c.OwnerClanId))
                        .ToList();
                    if (!enemyCastles.Any()) continue;

                    var nearest = enemyCastles
                        .Select(c => new
                        {
                            c.HexId,
                            Dist = HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!)
                        })
                        .OrderBy(x => x.Dist)
                        .First();

                    var scoutPath = pathFinder.FindPath(world.Map, army.CurrentHexId, nearest.HexId);
                    if (scoutPath.Count > 1)
                        yield return new MoveArmyCommand(army.Id, scoutPath[1]);
                    continue;
                }

                // 同Hexに敵がいれば移動命令不要（BattleSystemが処理）
                if (visibleEnemies.Any(e => e.CurrentHexId == army.CurrentHexId))
                    continue;

                // 撤退
                if (army.Soldiers <= RetreatThreshold)
                {
                    var target = GetRetreatTarget(army, currentHex, myCastles, visibleEnemies, world);
                    if (target != null && target != army.CurrentHexId)
                        yield return new MoveArmyCommand(army.Id, target.Value);
                    continue;
                }

                var nearestCastle = visibleEnemyCastles
                    .Select(c => new
                    {
                        c.HexId,
                        Dist = HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!)
                    })
                    .OrderBy(x => x.Dist)
                    .FirstOrDefault();

                var nearestEnemy = visibleEnemies
                    .Select(e => new
                    {
                        e.CurrentHexId,
                        Dist = HexDistance.Calculate(currentHex, world.Map.GetHexById(e.CurrentHexId)!)
                    })
                    .OrderBy(x => x.Dist)
                    .FirstOrDefault();

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

            if (!visibleEnemies.Any()) return null;

            return world.Map.Hexes
                .Where(h => h.Terrain != TerrainType.Mountain)
                .OrderByDescending(h =>
                    visibleEnemies.Min(e =>
                        HexDistance.Calculate(h, world.Map.GetHexById(e.CurrentHexId)!)))
                .FirstOrDefault()?.Id;
        }

        /// <summary>
        /// Intel情報から最後に判明した敵位置を返す。
        /// Visionで見えない場合に参照する。
        /// </summary>
        private static int? GetIntelTarget(Army army, Clan clan, WorldState world, Hex currentHex)
        {
            var knownEnemies = world.Intel
                .Where(kv => kv.Key.ownerClanId == clan.Id)
                .Select(kv => kv.Value)
                .Where(d => world.Armies.Any(a =>
                    a.Id == d.EnemyArmyId &&
                    a.Soldiers > 0 &&
                    a.ClanId != clan.Id))
                .ToList();

            if (!knownEnemies.Any()) return null;

            return knownEnemies
                .Select(d => new
                {
                    d.LastKnownHexId,
                    Hex  = world.Map.GetHexById(d.LastKnownHexId),
                })
                .Where(x => x.Hex != null)
                .OrderBy(x => HexDistance.Calculate(currentHex, x.Hex!))
                .FirstOrDefault()?.LastKnownHexId;
        }
    }
}
