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
    public class SiegeSystemTests
    {
        /// <summary>
        /// 城(Hex1)を中心に、隣接3Hexを持つマップを構築する。
        /// Hex1(0,0) East→Hex2(1,0)  West→Hex3(-1,0)  NorthEast→Hex4(1,-1)
        /// ※ (0,1)はHex1の隣接ではない（SouthEast=(1,1), SouthWest=(-1,1)）
        /// </summary>
        private static WorldState MakeWorld()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1,  0,  0, TerrainType.Plain)); // 城Hex
            world.Map.AddHex(new Hex(2,  1,  0, TerrainType.Plain)); // East
            world.Map.AddHex(new Hex(3, -1,  0, TerrainType.Plain)); // West
            world.Map.AddHex(new Hex(4,  1, -1, TerrainType.Plain)); // NorthEast
            world.Castles.Add(new Castle(1, "テスト城", hexId: 1, ownerClanId: 1));
            return world;
        }

        private static Army AddArmy(WorldState world, int id, int clanId, int hexId)
        {
            var army = new Army(id, id, clanId, hexId);
            world.Armies.Add(army);
            return army;
        }

        [TestMethod]
        public void OneNeighborFree_NotSieged()
        {
            // 隣接3Hexのうち1つに味方 → 補給路あり → 包囲なし
            var world = MakeWorld();
            AddArmy(world, 1, clanId: 2, hexId: 2); // 敵
            AddArmy(world, 2, clanId: 2, hexId: 3); // 敵
            AddArmy(world, 3, clanId: 1, hexId: 4); // 味方（補給路）

            new SiegeSystem().Update(new SimulationContext(world));

            Assert.AreEqual(0, world.Castles[0].SiegeTick);
        }

        [TestMethod]
        public void AllNeighborsBlocked_SiegeStarts()
        {
            // 隣接3Hex全てに敵 → 包囲成立
            var world = MakeWorld();
            AddArmy(world, 1, clanId: 2, hexId: 2);
            AddArmy(world, 2, clanId: 2, hexId: 3);
            AddArmy(world, 3, clanId: 2, hexId: 4);

            var context = new SimulationContext(world);
            new SiegeSystem().Update(context);

            Assert.AreEqual(1, world.Castles[0].SiegeTick);
            Assert.IsTrue(context.EventQueue.OfType<SiegeEvent>()
                .Any(e => e.Type == SiegeEventType.SiegeStarted));
        }

        [TestMethod]
        public void EnemyOnCastleHexOnly_NotSieged()
        {
            // 城Hexに敵がいるだけ（隣接Hexは空）→ 包囲なし
            var world = MakeWorld();
            AddArmy(world, 1, clanId: 2, hexId: 1); // 城Hexに敵

            new SiegeSystem().Update(new SimulationContext(world));

            Assert.AreEqual(0, world.Castles[0].SiegeTick);
        }

        [TestMethod]
        public void SiegeContinues_TickIncreases()
        {
            var world = MakeWorld();
            AddArmy(world, 1, clanId: 2, hexId: 2);
            AddArmy(world, 2, clanId: 2, hexId: 3);
            AddArmy(world, 3, clanId: 2, hexId: 4);

            var context = new SimulationContext(world);
            new SiegeSystem().Update(context);
            new SiegeSystem().Update(context);

            Assert.AreEqual(2, world.Castles[0].SiegeTick);
        }

        [TestMethod]
        public void SiegeLifted_TickResets()
        {
            var world = MakeWorld();
            world.Castles[0].SiegeTick = 3;
            // 隣接Hexに敵なし → 包囲解除

            var context = new SimulationContext(world);
            new SiegeSystem().Update(context);

            Assert.AreEqual(0, world.Castles[0].SiegeTick);
            Assert.IsTrue(context.EventQueue.OfType<SiegeEvent>()
                .Any(e => e.Type == SiegeEventType.SiegeLifted));
        }

        [TestMethod]
        public void SiegeTick10_Surrenders()
        {
            var world = MakeWorld();
            world.Castles[0].SiegeTick = 9; // 次のTickで10になる
            AddArmy(world, 1, clanId: 2, hexId: 2);
            AddArmy(world, 2, clanId: 2, hexId: 3);
            AddArmy(world, 3, clanId: 2, hexId: 4);

            var context = new SimulationContext(world);
            new SiegeSystem().Update(context);

            Assert.AreEqual(2, world.Castles[0].OwnerClanId);
            Assert.AreEqual(0, world.Castles[0].SiegeTick);
            Assert.IsTrue(context.EventQueue.OfType<SiegeEvent>()
                .Any(e => e.Type == SiegeEventType.Surrendered));
            Assert.IsTrue(context.EventQueue.OfType<CastleCapturedEvent>().Any());
        }

        [TestMethod]
        public void Siege_DefenderMoraleDecreases()
        {
            var world = MakeWorld();
            var defender = AddArmy(world, 1, clanId: 1, hexId: 1);
            defender.Morale = 80;
            AddArmy(world, 2, clanId: 2, hexId: 2);
            AddArmy(world, 3, clanId: 2, hexId: 3);
            AddArmy(world, 4, clanId: 2, hexId: 4);

            new SiegeSystem().Update(new SimulationContext(world));

            Assert.AreEqual(78, defender.Morale); // -2
        }

        [TestMethod]
        public void Siege_BesiegerMoraleIncreases()
        {
            var world = MakeWorld();
            var besieger = AddArmy(world, 1, clanId: 2, hexId: 2);
            besieger.Morale = 80;
            AddArmy(world, 2, clanId: 2, hexId: 3);
            AddArmy(world, 3, clanId: 2, hexId: 4);

            new SiegeSystem().Update(new SimulationContext(world));

            Assert.AreEqual(81, besieger.Morale); // +1
        }
    }
}
