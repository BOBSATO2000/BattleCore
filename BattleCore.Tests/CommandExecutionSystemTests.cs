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
    public class CommandExecutionSystemTests
    {
        [TestMethod]
        public void MoveCommandMovesArmy()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var army = new Army(1, 1, 1, 1);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            context.CommandQueue.Enqueue(new MoveArmyCommand(1, 2));

            var system = new CommandExecutionSystem();
            system.Update(context);

            // CommandExecutionSystem は OrderMove を呼ぶ
            // MovementSystem が実際に移動させるので destination が設定されていることを確認
            Assert.AreEqual(2, army.DestinationHexId);
        }
    }
}
