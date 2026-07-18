using BattleCore.Events;
using BattleCore.Relations;
using BattleCore.Simulation;
using System;
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
        public int BetrayalThreshold        { get; }
        public int ChainBetrayalLoyaltyDrop { get; }
        public int WinLoyaltyBonus          { get; }
        public int LossLoyaltyPenalty       { get; }
        public int SpringBonus              { get; }

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

        public void Update(SimulationContext context)
        {
            var world = context.World;

            // ① 忠誠変動
            foreach (var membership in world.Memberships)
                ApplyLoyaltyChange(membership, context);

            // ② 裏切り判定
            var memberships = world.Memberships.ToList();
            foreach (var membership in memberships)
            {
                var officer = world.Officers
                    .FirstOrDefault(o => o.Id == membership.OfficerId);

                if (officer == null) continue;

                var score = CalcBetrayalScore(officer, membership, world);

                if (score < BetrayalThreshold) continue;

                world.Memberships.Remove(membership);

                foreach (var army in world.Armies.Where(a => a.OfficerId == officer.Id))
                    army.Defect(newClanId: 0);

                var allies = world.Memberships
                    .Where(m => m.ClanId == membership.ClanId)
                    .ToList();

                foreach (var ally in allies)
                    ally.Loyalty = Math.Max(0, ally.Loyalty - ChainBetrayalLoyaltyDrop);

                context.EventQueue.Enqueue(
                    new BetrayalEvent(officer.Id, membership.ClanId, score));
            }
        }

        private void ApplyLoyaltyChange(Membership membership, SimulationContext context)
        {
            var world = context.World;

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
