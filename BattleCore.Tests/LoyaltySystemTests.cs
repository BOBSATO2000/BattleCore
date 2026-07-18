using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Relations;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class LoyaltySystemTests
    {
        [TestMethod]
        public void HighAmbitionLowLoyaltyTriggersBetrayalEvent()
        {
            // 野心100・忠誠0・勢力忠誠0 → score=100 → 閾値80超えで離反
            var world = new WorldState();
            var officer = new Officer(1, "野心家") { Ambition = 100, Loyalty = 0 };
            world.Officers.Add(officer);

            var membership = new Membership(1, officer.Id, clanId: 1) { Loyalty = 0 };
            world.Memberships.Add(membership);

            var context = new SimulationContext(world);
            new LoyaltySystem(betrayalThreshold: 80).Update(context);

            Assert.AreEqual(1, context.EventQueue.Count);
            var ev = (BetrayalEvent)context.EventQueue.Dequeue();
            Assert.AreEqual(officer.Id, ev.OfficerId);
            Assert.AreEqual(1, ev.FromClanId);
        }

        [TestMethod]
        public void HighLoyaltyPreventsBetrayal()
        {
            // 野心50・忠誠100・勢力忠誠100 → score=50-50-50=-50 → 離反しない
            var world = new WorldState();
            var officer = new Officer(1, "忠臣") { Ambition = 50, Loyalty = 100 };
            world.Officers.Add(officer);

            var membership = new Membership(1, officer.Id, clanId: 1) { Loyalty = 100 };
            world.Memberships.Add(membership);

            var context = new SimulationContext(world);
            new LoyaltySystem(betrayalThreshold: 80).Update(context);

            Assert.AreEqual(0, context.EventQueue.Count);
            Assert.AreEqual(1, world.Memberships.Count);
        }

        [TestMethod]
        public void BetrayalRemovesMembershipFromWorld()
        {
            // 離反後はMembershipが削除される
            var world = new WorldState();
            var officer = new Officer(1, "裏切り者") { Ambition = 100, Loyalty = 0 };
            world.Officers.Add(officer);
            world.Memberships.Add(new Membership(1, officer.Id, clanId: 1) { Loyalty = 0 });

            var context = new SimulationContext(world);
            new LoyaltySystem(betrayalThreshold: 80).Update(context);

            Assert.AreEqual(0, world.Memberships.Count);
        }

        [TestMethod]
        public void DislikeTowardClanMemberIncreasesScore()
        {
            // 主君への Dislike が高いと裏切りスコアが上がる
            // Ambition=60, Loyalty=60, MembershipLoyalty=60 → base score = 60-30-30 = 0
            // Dislike=100 → score = 100 → 閾値80超えで離反
            var world = new WorldState();

            var officer = new Officer(1, "不満武将") { Ambition = 60, Loyalty = 60 };
            var lord    = new Officer(2, "主君");
            world.Officers.Add(officer);
            world.Officers.Add(lord);

            world.Memberships.Add(new Membership(1, officer.Id, clanId: 1) { Loyalty = 60 });
            world.Memberships.Add(new Membership(2, lord.Id,    clanId: 1));

            // officer → lord への強い反感
            world.Relationships.Add(new Relationship(1, officer.Id, lord.Id)
            {
                Dislike = 100
            });

            var context = new SimulationContext(world);
            new LoyaltySystem(betrayalThreshold: 80).Update(context);

            Assert.AreEqual(1, context.EventQueue.Count);
        }

        [TestMethod]
        public void BetrayalDefectsArmyToNoClan()
        {
            // 離反した武将が指揮する Army の ClanId が 0（無所属）になる
            var world = new WorldState();
            var officer = new Officer(1, "裏切り者") { Ambition = 100, Loyalty = 0 };
            world.Officers.Add(officer);
            world.Memberships.Add(new Membership(1, officer.Id, clanId: 1) { Loyalty = 0 });

            var army = new Army(1, 1, 1, 1);
            army.AssignOfficer(officer.Id);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            new LoyaltySystem(betrayalThreshold: 80).Update(context);

            Assert.AreEqual(0, army.ClanId);
        }

        [TestMethod]
        public void BetrayalReducesAllyLoyalty()
        {
            // 離反発生後、同じ勢力の他の武将の Loyalty が下がる
            var world = new WorldState();

            var traitor = new Officer(1, "裏切り者") { Ambition = 100, Loyalty = 0 };
            var ally    = new Officer(2, "味方武将") { Ambition = 0,   Loyalty = 100 };
            world.Officers.Add(traitor);
            world.Officers.Add(ally);

            world.Memberships.Add(new Membership(1, traitor.Id, clanId: 1) { Loyalty = 0 });
            var allyMembership = new Membership(2, ally.Id, clanId: 1) { Loyalty = 80 };
            world.Memberships.Add(allyMembership);

            var context = new SimulationContext(world);
            new LoyaltySystem(betrayalThreshold: 80, chainBetrayalLoyaltyDrop: 10).Update(context);

            Assert.AreEqual(72, allyMembership.Loyalty);
        }

        [TestMethod]
        public void ArmyAliveIncreasesLoyalty()
        {
            // 勢力の Army が生きていると Loyalty が上がる
            var world = new WorldState();
            var officer = new Officer(1, "忠臣") { Ambition = 0, Loyalty = 50 };
            world.Officers.Add(officer);
            var membership = new Membership(1, officer.Id, clanId: 1) { Loyalty = 50 };
            world.Memberships.Add(membership);

            // 勢力の Army が健在
            world.Armies.Add(new Army(1, 1, 1, 1)); // ClanId=1, Soldiers=1000

            var context = new SimulationContext(world);
            new LoyaltySystem(betrayalThreshold: 200, winLoyaltyBonus: 3).Update(context);

            Assert.AreEqual(55, membership.Loyalty);
        }

        [TestMethod]
        public void AllArmiesDestroyedDecreasesLoyalty()
        {
            // 勢力の Army が全滅すると Loyalty が下がる
            var world = new WorldState();
            var officer = new Officer(1, "武将") { Ambition = 0, Loyalty = 50 };
            world.Officers.Add(officer);
            var membership = new Membership(1, officer.Id, clanId: 1) { Loyalty = 50 };
            world.Memberships.Add(membership);

            // 勢力の Army が全滅
            var army = new Army(1, 1, 1, 1);
            army.LoseSoldiers(1000);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            new LoyaltySystem(betrayalThreshold: 200, lossLoyaltyPenalty: 5).Update(context);

            Assert.AreEqual(47, membership.Loyalty);
        }

        [TestMethod]
        public void SpringSeasonIncreasesLoyalty()
        {
            // 春のシーズンは Loyalty が上がる
            var world = new WorldState();
            var officer = new Officer(1, "武将") { Ambition = 0, Loyalty = 50 };
            world.Officers.Add(officer);
            var membership = new Membership(1, officer.Id, clanId: 1) { Loyalty = 50 };
            world.Memberships.Add(membership);

            var context = new SimulationContext(world);
            // GameTime の初期値は Spring
            Assert.AreEqual(Season.Spring, context.Time.Season);

            new LoyaltySystem(betrayalThreshold: 200, springBonus: 2).Update(context);

            Assert.AreEqual(52, membership.Loyalty);
        }
    }
}
