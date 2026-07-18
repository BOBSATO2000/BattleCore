using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class VictorySystemTests
    {
        [TestMethod]
        public void SingleActiveClanTriggersGameOver()
        {
            // 兵力が残っている勢力が1つだけ → GameOverEvent発火
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Clans.Add(new Clan(1) { Name = "織田" });
            world.Clans.Add(new Clan(2) { Name = "武田" });

            var a1 = new Army(1, 0, 1, 1);               // 織田: 1000兵
            var a2 = new Army(2, 0, 2, 1); a2.LoseSoldiers(1000); // 武田: 0兵
            world.Armies.AddRange(new[] { a1, a2 });

            var context = new SimulationContext(world);
            new VictorySystem().Update(context);

            var ev = context.EventQueue.OfType<GameOverEvent>().FirstOrDefault();
            Assert.IsNotNull(ev);
            Assert.AreEqual(1, ev.WinnerClanId);
        }

        [TestMethod]
        public void AllClanDestroyedTriggersDrawGameOver()
        {
            // 全勢力が全滅 → WinnerClanId=null
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var a1 = new Army(1, 0, 1, 1); a1.LoseSoldiers(1000);
            var a2 = new Army(2, 0, 2, 1); a2.LoseSoldiers(1000);
            world.Armies.AddRange(new[] { a1, a2 });

            var context = new SimulationContext(world);
            new VictorySystem().Update(context);

            var ev = context.EventQueue.OfType<GameOverEvent>().FirstOrDefault();
            Assert.IsNotNull(ev);
            Assert.IsNull(ev.WinnerClanId);
        }

        [TestMethod]
        public void MultipleActiveClansNoGameOver()
        {
            // 複数勢力が生存中 → GameOverEvent発火しない
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var a1 = new Army(1, 0, 1, 1);
            var a2 = new Army(2, 0, 2, 1);
            world.Armies.AddRange(new[] { a1, a2 });

            var context = new SimulationContext(world);
            new VictorySystem().Update(context);

            Assert.AreEqual(0, context.EventQueue.OfType<GameOverEvent>().Count());
        }

        [TestMethod]
        public void GameOverFiresOnlyOnce()
        {
            // 条件が続いても2回目は発火しない
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Clans.Add(new Clan(1) { Name = "織田" });

            var a1 = new Army(1, 0, 1, 1);
            world.Armies.Add(a1);

            var context = new SimulationContext(world);
            var system = new VictorySystem();
            system.Update(context);
            system.Update(context); // 2回目

            Assert.AreEqual(1, context.EventQueue.OfType<GameOverEvent>().Count());
        }
    }
}
