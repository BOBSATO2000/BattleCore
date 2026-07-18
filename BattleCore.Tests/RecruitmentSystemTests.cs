using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class RecruitmentSystemTests
    {
        [TestMethod]
        public void RoninJoinsNearestClan()
        {
            // 無所属武将が最も近い勢力に仕官する
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var officer = new Officer(1, "浪人") { Ambition = 80 };
            world.Officers.Add(officer);

            // 無所属Army
            var ronin = new Army(1, 1, 0, 1);
            ronin.AssignOfficer(officer.Id);
            world.Armies.Add(ronin);

            // 仕官先の勢力
            var clan = new Clan(1) { Name = "織田" };
            world.Clans.Add(clan);
            var clanArmy = new Army(2, 2, clan.Id, 2);
            world.Armies.Add(clanArmy);

            var context = new SimulationContext(world);
            new RecruitmentSystem(recruitAmbitionThreshold: 30).Update(context);

            Assert.AreEqual(clan.Id, ronin.ClanId);
            Assert.AreEqual(1, context.EventQueue.Count);
            var ev = (RecruitEvent)context.EventQueue.Dequeue();
            Assert.AreEqual(officer.Id, ev.OfficerId);
            Assert.AreEqual(clan.Id, ev.ToClanId);
        }

        [TestMethod]
        public void LowAmbitionRoninDoesNotJoin()
        {
            // Ambitionが低い武将は仕官しない
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var officer = new Officer(1, "隠居") { Ambition = 10 };
            world.Officers.Add(officer);

            var ronin = new Army(1, 1, 0, 1);
            ronin.AssignOfficer(officer.Id);
            world.Armies.Add(ronin);

            var clan = new Clan(1) { Name = "織田" };
            world.Clans.Add(clan);
            world.Armies.Add(new Army(2, 2, clan.Id, 2));

            var context = new SimulationContext(world);
            new RecruitmentSystem(recruitAmbitionThreshold: 30).Update(context);

            Assert.AreEqual(0, ronin.ClanId);
            Assert.AreEqual(0, context.EventQueue.Count);
        }

        [TestMethod]
        public void RecruitAddsNewMembership()
        {
            // 仕官後にMembershipが追加される
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var officer = new Officer(1, "浪人") { Ambition = 80 };
            world.Officers.Add(officer);

            var ronin = new Army(1, 1, 0, 1);
            ronin.AssignOfficer(officer.Id);
            world.Armies.Add(ronin);

            var clan = new Clan(1) { Name = "織田" };
            world.Clans.Add(clan);
            world.Armies.Add(new Army(2, 2, clan.Id, 2));

            var context = new SimulationContext(world);
            new RecruitmentSystem(recruitAmbitionThreshold: 30).Update(context);

            Assert.AreEqual(1, world.Memberships.Count);
            Assert.AreEqual(officer.Id, world.Memberships[0].OfficerId);
            Assert.AreEqual(clan.Id,    world.Memberships[0].ClanId);
        }
    }
}
