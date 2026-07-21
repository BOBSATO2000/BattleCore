using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Scenario;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class V08Tests
    {
        // ── DecisionResult ───────────────────────────────────────

        [TestMethod]
        public void DecisionResult_Accept_HasCommand()
        {
            var cmd    = new MoveArmyCommand(1, 2);
            var result = DecisionResult.Accept(cmd, DecisionReason.Advance);

            Assert.IsTrue(result.Accepted);
            Assert.IsNotNull(result.Command);
            Assert.AreEqual(DecisionReason.Advance, result.Reason);
            Assert.IsNull(result.Event);
        }

        [TestMethod]
        public void DecisionResult_Refuse_HasNoCommand()
        {
            var ev     = new OfficerRefusedOrderEvent(1, "光秀", "忠誠が低い");
            var result = DecisionResult.Refuse(ev);

            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.Command);
            Assert.IsNotNull(result.Event);
        }

        [TestMethod]
        public void DecisionResult_Accept_WithEvent_HasBoth()
        {
            var cmd = new MoveArmyCommand(1, 2);
            var ev  = new OfficerRequestedRetreatEvent(1, "信玄", 200);
            var result = DecisionResult.Accept(cmd, DecisionReason.CautiousRetreat, ev);

            Assert.IsTrue(result.Accepted);
            Assert.IsNotNull(result.Command);
            Assert.IsNotNull(result.Event);
            Assert.AreEqual(DecisionReason.CautiousRetreat, result.Reason);
        }

        // ── OfficerDecision.Evaluate() ───────────────────────────

        [TestMethod]
        public void Evaluate_RefusedOrder_ReturnsNotAccepted()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var clan    = new Clan(1) { Name = "織田" };
            var officer = new Officer(1, "光秀") { Personality = OfficerPersonality.Ambitious, Loyalty = 10 };
            var army    = new Army(1, 0, 1, 1);
            army.AssignOfficer(1);
            world.Clans.Add(clan);
            world.Officers.Add(officer);
            world.Armies.Add(army);
            world.Memberships.Add(new Membership(1, 1, 1) { Loyalty = 10 });

            var od      = new OfficerDecision(refusalLoyaltyThreshold: 20);
            var results = od.Evaluate(new[] { new MoveArmyCommand(1, 2) }, clan, world).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.IsFalse(results[0].Accepted);
            Assert.IsInstanceOfType(results[0].Event, typeof(OfficerRefusedOrderEvent));
        }

        [TestMethod]
        public void Evaluate_CautiousRetreat_HasEvent()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));

            var clan    = new Clan(1) { Name = "武田" };
            var officer = new Officer(1, "信玄") { Personality = OfficerPersonality.Cautious };
            var army    = new Army(1, 0, 1, 1);
            army.AssignOfficer(1);
            army.LoseSoldiers(800); // 200兵
            world.Clans.Add(clan);
            world.Officers.Add(officer);
            world.Armies.Add(army);
            world.Memberships.Add(new Membership(1, 1, 1) { Loyalty = 80 });
            world.Castles.Add(new Castle(1, "自城", 3, 1, 50));

            var od      = new OfficerDecision(cautiousRetreatSoldiers: 300);
            var results = od.Evaluate(new[] { new MoveArmyCommand(1, 2) }, clan, world).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].Accepted);
            Assert.AreEqual(DecisionReason.CautiousRetreat, results[0].Reason);
            Assert.IsInstanceOfType(results[0].Event, typeof(OfficerRequestedRetreatEvent));
        }

        // ── RelationshipSystem 放置 ──────────────────────────────

        [TestMethod]
        public void RelationshipSystem_Neglect_DecreasesRespect()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 5, 0)); // 距離5（閾値4以上）

            var clan = new Clan(1) { Name = "織田" };
            world.Clans.Add(clan);

            var o1 = new Officer(1, "信長");
            var o2 = new Officer(2, "光秀");
            world.Officers.AddRange(new[] { o1, o2 });

            var a1 = new Army(1, 0, 1, 1); a1.AssignOfficer(1);
            var a2 = new Army(2, 0, 1, 2); a2.AssignOfficer(2);
            world.Armies.AddRange(new[] { a1, a2 });

            // 初期Respect=50
            var rel12 = world.GetOrCreateRelationship(1, 2); rel12.Respect = 50;
            var rel21 = world.GetOrCreateRelationship(2, 1); rel21.Respect = 50;

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new RelationshipSystem(neglectDistanceThreshold: 4));
            engine.Step();

            Assert.IsTrue(rel12.Respect < 50);
            Assert.IsTrue(rel21.Respect < 50);
        }

        // ── EventTriggerSystem 隣接条件 ──────────────────────────

        [TestMethod]
        public void EventTrigger_AdjacentCondition_FiresWhenAdjacent()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var o1 = new Officer(1, "信長");
            var o2 = new Officer(2, "光秀");
            world.Officers.AddRange(new[] { o1, o2 });

            // 隣接配置
            var a1 = new Army(1, 0, 1, 1); a1.AssignOfficer(1);
            var a2 = new Army(2, 0, 2, 2); a2.AssignOfficer(2);
            world.Armies.AddRange(new[] { a1, a2 });

            var triggers = new List<EventTriggerData>
            {
                new EventTriggerData
                {
                    Id = "adjacent_test",
                    MinTick = 0,
                    OfficerId = 2,
                    AdjacentToOfficerId = 1,
                    Message = "光秀が信長に接近！"
                }
            };

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new EventTriggerSystem(triggers));
            engine.Step();

            var scenarioEvents = context.EventQueue.OfType<ScenarioEvent>().ToList();
            Assert.AreEqual(1, scenarioEvents.Count);
            Assert.AreEqual("光秀が信長に接近！", scenarioEvents[0].Message);
        }

        [TestMethod]
        public void EventTrigger_AdjacentCondition_DoesNotFireWhenFar()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 3, 0)); // 距離3（隣接していない）

            var o1 = new Officer(1, "信長");
            var o2 = new Officer(2, "光秀");
            world.Officers.AddRange(new[] { o1, o2 });

            var a1 = new Army(1, 0, 1, 1); a1.AssignOfficer(1);
            var a2 = new Army(2, 0, 2, 2); a2.AssignOfficer(2);
            world.Armies.AddRange(new[] { a1, a2 });

            var triggers = new List<EventTriggerData>
            {
                new EventTriggerData
                {
                    Id = "adjacent_test",
                    MinTick = 0,
                    OfficerId = 2,
                    AdjacentToOfficerId = 1,
                    Message = "光秀が信長に接近！"
                }
            };

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new EventTriggerSystem(triggers));
            engine.Step();

            Assert.AreEqual(0, context.EventQueue.OfType<ScenarioEvent>().Count());
        }

        // ── ScenarioLoader Personality 読み込み ──────────────────

        [TestMethod]
        public void ScenarioLoader_LoadsPersonality()
        {
            // sengoku1560.json に personality が追加されているか確認
            var path = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "scenarios", "sengoku1560.json");

            if (!System.IO.File.Exists(path))
            {
                Assert.Inconclusive("sengoku1560.json が見つかりません（ビルド出力を確認）");
                return;
            }

            var (world, _, _) = ScenarioLoader.Load(path);

            var nobunaga = world.Officers.FirstOrDefault(o => o.Name == "信長");
            var mitsuhide = world.Officers.FirstOrDefault(o => o.Name == "光秀");

            Assert.IsNotNull(nobunaga);
            Assert.IsNotNull(mitsuhide);
            Assert.AreEqual(OfficerPersonality.Brave,     nobunaga!.Personality);
            Assert.AreEqual(OfficerPersonality.Ambitious, mitsuhide!.Personality);
        }
    }
}
