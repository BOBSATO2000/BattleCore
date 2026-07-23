using BattleCore.Entities;
using BattleCore.World;
using System.Collections.Generic;

namespace BattleCore.AI
{
    /// <summary>
    /// 戦略評価器。軍ごとの CampaignPlan を生成・更新・破棄する。
    ///
    /// ルールの優先順（登録順）：
    ///   1. WithdrawRule              — 兵力・食糧不足は最優先で撤退
    ///   2. ConsolidateRule           — 自城包囲中は集結
    ///   3. CaptureCastleRule         — 敵城が見えたら攻略
    ///   4. DisruptSupplyRule         — 敵Campが見えたら補給妨害
    ///   5. SecureStrategicPointRule  — 未確保の戦略資源（Well/Shrine）を確保
    ///   6. ReconnaissanceRule        — 敵が見えなければ偵察前進
    /// </summary>
    public sealed class StrategyEvaluator
    {
        private readonly IReadOnlyList<IStrategyRule> rules;

        public StrategyEvaluator()
        {
            rules = new List<IStrategyRule>
            {
                new WithdrawRule(),
                new ConsolidateRule(),
                new CaptureCastleRule(),
                new DisruptSupplyRule(),
                new SecureStrategicPointRule(),
                new ReconnaissanceRule(),
            };
        }

        /// <summary>
        /// 軍の現在の CampaignPlan を評価・更新して返す。
        /// - 既存 Plan が有効なら RemainingTurns を1減らして継続
        /// - 無効なら新しい Plan を生成
        /// - どのルールも Matches しなければ null
        /// </summary>
        public CampaignPlan? Evaluate(Army army, Clan clan, WorldState world)
        {
            world.CampaignPlans.TryGetValue(army.Id, out var existing);

            // 既存 Plan が有効かつ目標が依然として意味を持つなら継続
            if (existing != null && existing.IsValid && IsStillRelevant(existing, clan, world))
            {
                existing.RemainingTurns--;
                return existing;
            }

            // 新しい Plan を生成
            foreach (var rule in rules)
            {
                if (rule.Matches(army, clan, world))
                {
                    var plan = rule.CreatePlan(army, clan, world);
                    world.CampaignPlans[army.Id] = plan;
                    return plan;
                }
            }

            world.CampaignPlans.Remove(army.Id);
            return null;
        }

        private static bool IsStillRelevant(CampaignPlan plan, Clan clan, WorldState world)
        {
            return plan.Goal switch
            {
                CampaignGoal.CaptureCastle =>
                    world.Castles.Any(c => c.HexId == plan.TargetHexId && c.OwnerClanId != clan.Id),
                CampaignGoal.Withdraw =>
                    world.Castles.Any(c => c.OwnerClanId == clan.Id),
                CampaignGoal.DisruptSupply =>
                    world.Structures.Any(s => s.HexId == plan.TargetHexId && s.OwnerClanId != clan.Id),
                CampaignGoal.Consolidate =>
                    world.Castles.Any(c => c.OwnerClanId == clan.Id && c.SiegeTick > 0),
                CampaignGoal.SecureStrategicPoint =>
                    world.Structures.Any(s => s.HexId == plan.TargetHexId && s.OwnerClanId != clan.Id),
                CampaignGoal.Reconnaissance => true,
                _ => false,
            };
        }
    }
}
