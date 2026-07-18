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
    }
}
