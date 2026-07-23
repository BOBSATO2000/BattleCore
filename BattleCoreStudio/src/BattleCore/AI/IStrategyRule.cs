using BattleCore.Entities;
using BattleCore.World;

namespace BattleCore.AI
{
    /// <summary>
    /// 「状況 → CampaignPlan」の生成ルール。
    /// StrategyEvaluator がルール群を優先順に評価し、最初に Matches した Plan を採用する。
    /// ITacticalRule と同じ成功パターン。
    /// </summary>
    public interface IStrategyRule
    {
        /// <summary>この状況でこのルールが適用されるか。</summary>
        bool Matches(Army army, Clan clan, WorldState world);

        /// <summary>適用される場合に生成する CampaignPlan。</summary>
        CampaignPlan CreatePlan(Army army, Clan clan, WorldState world);
    }
}
