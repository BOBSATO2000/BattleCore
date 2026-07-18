using BattleCore.Battle;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class OfficerGrowthTests
    {
        private static (WorldState world, Army attacker, Army defender, Officer officer)
            MakeWorld(int attackerSoldiers = 1000, int defenderSoldiers = 100)
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var o = new Officer(1, "信長") { Leadership = 100, Strategy = 100 };
            world.Officers.Add(o);

            var attacker = new Army(1, 0, 1, 1); attacker.AssignOfficer(o.Id);
            if (attackerSoldiers < 1000) attacker.LoseSoldiers(1000 - attackerSoldiers);

            var defender = new Army(2, 0, 2, 1);
            if (defenderSoldiers < 1000) defender.LoseSoldiers(1000 - defenderSoldiers);

            world.Armies.AddRange(new[] { attacker, defender });
            return (world, attacker, defender, o);
        }

        [TestMethod]
        public void WinnerLeadershipGrowsOnOddWin()
        {
            // 1回目の勝利でLeadership+1
            var (world, attacker, defender, officer) = MakeWorld();
            var resolver = new BattleResolver();
            resolver.Resolve(new BattleCore.Battle.Battle(attacker, defender), world);

            Assert.AreEqual(1, officer.BattleWins);
            Assert.AreEqual(101, officer.Leadership);
            Assert.AreEqual(100, officer.Strategy);
        }

        [TestMethod]
        public void WinnerStrategyGrowsOnEvenWin()
        {
            // 2回目の勝利でStrategy+1
            var (world, attacker, defender, officer) = MakeWorld();
            var resolver = new BattleResolver();
            resolver.Resolve(new BattleCore.Battle.Battle(attacker, defender), world);

            // 2回目：defenderを再設定
            var defender2 = new Army(3, 0, 2, 1); defender2.LoseSoldiers(900);
            world.Armies.Add(defender2);
            resolver.Resolve(new BattleCore.Battle.Battle(attacker, defender2), world);

            Assert.AreEqual(2, officer.BattleWins);
            Assert.AreEqual(101, officer.Leadership);
            Assert.AreEqual(101, officer.Strategy);
        }

        [TestMethod]
        public void GrowthCapAt200()
        {
            // Leadership上限200を超えない
            var (world, attacker, defender, officer) = MakeWorld();
            officer.Leadership = 200;
            officer.BattleWins = 0; // 次は奇数勝利

            var resolver = new BattleResolver();
            resolver.Resolve(new BattleCore.Battle.Battle(attacker, defender), world);

            Assert.AreEqual(200, officer.Leadership);
        }

        [TestMethod]
        public void LoserDoesNotGrow()
        {
            // 敗者のOfficerは成長しない
            var (world, attacker, defender, officer) = MakeWorld(
                attackerSoldiers: 100, defenderSoldiers: 1000);

            var defOfficer = new Officer(2, "信玄") { Leadership = 100, Strategy = 100 };
            world.Officers.Add(defOfficer);
            defender.AssignOfficer(defOfficer.Id);

            var resolver = new BattleResolver();
            resolver.Resolve(new BattleCore.Battle.Battle(attacker, defender), world);

            Assert.AreEqual(0, officer.BattleWins);
        }
    }
}
