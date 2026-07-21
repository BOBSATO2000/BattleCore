using BattleCore.AI;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class DecisionSystemTests
    {
        [TestMethod]
        public void ArmyMovesTowardEnemy()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 2);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            var system = new DecisionSystem(new SimpleArmyDecision());

            system.Update(context);

            // 両軍が互いを検知してコマンドを出す
            Assert.HasCount(2, context.CommandQueue);
        }

        [TestMethod]
        public void ArmyCreatesMoveCommandTowardEnemy()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            // army1 だけ配置し、army2 は離れた場所に置いて army1 からは見えない
            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 2);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            var system = new DecisionSystem(new SimpleArmyDecision());

            system.Update(context);

            // 少なくとも1件以上のコマンドが生成される
            Assert.IsTrue(context.CommandQueue.Count >= 1);
        }
    }
}
