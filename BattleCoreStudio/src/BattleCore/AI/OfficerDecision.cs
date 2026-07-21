using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Navigation;
using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.AI
{
    /// <summary>
    /// 武将単位の意思決定フィルタ。
    /// ClanStrategy が生成した MoveArmyCommand を受け取り、
    /// 武将の性格・忠誠・人間関係に基づいて DecisionResult を返す。
    ///
    /// 判断フロー:
    ///   1. 忠誠が極めて低い → 命令拒否（OfficerRefusedOrderEvent）
    ///   2. 慎重な性格 + 兵力不足 → 撤退進言（OfficerRequestedRetreatEvent）
    ///   3. 野心的 + 低忠誠 → 独断行動（最も近い敵城へ）
    ///   4. 主君への不満（Dislike高）→ 命令変更（Dissatisfied）
    ///   5. 上記以外 → 元の命令をそのまま通す
    /// </summary>
    public class OfficerDecision
    {
        private readonly IPathFinder pathFinder = new HexPathFinder();

        public int RefusalLoyaltyThreshold      { get; }
        public int CautiousRetreatSoldiers      { get; }
        public int IndependentActionLoyalty     { get; }
        public int DissatisfiedDislikeThreshold { get; }

        public OfficerDecision(
            int refusalLoyaltyThreshold      = 20,
            int cautiousRetreatSoldiers      = 300,
            int independentActionLoyalty     = 35,
            int dissatisfiedDislikeThreshold = 60)
        {
            RefusalLoyaltyThreshold      = refusalLoyaltyThreshold;
            CautiousRetreatSoldiers      = cautiousRetreatSoldiers;
            IndependentActionLoyalty     = independentActionLoyalty;
            DissatisfiedDislikeThreshold = dissatisfiedDislikeThreshold;
        }

        /// <summary>AiParamsから構築する。</summary>
        public OfficerDecision(AiParams p)
            : this(p.RefusalLoyaltyThreshold, p.CautiousRetreatSoldiers,
                   p.IndependentActionLoyalty, p.DissatisfiedDislikeThreshold) { }

        /// <summary>
        /// 元の命令リストを武将の意思で評価し、DecisionResult のリストを返す。
        /// 各結果は Command（null=拒否）・Reason・Accepted・Event を持つ。
        /// </summary>
        public IEnumerable<DecisionResult> Evaluate(
            IEnumerable<ICommand> originalCommands,
            Clan clan,
            WorldState world)
        {
            foreach (var cmd in originalCommands)
            {
                if (cmd is not MoveArmyCommand move)
                {
                    yield return DecisionResult.Accept(cmd);
                    continue;
                }

                var army    = world.GetArmyById(move.ArmyId);
                var officer = army?.OfficerId.HasValue == true
                    ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId!.Value)
                    : null;

                if (officer == null || army == null)
                {
                    yield return DecisionResult.Accept(cmd);
                    continue;
                }

                var membership = world.Memberships
                    .FirstOrDefault(m => m.OfficerId == officer.Id && m.ClanId == clan.Id);
                int loyalty = membership?.Loyalty ?? officer.Loyalty;

                // ① 忠誠が極めて低い → 命令拒否
                if (loyalty <= RefusalLoyaltyThreshold
                    && officer.Personality != OfficerPersonality.Loyal)
                {
                    yield return DecisionResult.Refuse(
                        new OfficerRefusedOrderEvent(officer.Id, officer.Name, "忠誠が低く命令を拒否した"),
                        DecisionExplanation.Create(DecisionReason.Advance, "命令拒否",
                            $"忠誠:{loyalty}",
                            $"性格:{officer.Personality}",
                            $"兵力:{army.Soldiers}"));
                    continue;
                }

                // ② 慎重な性格 + 兵力不足 → 撤退進言
                if (officer.Personality == OfficerPersonality.Cautious
                    && army.Soldiers <= CautiousRetreatSoldiers)
                {
                    var retreatTarget = GetRetreatTarget(army, clan, world);
                    if (retreatTarget.HasValue && retreatTarget.Value != army.CurrentHexId)
                    {
                        yield return DecisionResult.Accept(
                            new MoveArmyCommand(army.Id, retreatTarget.Value, DecisionReason.CautiousRetreat),
                            DecisionReason.CautiousRetreat,
                            new OfficerRequestedRetreatEvent(officer.Id, officer.Name, army.Soldiers),
                            DecisionExplanation.Create(DecisionReason.CautiousRetreat, "撤退進言",
                                $"兵力:{army.Soldiers}（閾値:{CautiousRetreatSoldiers}）",
                                $"性格:慎重",
                                $"忠誠:{loyalty}"));
                        continue;
                    }
                }

                // ③ 野心的 + 低忠誠 → 独断行動
                if (officer.Personality == OfficerPersonality.Ambitious
                    && loyalty <= IndependentActionLoyalty)
                {
                    var independentTarget = GetIndependentTarget(army, clan, world);
                    if (independentTarget.HasValue)
                    {
                        yield return DecisionResult.Accept(
                            new MoveArmyCommand(army.Id, independentTarget.Value, DecisionReason.IndependentAction),
                            DecisionReason.IndependentAction,
                            explanation: DecisionExplanation.Create(DecisionReason.IndependentAction, "独断行動",
                                $"性格:野心的",
                                $"忠誠:{loyalty}（閾値:{IndependentActionLoyalty}）",
                                $"兵力:{army.Soldiers}"));
                        continue;
                    }
                }

                // ④ 主君への不満 → 命令変更（撤退）
                if (IsDissatisfied(officer, clan, world))
                {
                    var retreatTarget = GetRetreatTarget(army, clan, world);
                    if (retreatTarget.HasValue && retreatTarget.Value != army.CurrentHexId)
                    {
                        var daimyo = clan.DaimyoOfficerId.HasValue
                            ? world.Officers.FirstOrDefault(o => o.Id == clan.DaimyoOfficerId.Value)
                            : null;
                        var rel = clan.DaimyoOfficerId.HasValue
                            ? world.Relationships.FirstOrDefault(r =>
                                r.FromOfficerId == officer.Id && r.ToOfficerId == clan.DaimyoOfficerId.Value)
                            : null;
                        yield return DecisionResult.Accept(
                            new MoveArmyCommand(army.Id, retreatTarget.Value, DecisionReason.Dissatisfied),
                            DecisionReason.Dissatisfied,
                            explanation: DecisionExplanation.Create(DecisionReason.Dissatisfied, "不満による命令変更",
                                $"主君:{daimyo?.Name ?? "?"} への反感:{rel?.Dislike ?? 0}（閾値:{DissatisfiedDislikeThreshold}）",
                                $"忠誠:{loyalty}",
                                $"兵力:{army.Soldiers}"));
                        continue;
                    }
                }

                // ⑤ 通常
                var enemyArmies = world.Armies
                    .Where(a => a.ClanId != clan.Id && a.Soldiers > 0)
                    .OrderBy(a => Map.HexDistance.Calculate(
                        world.Map.GetHexById(army.CurrentHexId)!,
                        world.Map.GetHexById(a.CurrentHexId)!))
                    .FirstOrDefault();
                yield return DecisionResult.Accept(cmd, move.Reason,
                    explanation: DecisionExplanation.Create(move.Reason, ReasonSummary(move.Reason),
                        $"兵力:{army.Soldiers}",
                        $"忠誠:{loyalty}",
                        $"性格:{officer.Personality}",
                        enemyArmies != null ? $"最近敵兵力:{enemyArmies.Soldiers}" : "敵なし"));
            }
        }

        // ── 後方互換用ラッパー（既存テストが Filter() を呼ぶため残す）──────────
        public (List<ICommand> commands, List<IGameEvent> events) Filter(
            IEnumerable<ICommand> originalCommands, Clan clan, WorldState world)
        {
            var cmds   = new List<ICommand>();
            var events = new List<IGameEvent>();
            foreach (var r in Evaluate(originalCommands, clan, world))
            {
                if (r.Accepted && r.Command != null) cmds.Add(r.Command);
                if (r.Event != null) events.Add(r.Event);
            }
            return (cmds, events);
        }

        // ── ヘルパー ─────────────────────────────────────────────

        private static string ReasonSummary(DecisionReason reason) => reason switch
        {
            DecisionReason.Advance          => "進軍",
            DecisionReason.TargetCastle     => "敵城攻略",
            DecisionReason.Retreat          => "撤退",
            DecisionReason.Reinforce        => "友軍救援",
            DecisionReason.CautiousRetreat  => "慎重撤退",
            DecisionReason.IndependentAction => "独断行動",
            DecisionReason.Dissatisfied     => "不満撤退",
            _                               => "不明",
        };

        private int? GetRetreatTarget(Entities.Army army, Clan clan, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId);
            if (currentHex == null) return null;

            return world.Castles
                .Where(c => c.OwnerClanId == clan.Id)
                .OrderBy(c => Map.HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!))
                .FirstOrDefault()?.HexId;
        }

        private int? GetIndependentTarget(Entities.Army army, Clan clan, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId);
            if (currentHex == null) return null;

            var target = world.Castles
                .Where(c => c.OwnerClanId != clan.Id)
                .OrderBy(c => Map.HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!))
                .FirstOrDefault();

            if (target == null) return null;
            var path = pathFinder.FindPath(world.Map, army.CurrentHexId, target.HexId);
            return path.Count > 1 ? path[1] : null;
        }

        private bool IsDissatisfied(Entities.Officer officer, Clan clan, WorldState world)
        {
            if (!clan.DaimyoOfficerId.HasValue) return false;
            var rel = world.Relationships.FirstOrDefault(r =>
                r.FromOfficerId == officer.Id && r.ToOfficerId == clan.DaimyoOfficerId.Value);
            return rel != null && rel.Dislike >= DissatisfiedDislikeThreshold;
        }
    }
}
