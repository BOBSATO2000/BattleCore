using BattleCore.AI;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.Systems.Battle;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class AutoBattleSimulationTests
    {
        private WorldState world = null!;
        private Army army1 = null!;
        private Army army2 = null!;
        private SimulationContext context = null!;
        private SimulationEngine engine = null!;

        [TestInitialize]
        public void Setup()
        {
            world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            army1 = new Army(1, 1, 1, 1);
            army2 = new Army(2, 2, 2, 2);

            world.Armies.Add(army1);
            world.Armies.Add(army2);

            context = new SimulationContext(world);
            engine = new SimulationEngine(context);

            // 正しい順序: Decision → CommandExecution → Movement → Battle
            engine.Register(new DecisionSystem(new SimpleArmyDecision()));
            engine.Register(new CommandExecutionSystem());
            engine.Register(new MovementSystem());
            engine.Register(new BattleSystem());
        }

        [TestMethod]
        public void EnemyArmiesMoveAndBattle()
        {
            engine.Step();

            // 占有ルール導入後：敵Hexへの移動はブロックされるが、
            // どちらかが移動するか、または戦闘トリガーが発火される
            bool moved = army1.CurrentHexId != 1 || army2.CurrentHexId != 2;
            bool battleTriggered = army1.Soldiers < 1000 || army2.Soldiers < 1000;
            Assert.IsTrue(moved || battleTriggered,
                "移動または戦闘が発生するはず");
        }

        [TestMethod]
        public void ArmiesLoseSoldiersAfterContact()
        {
            // 同じHexに配置して戦闘させる
            world.Armies.Clear();
            army1 = new Army(1, 1, 1, 1);
            army2 = new Army(2, 2, 2, 1);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            context = new SimulationContext(world);
            engine = new SimulationEngine(context);
            engine.Register(new BattleSystem());

            var before1 = army1.Soldiers;
            var before2 = army2.Soldiers;

            engine.Step();

            Assert.IsTrue(
                army1.Soldiers < before1 ||
                army2.Soldiers < before2);
        }
    }
}
