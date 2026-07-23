namespace BattleCore.AI
{
    /// <summary>
    /// 1軍が数ターンかけて実行する作戦計画。
    /// StrategyEvaluator が生成し、WorldState.CampaignPlans に保持される。
    /// AggressiveClanStrategy が参照して移動先を決定する。
    /// </summary>
    public sealed class CampaignPlan
    {
        /// <summary>作戦目標の種類。</summary>
        public CampaignGoal Goal { get; }

        /// <summary>目標地点のHexId。</summary>
        public int TargetHexId { get; }

        /// <summary>計画の残りターン数。毎ターン1ずつ減る。0になると破棄。</summary>
        public int RemainingTurns { get; set; }

        public CampaignPlan(CampaignGoal goal, int targetHexId, int durationTurns)
        {
            Goal           = goal;
            TargetHexId    = targetHexId;
            RemainingTurns = durationTurns;
        }

        /// <summary>
        /// 計画がまだ有効かどうか。
        /// RemainingTurns > 0 かつ目標が意味を持つ間は true。
        /// </summary>
        public bool IsValid => RemainingTurns > 0;
    }
}
