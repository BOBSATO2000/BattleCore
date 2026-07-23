using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.Vision;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class IntelAndSiegeReliefIntegrationTests
    {
        // ================================================================
        // ケース5：諜報→AI判断変更
        // Visionなし・Intelあり → AggressiveClanStrategy が Intel位置へ移動命令
        // ================================================================
        [TestMethod]
        public void Intel_NoVision_StrategyMovesToLastKnownPosition()
        {
            var world = new WorldState();
            // Hex1(自軍) ─ Hex2 ─ Hex3(敵の最後に判明した位置)
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(3, 2, 0, TerrainType.Plain));

            var clan = new Clan(1) { Name = "織田" };
            world.Clans.Add(clan);

            var army = new Army(1, 1, 1, 1); // 勢力1、Hex1に配置
            world.Armies.Add(army);

            // 敵軍（勢力2）はHex3にいる
            var enemyArmy = new Army(2, 2, 2, 3);
            world.Armies.Add(enemyArmy);

            // Vision は空（敵が見えていない）
            // Intel に「敵軍はHex3にいる」という情報を登録
            world.Intel[(clan.Id, enemyArmy.Id)] = new IntelData(
                ownerClanId:    clan.Id,
                enemyArmyId:    enemyArmy.Id,
                lastKnownHexId: 3,
                acquiredTick:   0);

            // AggressiveClanStrategy を実行
            var strategy = new AggressiveClanStrategy();
            var commands = strategy.Decide(clan, world).ToList();

            // Vision なし・Intel あり → Hex3方向（Hex2）への移動命令が出ること
            Assert.AreEqual(1, commands.Count, "移動命令が1件出ること");
            var move = commands[0] as MoveArmyCommand;
            Assert.IsNotNull(move);
            Assert.AreEqual(army.Id, move!.ArmyId);
            Assert.AreEqual(2, move.DestinationHexId,
                "Intel情報の敵位置(Hex3)に向かう経路の次Hex(Hex2)へ移動命令が出ること");
        }

        [TestMethod]
        public void Intel_UpdatedByIntelSystem_StrategyUsesLatestPosition()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(3, 2, 0, TerrainType.Plain));

            var clan      = new Clan(1) { Name = "織田" };
            var enemyClan = new Clan(2) { Name = "武田" };
            world.Clans.Add(clan);
            world.Clans.Add(enemyClan);

            // 諜報担当武将（Intelligence=200で必ず成功）
            var spy = new Officer(1, "忍者") { Intelligence = 200 };
            world.Officers.Add(spy);
            world.Memberships.Add(new Membership(1, spy.Id, clanId: 1));
            world.Armies.Add(new Army(1, 1, 1, 1));

            // 敵軍はHex3
            var enemyArmy = new Army(2, 2, 2, 3);
            world.Armies.Add(enemyArmy);

            // IntelSystemを実行 → Intel が更新される
            var context = new SimulationContext(world);
            new IntelSystem().Update(context);

            Assert.IsTrue(world.Intel.ContainsKey((clan.Id, enemyArmy.Id)),
                "IntelSystemがIntelを更新していること");
            Assert.AreEqual(3, world.Intel[(clan.Id, enemyArmy.Id)].LastKnownHexId,
                "敵の最後に判明した位置がHex3であること");

            // Vision なし状態でStrategyを実行
            var commands = new AggressiveClanStrategy().Decide(clan, world).ToList();
            var move     = commands.OfType<MoveArmyCommand>().FirstOrDefault();

            Assert.IsNotNull(move, "Intel情報を元に移動命令が出ること");
            Assert.AreEqual(2, move!.DestinationHexId, "Hex3方向（Hex2）へ向かうこと");
        }

        // ================================================================
        // ケース6：包囲解除→好循環
        // 包囲 → 援軍到着 → 補給路復活 → Food回復 → Morale回復
        // ================================================================
        [TestMethod]
        public void SiegeRelief_AllyArrives_SupplyRestored()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1,  0,  0, TerrainType.Plain)); // 城Hex
            world.Map.AddHex(new Hex(2,  1,  0, TerrainType.Plain)); // East
            world.Map.AddHex(new Hex(3, -1,  0, TerrainType.Plain)); // West
            world.Map.AddHex(new Hex(4,  1, -1, TerrainType.Plain)); // NorthEast

            var castle = new Castle(1, "テスト城", hexId: 1, ownerClanId: 1, reinforcementPerTick: 40);
            world.Castles.Add(castle);

            // 守備軍：食糧・士気が低下した状態
            var defender = new Army(1, 1, 1, 1);
            defender.Food   = 0;
            defender.Morale = 30;
            world.Armies.Add(defender);

            // 攻囲軍：East・West・NorthEastの3Hexを封鎖
            world.Armies.Add(new Army(2, 2, 2, 2));
            world.Armies.Add(new Army(3, 3, 2, 3));
            var besiegerHex4 = new Army(4, 4, 2, 4);
            world.Armies.Add(besiegerHex4);

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new SiegeSystem());
            engine.Register(new FoodSystem());
            engine.Register(new MoraleSystem());

            // Step1：包囲成立を確認
            engine.Step();
            Assert.AreEqual(1, castle.SiegeTick, "包囲が成立していること");

            // 援軍到着：Hex4の攻囲軍を撤退させる（補給路復活）
            besiegerHex4.MoveTo(99);

            // Step2：包囲解除 → 城補充再開
            engine.Step();

            Assert.AreEqual(0, castle.SiegeTick, "援軍到着で包囲が解除されること");
            Assert.IsTrue(defender.Food > 0,
                $"包囲解除後に城から食糧補充が再開されること（Food:{defender.Food}）");
        }
    }
}
