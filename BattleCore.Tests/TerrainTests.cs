using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Systems.Battle;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class TerrainTests
    {
        [TestMethod]
        public void HexKeepsTerrain()
        {
            var hex = new Hex(1, 0, 0, TerrainType.Forest);
            Assert.AreEqual(TerrainType.Forest, hex.Terrain);
        }

        private static (Army attacker, Army defender) MakeArmies()
        {
            var attacker = new Army(1, 1, 1, 1000);
            var defender = new Army(2, 2, 2, 1000);
            return (attacker, defender);
        }

        [TestMethod]
        public void PlainTerrain_NoBonus()
        {
            var calc = new DamageCalculator();
            var (a, d) = MakeArmies();
            var plain  = calc.Calculate(a, d, null, null, TerrainType.Plain);
            var noArg  = calc.Calculate(a, d, null, null);
            Assert.AreEqual(noArg.LoserLosses, plain.LoserLosses);
        }

        [TestMethod]
        public void ForestTerrain_ReducesLoserLosses20Percent()
        {
            var calc = new DamageCalculator();
            var (a, d) = MakeArmies();
            var plain  = calc.Calculate(a, d, null, null, TerrainType.Plain);
            var forest = calc.Calculate(a, d, null, null, TerrainType.Forest);
            Assert.AreEqual((int)(plain.LoserLosses * 0.80), forest.LoserLosses);
        }

        [TestMethod]
        public void MountainTerrain_ReducesLoserLosses30Percent()
        {
            var calc = new DamageCalculator();
            var (a, d) = MakeArmies();
            var plain    = calc.Calculate(a, d, null, null, TerrainType.Plain);
            var mountain = calc.Calculate(a, d, null, null, TerrainType.Mountain);
            Assert.AreEqual((int)(plain.LoserLosses * 0.70), mountain.LoserLosses);
        }
    }
}
