using BattleCore.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class HexDistanceTests
    {
        [TestMethod]
        public void DistanceBetweenAdjacentHexesIsOne()
        {
            var hex1 = new Hex(1, 0, 0);
            var hex2 = new Hex(2, 1, 0);

            var distance = HexDistance.Calculate(hex1, hex2);

            Assert.AreEqual(1, distance);
        }
        [TestMethod]
        public void DistanceTwoHexesApartIsTwo()
        {
            var hex1 = new Hex(1, 0, 0);
            var hex2 = new Hex(2, 2, 0);

            var distance = HexDistance.Calculate(hex1, hex2);

            Assert.AreEqual(2, distance);
        }
        [TestMethod]
        public void DistanceVerticalTwoHexesApartIsTwo()
        {
            var hex1 = new Hex(1, 0, 0);
            var hex2 = new Hex(2, 0, 2);

            var distance = HexDistance.Calculate(hex1, hex2);

            Assert.AreEqual(2, distance);
        }
        [TestMethod]
        public void DistanceDiagonalHexesIsTwo()
        {
            var hex1 = new Hex(1, 0, 0);
            var hex2 = new Hex(2, 1, 1);

            var distance = HexDistance.Calculate(hex1, hex2);

            Assert.AreEqual(2, distance);
        }
    }
}
