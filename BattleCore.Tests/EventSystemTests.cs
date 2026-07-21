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
    public class EventSystemTests
    {
        // ── MovementEvent ────────────────────────────────────────

        [TestMethod]
        public void MovementEvent_FiredOnArrival()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var officer = new Officer(1, "信長");
            world.Officers.Add(officer);

            var army = new Army(1, 0, 1, 1);
            army.AssignOfficer(1);
            army.OrderMove(2);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new MovementSystem());

            engine.Step();

            // 到着したので MovementEvent が積まれている
            Assert.AreEqual(1, context.EventQueue.Count);
            var ev = context.EventQueue.Dequeue() as MovementEvent;
            Assert.IsNotNull(ev);
            Assert.AreEqual("信長", ev!.OfficerName);
            Assert.AreEqual(2, ev.HexId);
        }

        [TestMethod]
        public void MovementEvent_NotFiredWhenNotArrived()
        {
            // 3Hex先への移動中（まだ到着していない）
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));

            var army = new Army(1, 0, 1, 1);
            army.OrderMove(3);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new MovementSystem());

            engine.Step(); // Hex1→Hex2（まだ到着していない）

            Assert.AreEqual(0, context.EventQueue.Count);
        }

        [TestMethod]
        public void MovementEvent_OfficerName_FallsBackToArmyId()
        {
            // 指揮官未配属の場合は「軍{Id}」になる
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var army = new Army(5, 0, 1, 1); // OfficerId なし
            army.OrderMove(2);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new MovementSystem());

            engine.Step();

            var ev = context.EventQueue.Dequeue() as MovementEvent;
            Assert.IsNotNull(ev);
            Assert.AreEqual("軍5", ev!.OfficerName);
        }

        // ── SupplyEvent ──────────────────────────────────────────

        [TestMethod]
        public void SupplyEvent_FiredWhenGainExceedsThreshold()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var clan  = new Clan(1) { Name = "織田" };
            world.Clans.Add(clan);

            var officer = new Officer(1, "信長");
            world.Officers.Add(officer);

            // 全滅状態から回復させる（大量補充が発生する）
            var army = new Army(1, 0, 1, 1);
            army.LoseSoldiers(1000); // 0兵
            army.AssignOfficer(1);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            // EventThreshold=50 に下げて確実に発火させる
            engine.Register(new SupplySystem(baseReplenishment: 300, eventThreshold: 200));

            engine.Step();

            var supplyEvents = context.EventQueue
                .OfType<SupplyEvent>().ToList();
            Assert.AreEqual(1, supplyEvents.Count);
            Assert.AreEqual("信長", supplyEvents[0].OfficerName);
            Assert.IsTrue(supplyEvents[0].Amount >= 300); // 春ボーナス込みで300以上
        }

        [TestMethod]
        public void SupplyEvent_NotFiredForSmallGain()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var clan = new Clan(1) { Name = "織田" };
            world.Clans.Add(clan);

            var army = new Army(1, 0, 1, 1);
            army.LoseSoldiers(30); // 970兵（少し減っているだけ）
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            // 基本補充50 < 閾値200 なのでイベントなし
            engine.Register(new SupplySystem(baseReplenishment: 50, eventThreshold: 200));

            engine.Step();

            Assert.AreEqual(0, context.EventQueue.OfType<SupplyEvent>().Count());
        }

        [TestMethod]
        public void SupplyEvent_NotFiredForNeutralArmy()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            // ClanId=0（無所属）の軍は補充されない
            var army = new Army(1, 0, 0, 1);
            army.LoseSoldiers(1000);
            world.Armies.Add(army);

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new SupplySystem(baseReplenishment: 300, eventThreshold: 50));

            engine.Step();

            Assert.AreEqual(0, context.EventQueue.OfType<SupplyEvent>().Count());
        }
    }
}
