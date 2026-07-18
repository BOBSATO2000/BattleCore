using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class RelationshipSystemTests
    {
        [TestMethod]
        public void AlliesOnSameHexIncreaseTrust()
        {
            // 同じ勢力・同じHexにいるとTrustが上がる
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var o1 = new Officer(1, "信長");
            var o2 = new Officer(2, "光秀");
            world.Officers.AddRange(new[] { o1, o2 });

            var a1 = new Army(1, 1, 1, 1); a1.AssignOfficer(o1.Id);
            var a2 = new Army(2, 2, 1, 1); a2.AssignOfficer(o2.Id); // 同じClan・同じHex
            world.Armies.AddRange(new[] { a1, a2 });

            var context = new SimulationContext(world);
            new RelationshipSystem(allyTrustGain: 1).Update(context);

            var rel = world.GetOrCreateRelationship(o1.Id, o2.Id);
            Assert.AreEqual(1, rel.Trust);
        }

        [TestMethod]
        public void EnemiesOnSameHexIncreaseDislike()
        {
            // 異なる勢力・同じHexにいるとDislikeが上がる
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var o1 = new Officer(1, "信長");
            var o2 = new Officer(2, "信玄");
            world.Officers.AddRange(new[] { o1, o2 });

            var a1 = new Army(1, 1, 1, 1); a1.AssignOfficer(o1.Id);
            var a2 = new Army(2, 2, 2, 1); a2.AssignOfficer(o2.Id); // 異なるClan・同じHex
            world.Armies.AddRange(new[] { a1, a2 });

            var context = new SimulationContext(world);
            new RelationshipSystem(enemyDislikeGain: 2).Update(context);

            var rel = world.GetOrCreateRelationship(o1.Id, o2.Id);
            Assert.AreEqual(2, rel.Dislike);
        }

        [TestMethod]
        public void StrongerAllyGainsRespect()
        {
            // 兵力が多い味方への Respect が上がる
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var o1 = new Officer(1, "信長");
            var o2 = new Officer(2, "光秀");
            world.Officers.AddRange(new[] { o1, o2 });

            var a1 = new Army(1, 1, 1, 1); a1.AssignOfficer(o1.Id); // 1000兵
            var a2 = new Army(2, 2, 1, 1); a2.AssignOfficer(o2.Id);
            a2.LoseSoldiers(500); // 500兵
            world.Armies.AddRange(new[] { a1, a2 });

            var context = new SimulationContext(world);
            new RelationshipSystem(respectGain: 1).Update(context);

            // 光秀→信長へのRespectが上がる
            var rel = world.GetOrCreateRelationship(o2.Id, o1.Id);
            Assert.AreEqual(1, rel.Respect);
        }

        [TestMethod]
        public void RelationshipValuesDoNotExceed100()
        {
            // 値が100を超えない
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var o1 = new Officer(1, "A");
            var o2 = new Officer(2, "B");
            world.Officers.AddRange(new[] { o1, o2 });

            var a1 = new Army(1, 1, 2, 1); a1.AssignOfficer(o1.Id);
            var a2 = new Army(2, 2, 1, 1); a2.AssignOfficer(o2.Id);
            world.Armies.AddRange(new[] { a1, a2 });

            // 既存のDislikeを99に設定
            var rel = world.GetOrCreateRelationship(o1.Id, o2.Id);
            rel.Dislike = 99;

            var context = new SimulationContext(world);
            new RelationshipSystem(enemyDislikeGain: 5).Update(context);

            Assert.AreEqual(100, rel.Dislike);
        }

        [TestMethod]
        public void HighTrustReducesWinnerLosses()
        {
            // Trust>=60のOfficerペアがいると勝者損害が5%減少する
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var o1 = new Officer(1, "信長");
            var o2 = new Officer(2, "信玄");
            world.Officers.AddRange(new[] { o1, o2 });

            // Trust>=60 を設定
            var rel = world.GetOrCreateRelationship(o1.Id, o2.Id);
            rel.Trust = 60;

            var attacker = new Army(1, 1, 1, 1); attacker.AssignOfficer(o1.Id); // 1000兵
            var defender = new Army(2, 2, 2, 1); defender.LoseSoldiers(700);    // 300兵
            world.Armies.AddRange(new[] { attacker, defender });

            var resolver = new BattleCore.Battle.BattleResolver();
            var battle = new BattleCore.Battle.Battle(attacker, defender);
            resolver.Resolve(battle, world);

            // Trust補正なし: winnerLosses = 300/3 = 100
            // Trust補正あり: 100 * 0.95 = 95
            Assert.IsTrue(attacker.Soldiers > 900); // 1000 - 95 = 905 > 900
        }

        [TestMethod]
        public void LowTrustDoesNotReduceWinnerLosses()
        {
            // Trust<60のOfficerペアは損害軽減なし
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var o1 = new Officer(1, "信長");
            var o2 = new Officer(2, "信玄");
            world.Officers.AddRange(new[] { o1, o2 });

            var rel = world.GetOrCreateRelationship(o1.Id, o2.Id);
            rel.Trust = 59;

            var attacker = new Army(1, 1, 1, 1); attacker.AssignOfficer(o1.Id);
            var defender = new Army(2, 2, 2, 1); defender.LoseSoldiers(700);
            world.Armies.AddRange(new[] { attacker, defender });

            var resolver = new BattleCore.Battle.BattleResolver();
            var battle = new BattleCore.Battle.Battle(attacker, defender);
            resolver.Resolve(battle, world);

            // Trust補正なし: winnerLosses = 300/3 × Strategy(0→factor=0.7) × Courage(0→factor=0.9) = 63
            // attacker.Soldiers = 1000 - 63 = 937
            Assert.AreEqual(937, attacker.Soldiers);
        }
    }
}
