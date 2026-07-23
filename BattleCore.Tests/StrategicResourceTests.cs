using BattleCore.AI;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class StrategicResourceTests
    {
        // ── River ──────────────────────────────────────────────

        [TestMethod]
        public void River_WithoutBridge_AppliesCooldown()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.River));
            var army = new Army(1, 1, 1, 1);
            world.Armies.Add(army);
            army.OrderMove(2);

            var engine = new SimulationEngine(world);
            engine.RegisterSystem(new MovementSystem());
            engine.Step();

            Assert.AreEqual(2, army.CurrentHexId);
            Assert.AreEqual(2, army.MoveCooldown); // 渡河コスト
        }

        [TestMethod]
        public void River_WithBridge_NoCooldown()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.River));
            world.Structures.Add(new Structure(1, StructureType.Bridge, 2, 0));
            var army = new Army(1, 1, 1, 1);
            world.Armies.Add(army);
            army.OrderMove(2);

            var engine = new SimulationEngine(world);
            engine.RegisterSystem(new MovementSystem());
            engine.Step();

            Assert.AreEqual(2, army.CurrentHexId);
            Assert.AreEqual(0, army.MoveCooldown);
        }

        // ── Road ───────────────────────────────────────────────

        [TestMethod]
        public void Road_ZeroApCost_NoApConsumed()
        {
            // Road があれば AP 消費なし → 移動後も AP が減らない
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Structures.Add(new Structure(1, StructureType.Road, 2, 0));
            var army = new Army(1, 1, 1, 1);
            world.Armies.Add(army);
            army.OrderMove(2);

            var engine = new SimulationEngine(world);
            engine.RegisterSystem(new MovementSystem());
            engine.Step();

            Assert.AreEqual(2, army.CurrentHexId);
            // Road なので AP 消費なし：初期 AP=2、次ターンのリセット前はまだ 2
            Assert.AreEqual(2, army.ActionPoints);
        }

        // ── Well ───────────────────────────────────────────────

        [TestMethod]
        public void Well_UnderSiege_HalvesConsumption()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(3, -1, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(4, 1, -1, TerrainType.Plain));

            var castle = new Castle(1, "テスト城", 1, ownerClanId: 1);
            castle.SiegeTick = 1;
            world.Castles.Add(castle);

            world.Structures.Add(new Structure(1, StructureType.Well, 1, 1));

            var army = new Army(1, 1, 1, 1);
            army.Food = 60;
            world.Armies.Add(army);

            new FoodSystem().Update(new SimulationContext(world));

            // 包囲中2倍消費(20) → Well で半減(10)
            Assert.AreEqual(50, army.Food);
        }

        [TestMethod]
        public void Well_NotUnderSiege_NoEffect()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Structures.Add(new Structure(1, StructureType.Well, 1, 1));
            var army = new Army(1, 1, 1, 1);
            army.Food = 60;
            world.Armies.Add(army);

            new FoodSystem().Update(new SimulationContext(world));

            Assert.AreEqual(50, army.Food); // 通常消費のみ
        }

        [TestMethod]
        public void Well_IncreaseSiegeThreshold()
        {
            // Well があれば降伏閾値+8 → 10+8=18Tick で降伏
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(3, -1, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(4, 1, -1, TerrainType.Plain));

            var castle = new Castle(1, "テスト城", 1, ownerClanId: 1);
            castle.SiegeTick = 9; // 通常なら次Tickで降伏
            world.Castles.Add(castle);
            world.Structures.Add(new Structure(1, StructureType.Well, 1, 1));

            var army1 = new Army(1, 1, 2, 2);
            var army2 = new Army(2, 2, 2, 3);
            var army3 = new Army(3, 3, 2, 4);
            world.Armies.Add(army1);
            world.Armies.Add(army2);
            world.Armies.Add(army3);

            new SiegeSystem().Update(new SimulationContext(world));

            // Well があるので降伏しない（SiegeTick=10、閾値18）
            Assert.AreEqual(1, world.Castles[0].OwnerClanId);
            Assert.AreEqual(10, world.Castles[0].SiegeTick);
        }

        // ── Shrine ─────────────────────────────────────────────

        [TestMethod]
        public void Shrine_SameHexSameClan_MoraleBonus()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Structures.Add(new Structure(1, StructureType.Shrine, 1, 1));
            var army = new Army(1, 1, 1, 1);
            army.Morale = 80;
            world.Armies.Add(army);

            new StrategicPointSystem().Update(new SimulationContext(world));

            Assert.AreEqual(83, army.Morale); // +3
        }

        [TestMethod]
        public void Shrine_EnemyClan_NoBonus()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Structures.Add(new Structure(1, StructureType.Shrine, 1, ownerClanId: 2));
            var army = new Army(1, 1, 1, 1); // ClanId=1
            army.Morale = 80;
            world.Armies.Add(army);

            new StrategicPointSystem().Update(new SimulationContext(world));

            Assert.AreEqual(80, army.Morale); // 変化なし
        }

        [TestMethod]
        public void Shrine_MoraleCapAt100()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Structures.Add(new Structure(1, StructureType.Shrine, 1, 1));
            var army = new Army(1, 1, 1, 1);
            army.Morale = 99;
            world.Armies.Add(army);

            new StrategicPointSystem().Update(new SimulationContext(world));

            Assert.AreEqual(100, army.Morale); // 上限100
        }

        // ── StrategyEvaluator ──────────────────────────────────

        [TestMethod]
        public void StrategyEvaluator_WithdrawRule_LowFood()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Castles.Add(new Castle(1, "自城", 1, ownerClanId: 1));
            var clan = new Clan(1) { Name = "織田" };
            var army = new Army(1, 1, 1, 1);
            army.Food = 20; // 閾値25以下
            army.SetInitialSoldiers(1000);
            world.Armies.Add(army);
            world.Clans.Add(clan);

            var evaluator = new StrategyEvaluator();
            var plan = evaluator.Evaluate(army, clan, world);

            Assert.IsNotNull(plan);
            Assert.AreEqual(CampaignGoal.Withdraw, plan!.Goal);
        }

        [TestMethod]
        public void StrategyEvaluator_CaptureCastleRule_EnemyCastleVisible()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Castles.Add(new Castle(1, "敵城", 2, ownerClanId: 2));
            var clan = new Clan(1) { Name = "織田" };
            var army = new Army(1, 1, 1, 1);
            army.Food = 80;
            army.SetInitialSoldiers(1000);
            world.Armies.Add(army);
            world.Clans.Add(clan);
            // 視界に敵城を入れる
            var vision = new BattleCore.Vision.VisionData(1);
            vision.VisibleHexes.Add(2);
            world.Visions[1] = vision;

            var evaluator = new StrategyEvaluator();
            var plan = evaluator.Evaluate(army, clan, world);

            Assert.IsNotNull(plan);
            Assert.AreEqual(CampaignGoal.CaptureCastle, plan!.Goal);
            Assert.AreEqual(2, plan.TargetHexId);
        }

        [TestMethod]
        public void StrategyEvaluator_ExistingPlan_ContinuesIfValid()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Castles.Add(new Castle(1, "敵城", 2, ownerClanId: 2));
            var clan = new Clan(1) { Name = "織田" };
            var army = new Army(1, 1, 1, 1);
            army.Food = 80;
            army.SetInitialSoldiers(1000);
            world.Armies.Add(army);
            world.Clans.Add(clan);

            var existing = new CampaignPlan(CampaignGoal.CaptureCastle, 2, 3);
            world.CampaignPlans[army.Id] = existing;

            var evaluator = new StrategyEvaluator();
            var plan = evaluator.Evaluate(army, clan, world);

            Assert.IsNotNull(plan);
            Assert.AreEqual(CampaignGoal.CaptureCastle, plan!.Goal);
            Assert.AreEqual(2, plan.RemainingTurns); // 3-1=2
        }

        [TestMethod]
        public void StrategyEvaluator_PlanExpired_GeneratesNew()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Castles.Add(new Castle(1, "敵城", 2, ownerClanId: 2));
            var clan = new Clan(1) { Name = "織田" };
            var army = new Army(1, 1, 1, 1);
            army.Food = 80;
            army.SetInitialSoldiers(1000);
            world.Armies.Add(army);
            world.Clans.Add(clan);
            var vision = new BattleCore.Vision.VisionData(1);
            vision.VisibleHexes.Add(2);
            world.Visions[1] = vision;

            // 期限切れの Plan
            var expired = new CampaignPlan(CampaignGoal.Reconnaissance, 2, 0);
            world.CampaignPlans[army.Id] = expired;

            var evaluator = new StrategyEvaluator();
            var plan = evaluator.Evaluate(army, clan, world);

            Assert.IsNotNull(plan);
            Assert.AreEqual(CampaignGoal.CaptureCastle, plan!.Goal); // 新しい Plan
        }

        [TestMethod]
        public void StrategyEvaluator_SecureStrategicPoint_WellVisible()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Structures.Add(new Structure(1, StructureType.Well, 2, ownerClanId: 2));
            var clan = new Clan(1) { Name = "織田" };
            var army = new Army(1, 1, 1, 1);
            army.Food = 80;
            army.SetInitialSoldiers(1000);
            world.Armies.Add(army);
            world.Clans.Add(clan);
            var vision = new BattleCore.Vision.VisionData(1);
            vision.VisibleHexes.Add(2);
            world.Visions[1] = vision;

            var evaluator = new StrategyEvaluator();
            var plan = evaluator.Evaluate(army, clan, world);

            Assert.IsNotNull(plan);
            Assert.AreEqual(CampaignGoal.SecureStrategicPoint, plan!.Goal);
            Assert.AreEqual(2, plan.TargetHexId);
        }
    }
}
