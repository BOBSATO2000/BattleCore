using BattleCore.Entities;
using System;

namespace BattleCore.Systems.Battle
{
    /// <summary>
    /// 戦闘ダメージを計算するクラス。
    /// Officer が配属されている場合、能力値による補正を適用する。
    ///
    /// 補正ルール：
    ///   Leadership（統率）: 実効兵力に補正。100を基準に±50%まで変動。
    ///   Strategy（戦術）  : 与ダメージに補正。100を基準に±30%まで変動。
    ///   Courage（武勇）   : 接近戦追加ダメージ。Strategy補正に上乗せ。
    ///
    /// Officer未配属の場合は補正なし（従来と同じ計算）。
    /// </summary>
    public sealed class DamageCalculator : IDamageCalculator
    {
        /// <summary>Officer未配属時に使用するデフォルト能力値。</summary>
        private const int DefaultStat = 100;

        public BattleResult Calculate(Army left, Army right)
            => Calculate(left, right, null, null);

        /// <summary>
        /// Officer能力値を考慮して戦闘結果を計算する。
        /// </summary>
        public BattleResult Calculate(
            Army left,
            Army right,
            Officer? leftOfficer,
            Officer? rightOfficer)
        {
            // 実効兵力 = 兵力 × (Leadership / 100)
            var leftPower  = ApplyLeadership(left.Soldiers,  leftOfficer);
            var rightPower = ApplyLeadership(right.Soldiers, rightOfficer);

            Army winner, loser;
            Officer? winnerOfficer, loserOfficer;
            int loserSoldiers;

            if (leftPower >= rightPower)
            {
                winner = left;  winnerOfficer = leftOfficer;
                loser  = right; loserOfficer  = rightOfficer;
                loserSoldiers = right.Soldiers;
            }
            else
            {
                winner = right; winnerOfficer = rightOfficer;
                loser  = left;  loserOfficer  = leftOfficer;
                loserSoldiers = left.Soldiers;
            }

            // 勝者の損害 = 敗者兵力の1/3 × Strategy補正 × Courage補正
            var winnerLosses = (int)(loserSoldiers / 3.0
                * GetStrategyFactor(winnerOfficer)
                * GetCourageFactor(winnerOfficer));

            winnerLosses = Math.Max(0, winnerLosses);

            return new BattleResult(
                winner:       winner,
                loser:        loser,
                winnerLosses: winnerLosses,
                loserLosses:  loserSoldiers);
        }

        // Leadership が高いほど実効兵力が増える（50〜150%）
        private static double ApplyLeadership(int soldiers, Officer? officer)
        {
            var leadership = officer?.Leadership ?? DefaultStat;
            var factor = Math.Clamp(leadership / 100.0, 0.5, 1.5);
            return soldiers * factor;
        }

        // Strategy が高いほど与ダメージが減る（戦術が巧みなほど損害を抑える）
        // 100基準：70〜130%
        private static double GetStrategyFactor(Officer? officer)
        {
            var strategy = officer?.Strategy ?? DefaultStat;
            return Math.Clamp(strategy / 100.0, 0.7, 1.3);
        }

        // Courage が高いほど接近戦で追加ダメージを与える（損害が増える）
        // 100基準：90〜110%
        private static double GetCourageFactor(Officer? officer)
        {
            var courage = officer?.Courage ?? DefaultStat;
            return Math.Clamp(courage / 100.0, 0.9, 1.1);
        }
    }
}
