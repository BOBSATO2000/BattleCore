using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class SupplySystemTests
    {
        [TestMethod]
        public void ArmyReplenishesSoldiers()
        {
            // 兵力が減った軍が補充される
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army = new Army(1, 1, 1, 1);
            army.LoseSoldiers(500); // 500兵
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            new SupplySystem(baseReplenishment: 50, springBonus: 0).Update(context);

            Assert.AreEqual(550, army.Soldiers);
        }

        [TestMethod]
        public void ArmyDoesNotExceedMaxSoldiers()
        {
            // 上限を超えない
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army = new Army(1, 1, 1, 1);
            army.LoseSoldiers(10); // 990兵
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            new SupplySystem(baseReplenishment: 50, springBonus: 0, maxSoldiers: 1000).Update(context);

            Assert.AreEqual(1000, army.Soldiers);
        }

        [TestMethod]
        public void RoninArmyDoesNotReplenish()
        {
            // 無所属（ClanId=0）は補充しない
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army = new Army(1, 1, 0, 1); // ClanId=0
            army.LoseSoldiers(500);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            new SupplySystem(baseReplenishment: 50, springBonus: 0).Update(context);

            Assert.AreEqual(500, army.Soldiers);
        }

        [TestMethod]
        public void DestroyedArmyDoesNotRebuild()
        {
            // 全滅した軍は補充しない
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army = new Army(1, 1, 1, 1);
            army.LoseSoldiers(1000); // 全滅
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            new SupplySystem(baseReplenishment: 50, springBonus: 0).Update(context);

            Assert.AreEqual(0, army.Soldiers);
        }

        [TestMethod]
        public void SpringIncreasesReplenishment()
        {
            // 春は補充量が増える
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army = new Army(1, 1, 1, 1);
            army.LoseSoldiers(500);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            // GameTimeの初期値はSpring
            Assert.AreEqual(Season.Spring, context.Time.Season);

            new SupplySystem(baseReplenishment: 50, springBonus: 30).Update(context);

            Assert.AreEqual(580, army.Soldiers);
        }
    }
}
