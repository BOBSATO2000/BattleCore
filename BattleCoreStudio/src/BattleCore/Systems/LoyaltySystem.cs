using BattleCore.Events;
using BattleCore.Relations;
using BattleCore.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 忠誠変動・裏切り判定システム。
    /// 毎Step実行され、状況に応じて Membership.Loyalty を変動させた後、
    /// 裏切り判定を行う。
    ///
    /// 忠誠変動ルール：
    ///   +WinLoyaltyBonus    : 所属勢力の Army が兵力を保っている（勝ち続けている）
    ///   +SpringBonus        : 季節が春（新年の士気高揚）
    ///   -LossLoyaltyPenalty : 所属勢力の Army が全滅している
    ///
    /// 裏切りスコア計算：
    ///   score = Officer.Ambition
    ///         - Officer.Loyalty / 2
    ///         - Membership.Loyalty / 2
    ///         + 主君への Relationship.Dislike
    ///
    /// score >= BetrayalThreshold で離反イベント発生。
    /// 離反処理：
    ///   1. Membership を削除
    ///   2. その武将が指揮する Army を離反（Defect）させる
    ///   3. 同じ勢力の他の武将の Loyalty を低下させる（連鎖離反）
    /// </summary>
    public class LoyaltySystem : ISimulationSystem
    {
        /// <summary>被反判定の閾値。score がこの値以上になると離反イベントが発生する。</summary>
        public int BetrayalThreshold        { get; }

        /// <summary>連鎖離反時に他の武将の Loyalty を低下させる量。</summary>
        public int ChainBetrayalLoyaltyDrop { get; }

        /// <summary>所属勢力のArmyが生存している場合の Loyalty 上昇量。</summary>
        public int WinLoyaltyBonus          { get; }

        /// <summary>所属勢力のArmyが全滅している場合の Loyalty 低下量。</summary>
        public int LossLoyaltyPenalty       { get; }

        /// <summary>春の季節に適用される Loyalty ボーナス。</summary>
        public int SpringBonus              { get; }

        /// <summary>各パラメータを指定するコンストラクタ。全てデフォルト値あり。</summary>
        public LoyaltySystem(
            int betrayalThreshold        = 80,
            int chainBetrayalLoyaltyDrop = 10,
            int winLoyaltyBonus          = 3,
            int lossLoyaltyPenalty       = 5,
            int springBonus              = 2)
        {
            BetrayalThreshold        = betrayalThreshold;
            ChainBetrayalLoyaltyDrop = chainBetrayalLoyaltyDrop;
            WinLoyaltyBonus          = winLoyaltyBonus;
            LossLoyaltyPenalty       = lossLoyaltyPenalty;
            SpringBonus              = springBonus;
        }

        /// <summary>
        /// 忠誠変動を適用し、被反判定を実行する。
        /// 被反した武将の Membership を削除し、Army を離反させ、連鎖離反を発生させる。
        /// </summary>
        public void Update(SimulationContext context)
        {
            var world = context.World;
            // ① 忠誠変動
            foreach (var membership in world.Memberships.ToList())
                ApplyLoyaltyChange(membership, context);

            // ② 裏切り判定
            var memberships = world.Memberships.ToList();
            var toRemove = new List<Membership>();
            foreach (var membership in memberships)
            {
                if (toRemove.Contains(membership)) continue;

                var officer = world.Officers
                    .FirstOrDefault(o => o.Id == membership.OfficerId);

                if (officer == null) continue;

                var score = CalcBetrayalScore(officer, membership, world);

                if (score < BetrayalThreshold) continue;

                toRemove.Add(membership);

                foreach (var army in world.Armies.Where(a => a.OfficerId == officer.Id))
                    army.Defect(newClanId: 0);

                var allies = world.Memberships
                    .Where(m => m.ClanId == membership.ClanId && !toRemove.Contains(m))
                    .ToList();

                foreach (var ally in allies)
                    ally.Loyalty = Math.Max(0, ally.Loyalty - ChainBetrayalLoyaltyDrop);

                context.EventQueue.Enqueue(
                    new BetrayalEvent(officer.Id, membership.ClanId, score));
            }
            foreach (var m in toRemove)
                world.Memberships.Remove(m);
        }

        /// <summary>
        /// 1件の Membership に対して忠誠変動を適用する。
        /// 所属勢力の Army 状態と季節に応じて Loyalty を増減させる。
        /// </summary>
        private void ApplyLoyaltyChange(Membership membership, SimulationContext context)
        {
            var world = context.World;

            // 士気による忠誠低下（士気が低い武将は心が離れる）
            var officer = world.Officers.FirstOrDefault(o => o.Id == membership.OfficerId);
            if (officer != null)
            {
                var army = world.Armies.FirstOrDefault(a => a.OfficerId == officer.Id);
                if (army != null)
                {
                    if (army.Morale < 10)
                        membership.Loyalty = Math.Max(0, membership.Loyalty - 3);
                    else if (army.Morale < 30)
                        membership.Loyalty = Math.Max(0, membership.Loyalty - 1);
                }
            }

            // 所属勢力の Army 状態を確認
            var clanArmies = world.Armies
                .Where(a => a.ClanId == membership.ClanId)
                .ToList();

            if (clanArmies.Any())
            {
                var allDestroyed = clanArmies.All(a => a.Soldiers == 0);
                var anyAlive     = clanArmies.Any(a => a.Soldiers > 0);

                if (allDestroyed)
                    membership.Loyalty = Math.Max(0,   membership.Loyalty - LossLoyaltyPenalty);
                else if (anyAlive)
                    membership.Loyalty = Math.Min(100, membership.Loyalty + WinLoyaltyBonus);
            }

            // 春ボーナス
            if (context.Time.Season == Season.Spring)
                membership.Loyalty = Math.Min(100, membership.Loyalty + SpringBonus);
        }

        /// <summary>
        /// 武将の被反スコアを計算する。
        /// score = Ambition - Loyalty/2 - Membership.Loyalty/2 + 主君へのDislike合計
        /// </summary>
        private static int CalcBetrayalScore(
            BattleCore.Entities.Officer officer,
            Membership membership,
            BattleCore.World.WorldState world)
        {
            var score = officer.Ambition
                      - officer.Loyalty / 2
                      - membership.Loyalty / 2;

            var dislike = world.Relationships
                .Where(r =>
                    r.FromOfficerId == officer.Id &&
                    world.Memberships.Any(m =>
                        m.OfficerId == r.ToOfficerId &&
                        m.ClanId == membership.ClanId))
                .Sum(r => r.Dislike);

            return score + dislike;
        }
    }
}
