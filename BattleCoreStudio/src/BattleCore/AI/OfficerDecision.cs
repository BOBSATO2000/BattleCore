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
                        new OfficerRefusedOrderEvent(officer.Id, officer.Name, "忠誠が低く命令を拒否した"));
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
                            new OfficerRequestedRetreatEvent(officer.Id, officer.Name, army.Soldiers));
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
                            DecisionReason.IndependentAction);
                        continue;
                    }
                }

                // ④ 主君への不満 → 命令変更（撤退）
                if (IsDissatisfied(officer, clan, world))
                {
                    var retreatTarget = GetRetreatTarget(army, clan, world);
                    if (retreatTarget.HasValue && retreatTarget.Value != army.CurrentHexId)
                    {
                        yield return DecisionResult.Accept(
                            new MoveArmyCommand(army.Id, retreatTarget.Value, DecisionReason.Dissatisfied),
                            DecisionReason.Dissatisfied);
                        continue;
                    }
                }

                // ⑤ 通常
                yield return DecisionResult.Accept(cmd, move.Reason);
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
