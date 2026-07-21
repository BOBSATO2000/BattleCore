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
    /// 武将の性格・忠誠・人間関係に基づいて命令を変更・拒否・独断行動に変える。
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

        /// <summary>命令拒否が起きる忠誠の上限値。</summary>
        public int RefusalLoyaltyThreshold { get; }

        /// <summary>慎重性格が撤退進言する兵力の上限値。</summary>
        public int CautiousRetreatSoldiers { get; }

        /// <summary>野心的独断行動が起きる忠誠の上限値。</summary>
        public int IndependentActionLoyalty { get; }

        /// <summary>主君への不満で命令変更が起きる Dislike の下限値。</summary>
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
        /// 元の命令リストを武将の意思でフィルタリングし、
        /// 変更後の命令リストとイベントリストを返す。
        /// </summary>
        public (List<ICommand> commands, List<IGameEvent> events) Filter(
            IEnumerable<ICommand> originalCommands,
            Clan clan,
            WorldState world)
        {
            var resultCommands = new List<ICommand>();
            var resultEvents   = new List<IGameEvent>();

            foreach (var cmd in originalCommands)
            {
                if (cmd is not MoveArmyCommand move)
                {
                    resultCommands.Add(cmd);
                    continue;
                }

                var army    = world.GetArmyById(move.ArmyId);
                var officer = army?.OfficerId.HasValue == true
                    ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId!.Value)
                    : null;

                // 指揮官なし → そのまま通す
                if (officer == null || army == null)
                {
                    resultCommands.Add(cmd);
                    continue;
                }

                var membership = world.Memberships
                    .FirstOrDefault(m => m.OfficerId == officer.Id && m.ClanId == clan.Id);
                int loyalty = membership?.Loyalty ?? officer.Loyalty;

                // ① 忠誠が極めて低い → 命令拒否
                if (loyalty <= RefusalLoyaltyThreshold
                    && officer.Personality != OfficerPersonality.Loyal)
                {
                    resultEvents.Add(new OfficerRefusedOrderEvent(
                        officer.Id, officer.Name, "忠誠が低く命令を拒否した"));
                    continue; // 命令を破棄
                }

                // ② 慎重な性格 + 兵力不足 → 撤退進言
                if (officer.Personality == OfficerPersonality.Cautious
                    && army.Soldiers <= CautiousRetreatSoldiers)
                {
                    var retreatTarget = GetRetreatTarget(army, clan, world);
                    if (retreatTarget.HasValue && retreatTarget.Value != army.CurrentHexId)
                    {
                        resultEvents.Add(new OfficerRequestedRetreatEvent(
                            officer.Id, officer.Name, army.Soldiers));
                        resultCommands.Add(new MoveArmyCommand(
                            army.Id, retreatTarget.Value, DecisionReason.CautiousRetreat));
                        continue;
                    }
                }

                // ③ 野心的 + 低忠誠 → 独断行動（最も近い敵城へ）
                if (officer.Personality == OfficerPersonality.Ambitious
                    && loyalty <= IndependentActionLoyalty)
                {
                    var independentTarget = GetIndependentTarget(army, clan, world);
                    if (independentTarget.HasValue)
                    {
                        resultCommands.Add(new MoveArmyCommand(
                            army.Id, independentTarget.Value, DecisionReason.IndependentAction));
                        continue;
                    }
                }

                // ④ 主君への不満（Dislike高）→ 命令変更（撤退）
                if (IsDissatisfied(officer, clan, world))
                {
                    var retreatTarget = GetRetreatTarget(army, clan, world);
                    if (retreatTarget.HasValue && retreatTarget.Value != army.CurrentHexId)
                    {
                        resultCommands.Add(new MoveArmyCommand(
                            army.Id, retreatTarget.Value, DecisionReason.Dissatisfied));
                        continue;
                    }
                }

                // ⑤ 通常 → 元の命令をそのまま通す
                resultCommands.Add(cmd);
            }

            return (resultCommands, resultEvents);
        }

        // ── ヘルパー ─────────────────────────────────────────────

        private int? GetRetreatTarget(Entities.Army army, Clan clan, WorldState world)
        {
            var myCastles = world.Castles
                .Where(c => c.OwnerClanId == clan.Id)
                .ToList();

            if (myCastles.Any())
            {
                var currentHex = world.Map.GetHexById(army.CurrentHexId);
                if (currentHex == null) return null;

                return myCastles
                    .OrderBy(c => Map.HexDistance.Calculate(
                        currentHex, world.Map.GetHexById(c.HexId)!))
                    .First().HexId;
            }

            return null;
        }

        private int? GetIndependentTarget(Entities.Army army, Clan clan, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId);
            if (currentHex == null) return null;

            // 最も近い敵城（自勢力以外）を独断で狙う
            var target = world.Castles
                .Where(c => c.OwnerClanId != clan.Id)
                .Select(c => new
                {
                    c.HexId,
                    Dist = Map.HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!)
                })
                .OrderBy(x => x.Dist)
                .FirstOrDefault();

            if (target == null) return null;

            var path = pathFinder.FindPath(world.Map, army.CurrentHexId, target.HexId);
            return path.Count > 1 ? path[1] : null;
        }

        private bool IsDissatisfied(Entities.Officer officer, Clan clan, WorldState world)
        {
            // 主君（DaimyoOfficerId）への Dislike が閾値以上
            var daimyoId = clan.DaimyoOfficerId;
            if (!daimyoId.HasValue) return false;

            var rel = world.Relationships
                .FirstOrDefault(r =>
                    r.FromOfficerId == officer.Id &&
                    r.ToOfficerId   == daimyoId.Value);

            return rel != null && rel.Dislike >= DissatisfiedDislikeThreshold;
        }
    }
}
