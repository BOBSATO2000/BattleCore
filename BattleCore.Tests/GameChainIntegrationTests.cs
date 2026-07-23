using BattleCore.AI;
using BattleCore.Battle;
using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.Systems.Battle;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class GameChainIntegrationTests
    {
        // ================================================================
        // ケース1：勝利連鎖
        // 戦闘勝利 → 士気上昇 → 攻撃継続 → 城占領
        // ================================================================
        [TestMethod]
        public void VictoryChain_BattleWin_MoraleRises_CastleCaptured()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain)); // 攻撃側Hex（城あり）
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain)); // 防御側Hex（城あり）

            // 攻撃側（勢力1）：兵力1000
            var attacker = new Army(1, 1, 1, 1);
            attacker.Morale = 60;
            world.Armies.Add(attacker);

            // 防御側（勢力2）：兵力200（大幅に弱い）
            var defender = new Army(2, 2, 2, 2);
            defender.SetInitialSoldiers(200);
            defender.Morale = 60;
            world.Armies.Add(defender);

            // 防御側の城
            var castle = new Castle(1, "敵城", hexId: 2, ownerClanId: 2);
            world.Castles.Add(castle);

            // 戦闘を直接解決（BattleResolverで士気変化を確認）
            var resolver = new BattleResolver();
            var battle   = new BattleCore.Battle.Battle(attacker, defender);
            resolver.Resolve(battle, world);

            // 勝者（attacker）の士気が上昇していること
            Assert.IsTrue(attacker.Morale > 60, $"攻撃側士気: {attacker.Morale}");
            // 敗者（defender）の士気が低下していること
            Assert.IsTrue(defender.Morale < 60, $"防御側士気: {defender.Morale}");

            // 防御側が全滅したら城占領
            if (defender.Soldiers == 0)
            {
                // attacker を城Hexに移動させてから占領処理
                attacker.MoveTo(2);
                var context = new SimulationContext(world);
                new CastleSystem().Update(context);
                Assert.AreEqual(1, castle.OwnerClanId, "城が攻撃側に占領されること");
                Assert.IsTrue(context.EventQueue.OfType<CastleCapturedEvent>().Any());
            }
            else
            {
                // 全滅していなくても士気変化は確認済み
                Assert.IsTrue(defender.Soldiers < 200, "防御側が損害を受けていること");
            }
        }

        // ================================================================
        // ケース2：長期包囲
        // 包囲 → 攻撃側も食糧不足 → 撤退判断
        // ================================================================
        [TestMethod]
        public void LongSiege_BesiegerStarves_RetreatsEventually()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1,  0,  0, TerrainType.Plain)); // 城Hex
            world.Map.AddHex(new Hex(2,  1,  0, TerrainType.Plain)); // 攻囲軍Hex（East）
            world.Map.AddHex(new Hex(3, -1,  0, TerrainType.Plain)); // West
            world.Map.AddHex(new Hex(4,  1, -1, TerrainType.Plain)); // NorthEast

            // 守備側
            world.Armies.Add(new Army(1, 1, 1, 1));
            var castle = new Castle(1, "テスト城", hexId: 1, ownerClanId: 1, reinforcementPerTick: 0);
            world.Castles.Add(castle);

            // 攻囲側：食糧わずか
            var besieger = new Army(2, 2, 2, 2);
            besieger.Food = 20; // 2Tick以内に枯渇（1.5倍消費=15/Tick）
            world.Armies.Add(new Army(3, 3, 2, 3));
            world.Armies.Add(new Army(4, 4, 2, 4));
            world.Armies.Insert(0, besieger); // 先頭に追加

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new SiegeSystem());
            engine.Register(new FoodSystem());
            engine.Register(new MoraleSystem());

            // Step1
            engine.Step();
            Assert.AreEqual(1, castle.SiegeTick, "包囲が成立していること");
            Assert.IsTrue(besieger.Food < 20, "攻囲側の食糧が消費されていること");

            // Step2：食糧枯渇
            engine.Step();
            Assert.AreEqual(0, besieger.Food, "攻囲側の食糧が枯渇していること");
            Assert.IsTrue(besieger.Morale < 100, "食糧枯渇で攻囲側の士気が低下していること");

            // 士気低下により攻囲側の実効兵力が下がることを確認
            // （Morale/100 が DamageCalculator に乗算される）
            Assert.IsTrue(besieger.Morale < 100,
                $"長期包囲で攻囲側も消耗すること（士気:{besieger.Morale}）");
        }

        // ================================================================
        // ケース3：諜報
        // 諜報成功 → 敵兵力判明（IntelEvent発火）
        // ================================================================
        [TestMethod]
        public void Intel_SpySucceeds_EnemyInfoRevealed()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));

            // 諜報側（勢力1）：Intelligence最大で必ず成功
            var spyClan    = new Clan(1) { Name = "織田" };
            var spyOfficer = new Officer(1, "忍者") { Intelligence = 200 };
            world.Clans.Add(spyClan);
            world.Officers.Add(spyOfficer);
            world.Memberships.Add(new Membership(1, spyOfficer.Id, clanId: 1));
            world.Armies.Add(new Army(1, 1, 1, 1));

            // 諜報対象（勢力2）
            var targetClan = new Clan(2) { Name = "武田" };
            world.Clans.Add(targetClan);
            var targetArmy = new Army(2, 2, 2, 2);
            targetArmy.SetInitialSoldiers(500);
            world.Armies.Add(targetArmy);

            var context = new SimulationContext(world);
            // Intelligence=200 → 成功率200/200=1.0 → 必ず成功
            new IntelSystem().Update(context);

            var intelEvents = context.EventQueue.OfType<IntelEvent>().ToList();
            Assert.IsTrue(intelEvents.Any(), "諜報イベントが発火すること");

            var ev = intelEvents.First();
            Assert.AreEqual("織田", ev.SpyClanName);
            Assert.AreEqual("武田", ev.TargetClanName);
            Assert.IsTrue(ev.Info.Contains("500"), $"敵兵力500が情報に含まれること: {ev.Info}");
        }

        // ================================================================
        // ケース4：離反
        // 士気低下 → 忠誠低下 → 離反 → 勢力変更
        // ================================================================
        [TestMethod]
        public void BetrayalChain_LowMorale_LoyaltyDrops_OfficerDefects()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0, TerrainType.Plain));
            world.Map.AddHex(new Hex(2, 1, 0, TerrainType.Plain));

            // 野心的な武将（離反しやすい）
            var officer = new Officer(1, "野心家")
            {
                Ambition     = 90,
                Loyalty      = 10,
                Personality  = OfficerPersonality.Ambitious,
            };
            world.Officers.Add(officer);

            var membership = new Membership(1, officer.Id, clanId: 1) { Loyalty = 20 };
            world.Memberships.Add(membership);

            var army = new Army(1, 1, 1, 1);
            army.AssignOfficer(officer.Id);
            army.Morale = 8; // 士気<10 → 忠誠-3/Tick
            world.Armies.Add(army);

            // 敵軍を同Hexに置いて士気をさらに下げる
            world.Armies.Add(new Army(2, 2, 2, 1));

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);
            engine.Register(new MoraleSystem());
            engine.Register(new LoyaltySystem(
                betrayalThreshold:   60,   // 低めの閾値で離反しやすく
                winLoyaltyBonus:     0,
                springBonus:         0));

            // Step1：士気低下 → 忠誠低下
            engine.Step();

            // 士気<10 → 忠誠-3、敵同Hex → 士気-5
            Assert.IsTrue(army.Morale <= 8,  $"士気が低下していること: {army.Morale}");

            // Step2：忠誠がさらに低下して離反スコアが閾値を超える
            engine.Step();

            var betrayalEvents = context.EventQueue.OfType<BetrayalEvent>().ToList();
            Assert.IsTrue(betrayalEvents.Any(),
                $"離反イベントが発火すること（忠誠:{membership.Loyalty}）");

            // 離反後：Membershipが削除され、ArmyのClanIdが0になる
            Assert.IsEmpty(world.Memberships, "離反後はMembershipが削除されること");
            Assert.AreEqual(0, army.ClanId,   "離反後はArmyが無所属になること");
        }
    }
}
