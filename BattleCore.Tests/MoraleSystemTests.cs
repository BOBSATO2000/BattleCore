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
    public class MoraleSystemTests
    {
        private static (WorldState world, Army army) MakeArmy(int clanId, int hexId, int soldiers = 1000)
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(hexId, hexId - 1, 0, TerrainType.Plain));
            var army = new Army(1, 1, clanId, hexId);
            army.SetInitialSoldiers(soldiers);
            world.Armies.Add(army);
            return (world, army);
        }

        [TestMethod]
        public void EnemyOnSameHex_ReducesMorale()
        {
            var (world, army) = MakeArmy(clanId: 1, hexId: 1);
            var enemy = new Army(2, 2, 2, 1);
            world.Armies.Add(enemy);

            new MoraleSystem().Update(new SimulationContext(world));

            Assert.AreEqual(95, army.Morale); // -5
        }

        [TestMethod]
        public void AllyOnNeighborHex_IncreasesMorale()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));

            var army = new Army(1, 1, 1, 1);
            army.Morale = 90; // 90から開始することで+3が確認できる
            var ally = new Army(2, 2, 1, 2);
            world.Armies.Add(army);
            world.Armies.Add(ally);

            new MoraleSystem().Update(new SimulationContext(world));

            Assert.AreEqual(93, army.Morale); // +3
        }

        [TestMethod]
        public void LowStrength_ReducesMorale()
        {
            var (world, army) = MakeArmy(clanId: 1, hexId: 1, soldiers: 1000);
            army.LoseSoldiers(600); // 400残 = 40% < 50%

            new MoraleSystem().Update(new SimulationContext(world));

            Assert.AreEqual(97, army.Morale); // -3
        }

        [TestMethod]
        public void FullStrength_NoMoralePenalty()
        {
            var (world, army) = MakeArmy(clanId: 1, hexId: 1, soldiers: 1000);
            // 敵なし・隣接味方なし・兵力満タン → 変化なし

            new MoraleSystem().Update(new SimulationContext(world));

            Assert.AreEqual(100, army.Morale);
        }

        [TestMethod]
        public void MoraleClamped_NotBelow0()
        {
            var (world, army) = MakeArmy(clanId: 1, hexId: 1, soldiers: 1000);
            army.Morale = 2;
            army.LoseSoldiers(600); // 兵力50%未満 → -3

            new MoraleSystem().Update(new SimulationContext(world));

            Assert.AreEqual(0, army.Morale); // 0未満にならない
        }

        [TestMethod]
        public void MoraleClamped_NotAbove100()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));

            var army = new Army(1, 1, 1, 1);
            army.Morale = 99;
            var ally = new Army(2, 2, 1, 2);
            world.Armies.Add(army);
            world.Armies.Add(ally);

            new MoraleSystem().Update(new SimulationContext(world));

            Assert.AreEqual(100, army.Morale); // 100超えない
        }

        [TestMethod]
        public void BattleWin_IncreasesMorale()
        {
            // BattleResolverが勝者+5を適用することを確認
            var army = new Army(1, 1, 1, 1);
            army.Morale = 80;
            army.Morale = System.Math.Min(100, army.Morale + 5);
            Assert.AreEqual(85, army.Morale);
        }

        [TestMethod]
        public void BattleLoss_ReducesMorale()
        {
            // BattleResolverが敗者-15を適用することを確認
            var army = new Army(1, 1, 1, 1);
            army.Morale = 50;
            army.Morale = System.Math.Max(0, army.Morale - 15);
            Assert.AreEqual(35, army.Morale);
        }

        [TestMethod]
        public void LowMorale_FiresEvent()
        {
            var (world, army) = MakeArmy(clanId: 1, hexId: 1);
            army.Morale = 22;
            var enemy = new Army(2, 2, 2, 1);
            world.Armies.Add(enemy);

            var context = new SimulationContext(world);
            new MoraleSystem().Update(context);

            // 22-5=17 → 閾値20以下に初めて突入 → MoraleEvent発火
            Assert.IsTrue(context.EventQueue.OfType<MoraleEvent>().Any());
        }
    }
}
