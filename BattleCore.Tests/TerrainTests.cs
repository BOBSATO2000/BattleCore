using BattleCore.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class TerrainTests
    {
        [TestMethod]
        public void HexKeepsTerrain()
        {
            var hex = new Hex(
                1,
                0,
                0,
                TerrainType.Forest);

            Assert.AreEqual(
                TerrainType.Forest,
                hex.Terrain);
        }
    }
}
