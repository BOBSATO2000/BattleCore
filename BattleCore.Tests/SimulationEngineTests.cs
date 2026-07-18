using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class SimulationEngineTests
    {
        [TestMethod]
        public void StepMovesArmy()
        {
            // Arrange
            var world = new WorldState();

            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var army = new Army(1, 1, 1, 1);
            world.Armies.Add(army);

            var context = new SimulationContext(world);

            context.CommandQueue.Enqueue(new MoveArmyCommand(1, 2));

            var engine = new SimulationEngine(context);
            engine.Register(new CommandExecutionSystem());
            engine.Register(new MovementSystem());

            // Act
            engine.Step();

            // Assert
            Assert.AreEqual(2, army.CurrentHexId);
        }

        [TestMethod]
        public void StepAdvancesTime()
        {
            var world = new WorldState();
            var context = new SimulationContext(world);
            var engine = new SimulationEngine(context);

            var before = context.Time.Tick;
            engine.Step();

            Assert.AreEqual(before + 1, context.Time.Tick);
        }
    }
}
