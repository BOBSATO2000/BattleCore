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
    public class CastleSystemTests
    {
        private static (WorldState world, Army army, Castle castle) Setup(int armyClanId, int castleOwnerClanId)
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            var army = new Army(1, 1, armyClanId, 1);
            world.Armies.Add(army);
            var castle = new Castle(1, "テスト城", 1, castleOwnerClanId, 100);
            world.Castles.Add(castle);
            return (world, army, castle);
        }

        [TestMethod]
        public void OccupyingArmy_GetsReinforcement()
        {
            var (world, army, castle) = Setup(armyClanId: 1, castleOwnerClanId: 1);
            // MaxSoldiers=1000のまま、兵力を900に減らして補充をテスト
            army.LoseSoldiers(100);
            var initialSoldiers = army.Soldiers; // 900

            var engine = new SimulationEngine(world);
            engine.RegisterSystem(new CastleSystem());
            engine.Step();

            Assert.AreEqual(initialSoldiers + 100, army.Soldiers);
        }

        [TestMethod]
        public void EnemyArmy_CapturesCastle()
        {
            var (world, army, castle) = Setup(armyClanId: 2, castleOwnerClanId: 1);

            var engine = new SimulationEngine(world);
            engine.RegisterSystem(new CastleSystem());
            engine.Step();

            Assert.AreEqual(2, castle.OwnerClanId);
        }

        [TestMethod]
        public void CapturedCastle_FiresEvent()
        {
            var (world, army, castle) = Setup(armyClanId: 2, castleOwnerClanId: 1);
            var context = new SimulationContext(world);

            new CastleSystem().Update(context);

            var ev = context.EventQueue.OfType<CastleCapturedEvent>().FirstOrDefault();
            Assert.IsNotNull(ev);
            Assert.AreEqual(2, ev.NewOwnerClanId);
        }

        [TestMethod]
        public void CastleBonus_ReducesLoserLosses()
        {
            var calc    = new Systems.Battle.DamageCalculator();
            var left    = new Army(1, 1, 1, 1000);
            var right   = new Army(2, 2, 2, 1000);
            var noCastle = calc.Calculate(left, right, null, null, TerrainType.Plain, Simulation.Weather.Sunny, false);
            var castle   = calc.Calculate(left, right, null, null, TerrainType.Plain, Simulation.Weather.Sunny, true);

            Assert.AreEqual((int)(noCastle.LoserLosses * 0.80), castle.LoserLosses);
        }
    }
}
