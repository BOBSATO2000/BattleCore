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
    public class FoodSystemTests
    {
        private static (WorldState world, Army army) MakeArmy(int hexId = 1)
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(hexId, 0, 0, TerrainType.Plain));
            var army = new Army(1, 1, 1, hexId);
            world.Armies.Add(army);
            return (world, army);
        }

        [TestMethod]
        public void NormalConsumption_ReducesFood()
        {
            var (world, army) = MakeArmy();
            army.Food = 100;

            new FoodSystem().Update(new SimulationContext(world));

            Assert.AreEqual(100 - Army.FoodConsumptionPerTick, army.Food);
        }

        [TestMethod]
        public void CastleOnSameHex_ReplenishesFood()
        {
            var (world, army) = MakeArmy();
            army.Food = 40;
            var castle = new Castle(1, "テスト城", 1, ownerClanId: 1, reinforcementPerTick: 40);
            world.Castles.Add(castle);

            new FoodSystem().Update(new SimulationContext(world));

            // +20（城補充）-10（消費）= 50
            Assert.AreEqual(50, army.Food);
        }

        [TestMethod]
        public void SiegedCastle_NoReplenishment()
        {
            var (world, army) = MakeArmy();
            army.Food = 50;
            var castle = new Castle(1, "テスト城", 1, ownerClanId: 1, reinforcementPerTick: 40);
            castle.SiegeTick = 1; // 包囲中
            world.Castles.Add(castle);

            new FoodSystem().Update(new SimulationContext(world));

            // 包囲中は城補充なし、守備側2倍消費 → 50 - 20 = 30
            Assert.AreEqual(30, army.Food);
        }

        [TestMethod]
        public void DefenderUnderSiege_DoubleConsumption()
        {
            var (world, army) = MakeArmy();
            army.Food = 60;
            var castle = new Castle(1, "テスト城", 1, ownerClanId: 1, reinforcementPerTick: 0);
            castle.SiegeTick = 3;
            world.Castles.Add(castle);

            new FoodSystem().Update(new SimulationContext(world));

            Assert.AreEqual(60 - Army.FoodConsumptionPerTick * 2, army.Food);
        }

        [TestMethod]
        public void Besieger_ExtraConsumption()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain)); // 城Hex
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain)); // 攻囲軍Hex（隣接）

            // 守備側城
            var castle = new Castle(1, "テスト城", 1, ownerClanId: 1, reinforcementPerTick: 0);
            castle.SiegeTick = 1;
            world.Castles.Add(castle);

            // 攻囲軍（勢力2、隣接Hexにいる）
            var besieger = new Army(1, 1, 2, 2);
            besieger.Food = 100;
            world.Armies.Add(besieger);

            new FoodSystem().Update(new SimulationContext(world));

            // 1.5倍消費 = 15
            Assert.AreEqual(100 - (int)(Army.FoodConsumptionPerTick * 1.5), besieger.Food);
        }

        [TestMethod]
        public void FoodReachesZero_MoralePenalty()
        {
            var (world, army) = MakeArmy();
            army.Food = 5; // 消費10で0になる

            var context = new SimulationContext(world);
            new FoodSystem().Update(context);

            Assert.AreEqual(0, army.Food);
            Assert.AreEqual(90, army.Morale); // -10
            var ev = context.EventQueue.OfType<MoraleEvent>().FirstOrDefault();
            Assert.IsNotNull(ev);
            Assert.AreEqual("兵糧切れ", ev.Reason);
        }

        [TestMethod]
        public void FoodZero_ReducesActionPoints()
        {
            var (world, army) = MakeArmy();
            army.Food = 0;
            army.ActionPoints = 2;

            new FoodSystem().Update(new SimulationContext(world));

            Assert.AreEqual(1, army.ActionPoints); // -1
        }

        [TestMethod]
        public void FoodNotReplenishedAboveMax()
        {
            var (world, army) = MakeArmy();
            army.Food = Army.MaxFood;
            var castle = new Castle(1, "テスト城", 1, ownerClanId: 1, reinforcementPerTick: 100);
            world.Castles.Add(castle);

            new FoodSystem().Update(new SimulationContext(world));

            Assert.IsTrue(army.Food <= Army.MaxFood);
        }
    }
}
