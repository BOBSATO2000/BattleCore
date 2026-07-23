using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.World;
using System.Collections.Generic;

namespace BattleCore.AI
{
    /// <summary>
    /// 戦術評価器。登録されたルール群を優先順に評価し、
    /// 最初に Matches したルールの Order を返す。
    ///
    /// ルールの優先順（登録順）：
    ///   1. SupplyRule       — 食糧不足は最優先
    ///   2. RetreatRule      — 兵力不足は撤退
    ///   3. GarrisonRule     — 包囲中は籠城
    ///   4. LowMoraleDefend  — 士気低下は防御
    ///   5. EntrenchRule     — 地形を活かした塹壕
    ///   6. FortifyRule      — 前線築城
    ///   7. BuildCampRule    — 補給拠点建設
    ///   8. ScoutRule        — 情報収集（最後）
    ///
    /// null が返った場合は通常の移動命令（AggressiveClanStrategy が担当）。
    /// </summary>
    public sealed class TacticalEvaluator
    {
        private readonly IReadOnlyList<ITacticalRule> rules;
        private readonly TacticalParams              @params;

        public TacticalEvaluator(TacticalParams? @params = null)
        {
            this.@params = @params ?? TacticalParams.Default;
            rules = new List<ITacticalRule>
            {
                new SupplyRule(),
                new RetreatRule(),
                new GarrisonRule(),
                new LowMoraleDefendRule(),
                new BurnFieldRule(),
                new AmbushRule(),
                new EntrenchRule(),
                new FortifyRule(),
                new BuildCampRule(),
                new ScoutRule(),
            };
        }

        /// <summary>
        /// 状況を評価し、最初に Matches したルールの Order を返す。
        /// どのルールも Matches しない場合は null（通常移動）。
        /// </summary>
        public ICommand? Evaluate(Army army, Clan clan, WorldState world)
        {
            var situation = new TacticalSituation(army, clan, world);

            foreach (var rule in rules)
            {
                if (rule.Matches(situation, @params))
                    return rule.CreateOrder(situation);
            }

            return null;
        }
    }
}
