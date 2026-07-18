using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class CastleAITests
    {
        [TestMethod]
        public void ArmyPrioritizesNearerEnemyCastle()
        {
            // Hex1(自軍) - Hex2(敵城) - Hex3 - Hex4(敵軍)
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));
            world.Map.AddHex(new Hex(4, 3, 0));

            var clanA = new Clan(1) { Name = "織田" };
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.AddRange(new[] { clanA, clanB });

            var armyA = new Army(1, 0, clanA.Id, 1);
            var armyB = new Army(2, 0, clanB.Id, 4); // 敵軍はHex4（距離3）
            world.Armies.AddRange(new[] { armyA, armyB });

            // 敵城はHex2（距離1）
            world.Castles.Add(new Castle(1, "敵城", 2, clanB.Id, 50));

            var commands = new AggressiveClanStrategy().Decide(clanA, world).ToList();

            Assert.AreEqual(1, commands.Count);
            var move = (MoveArmyCommand)commands[0];
            Assert.AreEqual(2, move.DestinationHexId);
        }

        [TestMethod]
        public void WeakArmy_RetreatsToOwnCastle()
        {
            // Hex1(自城) - Hex2(自軍200兵) - Hex3(敵軍)
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));

            var clanA = new Clan(1) { Name = "織田" };
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.AddRange(new[] { clanA, clanB });

            var armyA = new Army(1, 0, clanA.Id, 2);
            armyA.LoseSoldiers(800); // 200兵
            var armyB = new Army(2, 0, clanB.Id, 3);
            world.Armies.AddRange(new[] { armyA, armyB });

            // 自勢力の城はHex1
            world.Castles.Add(new Castle(1, "自城", 1, clanA.Id, 50));

            var commands = new AggressiveClanStrategy(retreatThreshold: 300).Decide(clanA, world).ToList();

            Assert.AreEqual(1, commands.Count);
            var move = (MoveArmyCommand)commands[0];
            Assert.AreEqual(1, move.DestinationHexId);
        }

        [TestMethod]
        public void FarEnemyCastle_ArmyTargetsEnemyInstead()
        {
            // 敵城が敵軍より遠い場合は敵軍を優先
            // Hex1(自軍) - Hex2(敵軍) - Hex3 - Hex4(敵城)
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));
            world.Map.AddHex(new Hex(4, 3, 0));

            var clanA = new Clan(1) { Name = "織田" };
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.AddRange(new[] { clanA, clanB });

            var armyA = new Army(1, 0, clanA.Id, 1);
            var armyB = new Army(2, 0, clanB.Id, 2); // 敵軍はHex2（距離1）
            world.Armies.AddRange(new[] { armyA, armyB });

            // 敵城はHex4（距離3）
            world.Castles.Add(new Castle(1, "敵城", 4, clanB.Id, 50));

            var commands = new AggressiveClanStrategy().Decide(clanA, world).ToList();

            Assert.AreEqual(1, commands.Count);
            var move = (MoveArmyCommand)commands[0];
            // 敵軍方向（Hex2）へ移動
            Assert.AreEqual(2, move.DestinationHexId);
        }
    }
}
