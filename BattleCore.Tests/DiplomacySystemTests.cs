using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.Battle;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class DiplomacySystemTests
    {
        [TestMethod]
        public void AlliedClansDoNotFight()
        {
            // 同盟中の勢力は同じHexにいても戦闘しない
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            world.Clans.Add(new Clan(1) { Name = "織田" });
            world.Clans.Add(new Clan(2) { Name = "上杉" });
            world.Alliances.Add(new Alliance(1, 1, 2, durationTicks: 10));

            var a1 = new Army(1, 0, 1, 1);
            var a2 = new Army(2, 0, 2, 1);
            world.Armies.AddRange(new[] { a1, a2 });

            var battles = new BattleFinder().Find(world).ToList();
            Assert.IsEmpty(battles);
        }

        [TestMethod]
        public void NonAlliedClansDoFight()
        {
            // 同盟なしの勢力は同じHexで戦闘する
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var a1 = new Army(1, 0, 1, 1);
            var a2 = new Army(2, 0, 2, 1);
            world.Armies.AddRange(new[] { a1, a2 });

            var battles = new BattleFinder().Find(world).ToList();
            Assert.HasCount(1, battles);
        }

        [TestMethod]
        public void AllianceExpiresAfterDuration()
        {
            // 同盟はRemainingTicks=1のとき1Step後に解消される
            var world = new WorldState();
            world.Clans.Add(new Clan(1) { Name = "織田" });
            world.Clans.Add(new Clan(2) { Name = "上杉" });
            world.Alliances.Add(new Alliance(1, 1, 2, durationTicks: 1));

            var context = new SimulationContext(world);
            new DiplomacySystem().Update(context);

            Assert.IsEmpty(world.Alliances);
            Assert.IsTrue(context.EventQueue.OfType<ScenarioEvent>().Any());
        }

        [TestMethod]
        public void AllianceRemainsWhileActive()
        {
            // 残りTickが残っている間は同盟が維持される
            var world = new WorldState();
            world.Clans.Add(new Clan(1) { Name = "織田" });
            world.Clans.Add(new Clan(2) { Name = "上杉" });
            world.Alliances.Add(new Alliance(1, 1, 2, durationTicks: 5));

            var context = new SimulationContext(world);
            new DiplomacySystem().Update(context);

            Assert.HasCount(1, world.Alliances);
            Assert.AreEqual(4, world.Alliances[0].RemainingTicks);
        }
    }
}
