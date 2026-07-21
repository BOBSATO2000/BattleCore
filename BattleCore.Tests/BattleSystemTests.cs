using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems.Battle;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class BattleSystemTests
    {
        [TestMethod]
        public void DifferentClanStartsBattle()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 1);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            var system = new BattleSystem();
            system.Update(context);

            Assert.IsTrue(army1.Soldiers < 1000 || army2.Soldiers < 1000);
        }

        [TestMethod]
        public void SameClanDoesNotBattle()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 1, 1); // 同じ ClanId=1
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            var system = new BattleSystem();
            system.Update(context);

            Assert.AreEqual(1000, army1.Soldiers);
            Assert.AreEqual(1000, army2.Soldiers);
        }

        [TestMethod]
        public void DifferentClanBattleReducesSoldiers()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 1);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            var system = new BattleSystem();
            system.Update(context);

            Assert.IsTrue(army1.Soldiers < 1000 || army2.Soldiers < 1000);
        }

        [TestMethod]
        public void ArmySoldiersNeverBecomeNegative()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 1);

            // army1 を極端に弱くする
            army1.LoseSoldiers(990);

            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            var system = new BattleSystem();
            system.Update(context);

            Assert.IsTrue(army1.Soldiers >= 0);
            Assert.IsTrue(army2.Soldiers >= 0);
        }

        [TestMethod]
        public void WinnerSurvives()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army1 = new Army(1, 1, 1, 1); // 1000
            var army2 = new Army(2, 2, 2, 1);
            army2.LoseSoldiers(900); // 100

            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            new BattleSystem().Update(context);

            Assert.IsTrue(army1.Soldiers > 0);
        }

        [TestMethod]
        public void LoserDestroyed()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army1 = new Army(1, 1, 1, 1); // 1000
            var army2 = new Army(2, 2, 2, 1);
            army2.LoseSoldiers(1000); // 0 → 戦闘対象外になるが念のため確認

            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            new BattleSystem().Update(context);

            Assert.AreEqual(0, army2.Soldiers);
        }

        [TestMethod]
        public void BattleReducesSoldierCount()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 1);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            new BattleSystem().Update(context);

            Assert.IsLessThan(2000, army1.Soldiers + army2.Soldiers);
        }

        [TestMethod]
        public void BattleOccursWhenDifferentClansOccupySameHex()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 1);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var before = army1.Soldiers + army2.Soldiers;

            var context = new SimulationContext(world);
            new BattleSystem().Update(context);

            Assert.IsLessThan(before, army1.Soldiers + army2.Soldiers);
        }

        [TestMethod]
        public void HighLeadershipWinsAgainstLowerSoldierCount()
        {
            // Leadership200の指揮官付き500兵 vs 指揮官なし700兵
            // 実効兵力: 500×1.5(clamp)=750 vs 700×1.0=700 → Leadership側が勝つ
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var officer = new Officer(1, "高統率武将") { Leadership = 200 };
            world.Officers.Add(officer);

            var army1 = new Army(1, 1, 1, 1);
            army1.LoseSoldiers(500); // 500兵
            army1.AssignOfficer(officer.Id);

            var army2 = new Army(2, 2, 2, 1);
            army2.LoseSoldiers(300); // 700兵

            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            new BattleSystem().Update(context);

            // Leadership補正で army1 が勝つ
            Assert.IsTrue(army1.Soldiers > 0);
            Assert.AreEqual(0, army2.Soldiers);
        }

        [TestMethod]
        public void HighStrategyReducesWinnerLosses()
        {
            // Strategy200の指揮官付き軍 vs 指揮官なし軍（同兵力）
            // 勝者損害 = 敵兵力/3 × Strategy補正(1.3上限) → 損害が増える
            // Strategy50の場合 = 敵兵力/3 × 0.7 → 損害が減る
            var world1 = new WorldState();
            world1.Map.AddHex(new Hex(1, 0, 0));
            var highStrategy = new Officer(1, "高戦術武将") { Leadership = 100, Strategy = 50 };
            world1.Officers.Add(highStrategy);
            var a1 = new Army(1, 1, 1, 1); a1.AssignOfficer(highStrategy.Id);
            var a2 = new Army(2, 2, 2, 1);
            world1.Armies.Add(a1); world1.Armies.Add(a2);
            new BattleSystem().Update(new SimulationContext(world1));
            var lowLosses = a1.Soldiers; // Strategy低い→損害少ない

            var world2 = new WorldState();
            world2.Map.AddHex(new Hex(1, 0, 0));
            var lowStrategy = new Officer(2, "低戦術武将") { Leadership = 100, Strategy = 150 };
            world2.Officers.Add(lowStrategy);
            var b1 = new Army(1, 1, 1, 1); b1.AssignOfficer(lowStrategy.Id);
            var b2 = new Army(2, 2, 2, 1);
            world2.Armies.Add(b1); world2.Armies.Add(b2);
            new BattleSystem().Update(new SimulationContext(world2));
            var highLosses = b1.Soldiers; // Strategy高い→損害多い

            Assert.IsGreaterThan(highLosses, lowLosses);
        }

        [TestMethod]
        public void NoOfficerBehavesAsDefault()
        {
            // Officer未配属でも従来通り動作する
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 1);
            army2.LoseSoldiers(900); // 100兵
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var context = new SimulationContext(world);
            new BattleSystem().Update(context);

            Assert.IsTrue(army1.Soldiers > 0);
            Assert.AreEqual(0, army2.Soldiers);
        }
    }
}
