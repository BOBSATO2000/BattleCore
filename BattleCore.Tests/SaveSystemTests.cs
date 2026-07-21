using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Save;
using BattleCore.Simulation;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace BattleCore.Tests
{
    [TestClass]
    public class SaveSystemTests
    {
        private static SimulationContext BuildContext()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Forest));
            world.Map.AddHex(new Hex(3, 2, 0, TerrainType.Mountain));

            var clanA = new Clan(1) { Name = "織田", Gold = 500, IsPlayerControlled = true };
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.AddRange(new[] { clanA, clanB });

            var army = new Army(1, 0, 1, 1);
            army.LoseSoldiers(200); // 800兵
            army.OrderMove(2);
            world.Armies.Add(army);

            world.Castles.Add(new Castle(1, "清洲城", 1, 1, 50));

            var context = new SimulationContext(world);
            context.CurrentPhase = TurnPhase.Battle;

            // 時間を3ステップ進める
            context.Time.Advance();
            context.Time.Advance();
            context.Time.Advance();

            return context;
        }

        [TestMethod]
        public void Save_CreatesFile()
        {
            var path = Path.GetTempFileName();
            try
            {
                var context = BuildContext();
                SaveSystem.Save(context, "sengoku1560", path);
                Assert.IsTrue(File.Exists(path));
                Assert.IsTrue(new FileInfo(path).Length > 0);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveAndLoad_RestoresTick()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                int expectedTick = original.Time.Tick;

                SaveSystem.Save(original, "sengoku1560", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(expectedTick, loaded.Time.Tick);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveAndLoad_RestoresPhase()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "sengoku1560", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(TurnPhase.Battle, loaded.CurrentPhase);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveAndLoad_RestoresArmySoldiers()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "sengoku1560", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(1, loaded.World.Armies.Count);
                Assert.AreEqual(800, loaded.World.Armies[0].Soldiers);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveAndLoad_RestoresArmyDestination()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "sengoku1560", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(2, loaded.World.Armies[0].DestinationHexId);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveAndLoad_RestoresClanData()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "sengoku1560", path);
                var (loaded, scenarioId) = SaveSystem.Load(path);

                Assert.AreEqual("sengoku1560", scenarioId);
                Assert.AreEqual(2, loaded.World.Clans.Count);
                Assert.AreEqual("織田", loaded.World.Clans[0].Name);
                Assert.IsTrue(loaded.World.Clans[0].IsPlayerControlled);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveAndLoad_RestoresCastle()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "sengoku1560", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(1, loaded.World.Castles.Count);
                Assert.AreEqual("清洲城", loaded.World.Castles[0].Name);
                Assert.AreEqual(1, loaded.World.Castles[0].OwnerClanId);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveAndLoad_RestoresMapHexes()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "sengoku1560", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(3, loaded.World.Map.Hexes.Count);
                Assert.AreEqual(TerrainType.Forest,   loaded.World.Map.Hexes[1].Terrain);
                Assert.AreEqual(TerrainType.Mountain, loaded.World.Map.Hexes[2].Terrain);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void LoadMetadata_ReturnsCorrectInfo()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "osaka1615", path);
                var meta = SaveSystem.LoadMetadata(path);

                Assert.AreEqual(SaveSystem.CurrentVersion, meta.Version);
                Assert.AreEqual("osaka1615", meta.ScenarioId);
                Assert.AreEqual(TurnPhase.Battle.ToString(), meta.Phase);
            }
            finally { File.Delete(path); }
        }

        // -------------------------------------------------------
        // SaveLoad統合テスト（Army数/Clan数/Turn/Phase/AP/Vision一致）
        // -------------------------------------------------------

        [TestMethod]
        public void SaveLoad_ArmyCount_Matches()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                // 軍を追加
                var army2 = new Army(2, 0, 2, 2);
                original.World.Armies.Add(army2);

                SaveSystem.Save(original, "test", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(original.World.Armies.Count, loaded.World.Armies.Count);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveLoad_ClanCount_Matches()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "test", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(original.World.Clans.Count, loaded.World.Clans.Count);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveLoad_Turn_Matches()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "test", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(original.Time.Tick, loaded.Time.Tick);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveLoad_Phase_Matches()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                original.CurrentPhase = TurnPhase.Supply;
                SaveSystem.Save(original, "test", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(TurnPhase.Supply, loaded.CurrentPhase);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveLoad_ActionPoints_Matches()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                original.World.Armies[0].ActionPoints = 2; // APを消費済みに設定
                SaveSystem.Save(original, "test", path);
                var (loaded, _) = SaveSystem.Load(path);

                Assert.AreEqual(2, loaded.World.Armies[0].ActionPoints);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SaveLoad_Vision_IsEmpty_OnLoad()
        {
            // VisionはVisionSystemが毎Step再計算するため、
            // ロード直後は空であることを確認する。
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                // Visionを手動設定してからセーブ
                original.World.Visions[1] = new BattleCore.Vision.VisionData(1);
                original.World.Visions[1].VisibleHexes.Add(1);

                SaveSystem.Save(original, "test", path);
                var (loaded, _) = SaveSystem.Load(path);

                // ロード後はVisionは空（VisionSystemが次Stepで再計算）
                Assert.AreEqual(0, loaded.World.Visions.Count);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void LoadMetadata_EngineVersion_IsSet()
        {
            var path = Path.GetTempFileName();
            try
            {
                var original = BuildContext();
                SaveSystem.Save(original, "test", path);
                var meta = SaveSystem.LoadMetadata(path);

                Assert.AreEqual(SaveSystem.EngineVersion, meta.EngineVersion);
                Assert.IsFalse(string.IsNullOrEmpty(meta.EngineVersion));
            }
            finally { File.Delete(path); }
        }
    }
}
