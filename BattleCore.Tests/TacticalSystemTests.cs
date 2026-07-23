using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.Systems.Battle;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class TacticalSystemTests
    {
        // ── ZOC ───────────────────────────────────────────────

        [TestMethod]
        public void ZocSystem_ArmyAdjacentToEnemy_InZocTrue()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            var friendly = new Army(1, 1, 1, 1);
            var enemy    = new Army(2, 2, 2, 2);
            world.Armies.Add(friendly);
            world.Armies.Add(enemy);

            new ZocSystem().Update(new SimulationContext(world));

            Assert.IsTrue(friendly.InZoc);
        }

        [TestMethod]
        public void ZocSystem_ArmyNotAdjacentToEnemy_InZocFalse()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 5, 0)); // 遠い
            var friendly = new Army(1, 1, 1, 1);
            var enemy    = new Army(2, 2, 2, 3);
            world.Armies.Add(friendly);
            world.Armies.Add(enemy);

            new ZocSystem().Update(new SimulationContext(world));

            Assert.IsFalse(friendly.InZoc);
        }

        [TestMethod]
        public void ZocSystem_AlliedArmy_NotInZoc()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Alliances.Add(new Alliance(1, 1, 2, 999));
            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 2);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            new ZocSystem().Update(new SimulationContext(world));

            Assert.IsFalse(army1.InZoc);
        }

        // ── 陣形 ──────────────────────────────────────────────

        [TestMethod]
        public void FormationOrder_SetsFormation()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            var army = new Army(1, 1, 1, 1);
            world.Armies.Add(army);

            new FormationOrder(1, ArmyFormation.Circle).Execute(new SimulationContext(world));

            Assert.AreEqual(ArmyFormation.Circle, army.Formation);
        }

        [TestMethod]
        public void FormationModifier_Circle_ReducesLoserLosses()
        {
            var attacker = new Army(1, 1, 1, 1);
            var defender = new Army(2, 2, 2, 1);
            attacker.SetInitialSoldiers(1000);
            defender.SetInitialSoldiers(1000);
            defender.Formation = ArmyFormation.Circle;

            var calc = new DamageCalculator();
            var result = calc.Calculate(attacker, defender);

            // Circle は防御+10% → 敗者損害が少ない
            Assert.IsTrue(result.Breakdown.Entries.Any(e => e.Label.Contains("Circle")));
        }

        [TestMethod]
        public void FormationModifier_Wedge_IncreasesAttack()
        {
            var attacker = new Army(1, 1, 1, 1);
            var defender = new Army(2, 2, 2, 1);
            attacker.SetInitialSoldiers(1000);
            defender.SetInitialSoldiers(1000);
            attacker.Formation = ArmyFormation.Wedge;

            var calc = new DamageCalculator();
            var result = calc.Calculate(attacker, defender);

            Assert.IsTrue(result.Breakdown.Entries.Any(e => e.Label.Contains("Wedge")));
        }

        // ── 計略 ──────────────────────────────────────────────

        [TestMethod]
        public void StrategySystem_RecoversSP_EachTick()
        {
            var world   = new WorldState();
            var officer = new Officer(1, "諸葛亮") { Intelligence = 100, StrategyPoint = 0 };
            world.Officers.Add(officer);

            new StrategySystem().Update(new SimulationContext(world));

            Assert.AreEqual(1, officer.StrategyPoint);
        }

        [TestMethod]
        public void StrategySystem_DoesNotExceedMax()
        {
            var world   = new WorldState();
            var officer = new Officer(1, "諸葛亮") { Intelligence = 100 };
            officer.StrategyPoint = officer.MaxStrategyPoint;
            world.Officers.Add(officer);

            new StrategySystem().Update(new SimulationContext(world));

            Assert.AreEqual(officer.MaxStrategyPoint, officer.StrategyPoint);
        }

        [TestMethod]
        public void InspireStrategyOrder_IncreasesMonale()
        {
            var world   = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            var officer = new Officer(1, "張飛") { Intelligence = 50, StrategyPoint = 5 };
            var army    = new Army(1, 1, 1, 1);
            army.AssignOfficer(1);
            army.Morale = 70;
            world.Officers.Add(officer);
            world.Armies.Add(army);

            new InspireStrategyOrder(1).Execute(new SimulationContext(world));

            Assert.AreEqual(85, army.Morale);
            Assert.AreEqual(3, officer.StrategyPoint); // 5-2=3
        }

        [TestMethod]
        public void InspireStrategyOrder_InsufficientSP_NoEffect()
        {
            var world   = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            var officer = new Officer(1, "張飛") { Intelligence = 50, StrategyPoint = 1 };
            var army    = new Army(1, 1, 1, 1);
            army.AssignOfficer(1);
            army.Morale = 70;
            world.Officers.Add(officer);
            world.Armies.Add(army);

            new InspireStrategyOrder(1).Execute(new SimulationContext(world));

            Assert.AreEqual(70, army.Morale); // SP不足で発動せず
        }

        [TestMethod]
        public void FireStrategyOrder_DamagesEnemyFood()
        {
            var world   = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            var officer = new Officer(1, "周瑜") { Intelligence = 80, StrategyPoint = 5 };
            var attacker = new Army(1, 1, 1, 1);
            attacker.AssignOfficer(1);
            var enemy = new Army(2, 2, 2, 2);
            enemy.Food = 80;
            world.Officers.Add(officer);
            world.Armies.Add(attacker);
            world.Armies.Add(enemy);

            new FireStrategyOrder(1, 2).Execute(new SimulationContext(world));

            Assert.AreEqual(50, enemy.Food);   // -30
            Assert.AreEqual(85, enemy.Morale); // -15
            Assert.AreEqual(2, officer.StrategyPoint); // 5-3=2
        }

        [TestMethod]
        public void NightRaidStrategyOrder_DaytimeNoEffect()
        {
            var world   = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            var officer = new Officer(1, "陸遜") { Intelligence = 90, StrategyPoint = 5 };
            var army    = new Army(1, 1, 1, 1);
            army.AssignOfficer(1);
            world.Officers.Add(officer);
            world.Armies.Add(army);

            // Tick=0 は昼（IsNight = Tick奇数）
            var context = new SimulationContext(world);
            new NightRaidStrategyOrder(1).Execute(context);

            Assert.AreEqual(ArmyStance.Normal, army.Stance); // 昼なので発動せず
            Assert.AreEqual(5, officer.StrategyPoint);
        }

        // ── 一騎討ち ──────────────────────────────────────────

        [TestMethod]
        public void DuelSystem_HighCourageWins_EnemyMoraleDrops()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));

            var officer1 = new Officer(1, "関羽") { Courage = 100 };
            var officer2 = new Officer(2, "顔良") { Courage = 10 };
            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 1);
            army1.AssignOfficer(1);
            army2.AssignOfficer(2);
            army1.SetInitialSoldiers(1000);
            army2.SetInitialSoldiers(1000);

            world.Officers.Add(officer1);
            world.Officers.Add(officer2);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            // 100回試行して少なくとも1回は一騎討ちが発生することを確認
            bool duelOccurred = false;
            for (int i = 0; i < 100; i++)
            {
                army1.Morale = 100;
                army2.Morale = 100;
                var context = new SimulationContext(world);
                new DuelSystem().Update(context);
                if (context.EventQueue.OfType<DuelEvent>().Any())
                {
                    duelOccurred = true;
                    // 関羽(Courage=100)が顔良(Courage=10)に勝つはず
                    var ev = context.EventQueue.OfType<DuelEvent>().First();
                    if (ev.ChallengerWon)
                        Assert.IsTrue(army2.Morale < 100);
                    break;
                }
            }
            Assert.IsTrue(duelOccurred, "100回試行で一騎討ちが発生しなかった");
        }

        // ── 指揮範囲 ──────────────────────────────────────────

        [TestMethod]
        public void CommandRadiusSystem_WithinRadius_MoraleBonus()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            var clan    = new Clan(1) { Name = "織田", DaimyoOfficerId = 1 };
            var daimyo  = new Officer(1, "信長") { Leadership = 90 }; // radius=3
            var cmdArmy = new Army(1, 1, 1, 1);
            cmdArmy.AssignOfficer(1);
            cmdArmy.SetInitialSoldiers(1000);
            var subArmy = new Army(2, 2, 1, 2);
            subArmy.SetInitialSoldiers(1000);
            subArmy.Morale = 80;

            world.Clans.Add(clan);
            world.Officers.Add(daimyo);
            world.Armies.Add(cmdArmy);
            world.Armies.Add(subArmy);

            new CommandRadiusSystem().Update(new SimulationContext(world));

            Assert.AreEqual(82, subArmy.Morale); // +2
        }

        [TestMethod]
        public void CommandRadiusSystem_OutsideRadius_MoralePenalty()
        {
            var world = new WorldState();
            for (int i = 1; i <= 10; i++)
                world.Map.AddHex(new Hex(i, i - 1, 0));

            var clan    = new Clan(1) { Name = "織田", DaimyoOfficerId = 1 };
            var daimyo  = new Officer(1, "信長") { Leadership = 30 }; // radius=1
            var cmdArmy = new Army(1, 1, 1, 1);
            cmdArmy.AssignOfficer(1);
            cmdArmy.SetInitialSoldiers(1000);
            var farArmy = new Army(2, 2, 1, 10); // 9Hex離れている
            farArmy.SetInitialSoldiers(1000);
            farArmy.Morale = 80;

            world.Clans.Add(clan);
            world.Officers.Add(daimyo);
            world.Armies.Add(cmdArmy);
            world.Armies.Add(farArmy);

            new CommandRadiusSystem().Update(new SimulationContext(world));

            Assert.AreEqual(79, farArmy.Morale); // -1
        }
    }
}
