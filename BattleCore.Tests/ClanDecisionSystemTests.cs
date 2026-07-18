using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.Systems.Battle;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class ClanDecisionSystemTests
    {
        [TestMethod]
        public void ClanIssuesMoveCommandTowardEnemy()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));

            var clanA = new Clan(1) { Name = "織田" };
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.Add(clanA);
            world.Clans.Add(clanB);

            var armyA = new Army(1, 1, clanA.Id, 1);
            var armyB = new Army(2, 2, clanB.Id, 3);
            world.Armies.Add(armyA);
            world.Armies.Add(armyB);

            var context = new SimulationContext(world);
            var system = new ClanDecisionSystem(new AggressiveClanStrategy());

            system.Update(context);

            // 各 Clan の Army 分のコマンドが生成される
            Assert.AreEqual(2, context.CommandQueue.Count);
        }

        [TestMethod]
        public void ClanDoesNotMoveWhenNoEnemy()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var clan = new Clan(1) { Name = "織田" };
            world.Clans.Add(clan);

            var army = new Army(1, 1, clan.Id, 1);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            var system = new ClanDecisionSystem(new AggressiveClanStrategy());

            system.Update(context);

            // 敵がいないのでコマンドなし
            Assert.AreEqual(0, context.CommandQueue.Count);
        }

        [TestMethod]
        public void ClanDoesNotMoveWhenEnemyOnSameHex()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var clanA = new Clan(1) { Name = "織田" };
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.Add(clanA);
            world.Clans.Add(clanB);

            var armyA = new Army(1, 1, clanA.Id, 1);
            var armyB = new Army(2, 2, clanB.Id, 1); // 同じ Hex
            world.Armies.Add(armyA);
            world.Armies.Add(armyB);

            var context = new SimulationContext(world);
            var system = new ClanDecisionSystem(new AggressiveClanStrategy());

            system.Update(context);

            // 同 Hex なので移動命令なし（BattleSystem に任せる）
            Assert.AreEqual(0, context.CommandQueue.Count);
        }

        [TestMethod]
        public void FullSimulation_ClansAutoFightEachOther()
        {
            // ClanDecisionSystem を使った自動戦争シミュレーション
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));

            var clanA = new Clan(1) { Name = "織田" };
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.Add(clanA);
            world.Clans.Add(clanB);

            var armyA = new Army(1, 1, clanA.Id, 1);
            var armyB = new Army(2, 2, clanB.Id, 3);
            world.Armies.Add(armyA);
            world.Armies.Add(armyB);

            var context = new SimulationContext(world);
            var engine = new SimulationEngine(context);

            engine.Register(new ClanDecisionSystem(new AggressiveClanStrategy()));
            engine.Register(new CommandExecutionSystem());
            engine.Register(new MovementSystem());
            engine.Register(new BattleSystem());

            // 3ターン進める（Hex1→Hex2→Hex3 で接触するはず）
            engine.Step();
            engine.Step();
            engine.Step();

            // どちらかの兵力が減っている（戦闘が発生した）
            Assert.IsTrue(
                armyA.Soldiers < 1000 ||
                armyB.Soldiers < 1000);
        }
        [TestMethod]
        public void WeakArmyRetreatsFromEnemy()
        {
            // 兵力が閾値以下の軍は移動命令を出す（撤退方向）
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));

            var clanA = new Clan(1) { Name = "織田" };
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.AddRange(new[] { clanA, clanB });

            // armyAは兵力200（閾値300以下）、Hex2に配置
            var armyA = new Army(1, 0, clanA.Id, 2); armyA.LoseSoldiers(800); // 200兵
            var armyB = new Army(2, 0, clanB.Id, 3);
            world.Armies.AddRange(new[] { armyA, armyB });

            var strategy = new AggressiveClanStrategy(retreatThreshold: 300);
            var commands = strategy.Decide(clanA, world).ToList();

            // 撤退命令が1件出る
            Assert.AreEqual(1, commands.Count);
            Assert.IsInstanceOfType(commands[0], typeof(MoveArmyCommand));
        }

        [TestMethod]
        public void StrongArmyAdvancesTowardEnemy()
        {
            // 兵力が閾値超えの軍は敵へ進軍命令を出す
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));

            var clanA = new Clan(1) { Name = "織田" };
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.AddRange(new[] { clanA, clanB });

            var armyA = new Army(1, 0, clanA.Id, 1); // 1000兵
            var armyB = new Army(2, 0, clanB.Id, 3);
            world.Armies.AddRange(new[] { armyA, armyB });

            var strategy = new AggressiveClanStrategy(retreatThreshold: 300);
            var commands = strategy.Decide(clanA, world).ToList();

            // 進軍命令が1件出る
            Assert.AreEqual(1, commands.Count);
            Assert.IsInstanceOfType(commands[0], typeof(MoveArmyCommand));
        }
    }
}
