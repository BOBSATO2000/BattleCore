using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class MoraleToLoyaltyTests
    {
        /// <summary>
        /// winLoyaltyBonus=0, springBonus=0 で士気ペナルティだけを純粋に検証する。
        /// </summary>
        private static (WorldState world, Membership membership, Army army) Setup(int morale)
        {
            var world   = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, Map.TerrainType.Plain));
            var officer = new Officer(1, "武将") { Ambition = 0, Loyalty = 50 };
            world.Officers.Add(officer);
            var membership = new Membership(1, officer.Id, clanId: 1) { Loyalty = 50 };
            world.Memberships.Add(membership);
            var army = new Army(1, 1, 1, 1);
            army.AssignOfficer(officer.Id);
            army.Morale = morale;
            world.Armies.Add(army);
            return (world, membership, army);
        }

        [TestMethod]
        public void Morale29_LoyaltyMinus1()
        {
            var (world, membership, _) = Setup(morale: 29);

            new LoyaltySystem(betrayalThreshold: 200, winLoyaltyBonus: 0, springBonus: 0)
                .Update(new SimulationContext(world));

            Assert.AreEqual(49, membership.Loyalty); // -1（士気<30）
        }

        [TestMethod]
        public void Morale9_LoyaltyMinus3()
        {
            var (world, membership, _) = Setup(morale: 9);

            new LoyaltySystem(betrayalThreshold: 200, winLoyaltyBonus: 0, springBonus: 0)
                .Update(new SimulationContext(world));

            Assert.AreEqual(47, membership.Loyalty); // -3（士気<10）
        }

        [TestMethod]
        public void Morale30_NoLoyaltyPenalty()
        {
            var (world, membership, _) = Setup(morale: 30);

            new LoyaltySystem(betrayalThreshold: 200, winLoyaltyBonus: 0, springBonus: 0)
                .Update(new SimulationContext(world));

            Assert.AreEqual(50, membership.Loyalty); // ペナルティなし
        }

        [TestMethod]
        public void Morale100_NoLoyaltyPenalty()
        {
            var (world, membership, _) = Setup(morale: 100);

            new LoyaltySystem(betrayalThreshold: 200, winLoyaltyBonus: 0, springBonus: 0)
                .Update(new SimulationContext(world));

            Assert.AreEqual(50, membership.Loyalty); // ペナルティなし
        }

        [TestMethod]
        public void LowMorale_CanTriggerBetrayal()
        {
            // 士気9 → Loyalty -3 → 離反スコアが閾値を超えるケース
            // score = Ambition - Loyalty/2 - Membership.Loyalty/2
            // 士気ペナルティ適用後: Membership.Loyalty = 10 - 3 = 7
            // score = 80 - 20/2 - 7/2 = 80 - 10 - 3 = 67 → 閾値65で離反
            var world   = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, Map.TerrainType.Plain));
            var officer = new Officer(1, "不満武将") { Ambition = 80, Loyalty = 20 };
            world.Officers.Add(officer);
            var membership = new Membership(1, officer.Id, clanId: 1) { Loyalty = 10 };
            world.Memberships.Add(membership);
            var army = new Army(1, 1, 1, 1);
            army.AssignOfficer(officer.Id);
            army.Morale = 9;
            world.Armies.Add(army);

            new LoyaltySystem(betrayalThreshold: 65, winLoyaltyBonus: 0, springBonus: 0)
                .Update(new SimulationContext(world));

            Assert.IsEmpty(world.Memberships);
        }
    }
}
