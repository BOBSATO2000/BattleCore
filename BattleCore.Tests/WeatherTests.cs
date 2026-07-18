using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems.Battle;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BattleCore.Tests
{
    [TestClass]
    public class WeatherTests
    {
        [TestMethod]
        public void GameTime_DefaultWeather_IsSunny()
        {
            var time = new GameTime();
            Assert.AreEqual(Weather.Sunny, time.Weather);
        }

        [TestMethod]
        public void Rain_ReducesBothSidesDamage10Percent()
        {
            var calc  = new DamageCalculator();
            var left  = new Army(1, 1, 1, 1000);
            var right = new Army(2, 2, 2, 1000);

            var sunny = calc.Calculate(left, right, null, null, TerrainType.Plain, Weather.Sunny);
            var rain  = calc.Calculate(left, right, null, null, TerrainType.Plain, Weather.Rain);

            Assert.AreEqual((int)(sunny.WinnerLosses  * 0.90), rain.WinnerLosses);
            Assert.AreEqual((int)(sunny.LoserLosses   * 0.90), rain.LoserLosses);
        }

        [TestMethod]
        public void Fog_NoDamageChange()
        {
            var calc  = new DamageCalculator();
            var left  = new Army(1, 1, 1, 1000);
            var right = new Army(2, 2, 2, 1000);

            var sunny = calc.Calculate(left, right, null, null, TerrainType.Plain, Weather.Sunny);
            var fog   = calc.Calculate(left, right, null, null, TerrainType.Plain, Weather.Fog);

            Assert.AreEqual(sunny.WinnerLosses, fog.WinnerLosses);
            Assert.AreEqual(sunny.LoserLosses,  fog.LoserLosses);
        }

        [TestMethod]
        public void Rain_ForestMoveCost_IsTwo()
        {
            var world  = new WorldState();
            var plain  = new Hex(1, 0, 0, TerrainType.Plain);
            var forest = new Hex(2, 1, 0, TerrainType.Forest);
            world.Map.AddHex(plain);
            world.Map.AddHex(forest);

            var army = new Army(1, 1, 1, plain.Id);
            world.Armies.Add(army);
            army.OrderMove(forest.Id);

            // Rain をセット
            world.Weather = Weather.Rain;

            var engine = new SimulationEngine(world);
            engine.RegisterSystem(new Systems.MovementSystem());

            // Tick1: plain -> forest（Rain時クールダウン=2）
            engine.Step();
            Assert.AreEqual(forest.Id, army.CurrentHexId);
            Assert.AreEqual(2, army.MoveCooldown);
        }
    }
}
