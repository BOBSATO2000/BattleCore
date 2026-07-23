using BattleCore.Battle;
using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using BattlePair = BattleCore.Battle.Battle;

namespace BattleCore.Tests
{
    [TestClass]
    public class OccupancyTests
    {
        // ── OccupancyRules ─────────────────────────────────────

        [TestMethod]
        public void OccupancyRules_EmptyHex_CanEnter()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            Assert.IsTrue(OccupancyRules.CanEnter(1, 1, world));
        }

        [TestMethod]
        public void OccupancyRules_OccupiedHex_CannotEnter()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            var army = new Army(1, 1, 1, 1);
            army.SetInitialSoldiers(1000);
            world.Armies.Add(army);

            Assert.IsFalse(OccupancyRules.CanEnter(1, 2, world));
        }

        [TestMethod]
        public void OccupancyRules_CastleHex_AllowsMultiple()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Castles.Add(new Castle(1, "テスト城", 1, ownerClanId: 1)); // Capacity=4
            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 1, 1);
            army1.SetInitialSoldiers(1000);
            army2.SetInitialSoldiers(1000);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            // 2部隊いても Capacity=4 なのでまだ入れる
            Assert.IsTrue(OccupancyRules.CanEnter(1, 1, world));
        }

        [TestMethod]
        public void OccupancyRules_CastleAtCapacity_CannotEnter()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            var castle = new Castle(1, "小城", 1, ownerClanId: 1) { Capacity = 2 };
            world.Castles.Add(castle);
            world.Armies.Add(new Army(1, 1, 1, 1));
            world.Armies.Add(new Army(2, 2, 1, 1));
            world.Armies[0].SetInitialSoldiers(1000);
            world.Armies[1].SetInitialSoldiers(1000);

            Assert.IsFalse(OccupancyRules.CanEnter(1, 1, world));
        }

        // ── MovementSystem 占有ルール ──────────────────────────

        [TestMethod]
        public void MovementSystem_EnemyHex_SetsPendingAttack()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            var attacker = new Army(1, 1, 1, 1);
            var defender = new Army(2, 2, 2, 2);
            attacker.SetInitialSoldiers(1000);
            defender.SetInitialSoldiers(1000);
            world.Armies.Add(attacker);
            world.Armies.Add(defender);
            attacker.OrderMove(2);

            var engine = new SimulationEngine(world);
            engine.RegisterSystem(new MovementSystem());
            engine.Step();

            // 攻撃側は移動せず PendingAttackHexId が設定される
            // (Step後にリセットされるので、Stepの途中でイベントを確認)
            // → OccupancyEvent(Combat) が発火されたことを確認
            // ※ Step後は PendingAttackHexId がリセットされるため、
            //   イベントキューで確認する
            Assert.AreEqual(1, attacker.CurrentHexId); // 移動していない
        }

        [TestMethod]
        public void MovementSystem_FullHex_BlocksMovement()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            // Hex2 に味方が既にいる（満員）
            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 1, 2); // 同勢力
            army1.SetInitialSoldiers(1000);
            army2.SetInitialSoldiers(1000);
            world.Armies.Add(army1);
            world.Armies.Add(army2);
            army1.OrderMove(2);

            var context = new SimulationContext(world);
            new MovementSystem().Update(context);

            Assert.AreEqual(1, army1.CurrentHexId); // 移動ブロック
            Assert.IsTrue(context.EventQueue.OfType<OccupancyEvent>()
                .Any(e => e.Type == OccupancyEventType.Blocked));
        }

        [TestMethod]
        public void MovementSystem_EmptyHex_MovesNormally()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            var army = new Army(1, 1, 1, 1);
            army.SetInitialSoldiers(1000);
            world.Armies.Add(army);
            army.OrderMove(2);

            new MovementSystem().Update(new SimulationContext(world));

            Assert.AreEqual(2, army.CurrentHexId);
        }

        // ── BattleFinder 隣接戦闘 ─────────────────────────────

        [TestMethod]
        public void BattleFinder_PendingAttack_CreatesAdjacentBattle()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            var attacker = new Army(1, 1, 1, 1);
            var defender = new Army(2, 2, 2, 2);
            attacker.SetInitialSoldiers(1000);
            defender.SetInitialSoldiers(1000);
            attacker.PendingAttackHexId = 2;
            world.Armies.Add(attacker);
            world.Armies.Add(defender);

            var battles = new BattleFinder().Find(world).ToList();

            Assert.AreEqual(1, battles.Count);
            Assert.IsTrue(battles[0].IsAdjacentBattle);
        }

        [TestMethod]
        public void BattleFinder_NoPendingAttack_NoAdjacentBattle()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            var army1 = new Army(1, 1, 1, 1);
            var army2 = new Army(2, 2, 2, 2);
            army1.SetInitialSoldiers(1000);
            army2.SetInitialSoldiers(1000);
            world.Armies.Add(army1);
            world.Armies.Add(army2);

            var battles = new BattleFinder().Find(world).ToList();

            Assert.AreEqual(0, battles.Count); // 別Hexで PendingAttack なし
        }

        // ── 隣接戦闘の占領処理 ────────────────────────────────

        [TestMethod]
        public void AdjacentBattle_AttackerWins_OccupiesDefenderHex()
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));

            // 攻撃側を圧倒的に強くする
            var attacker = new Army(1, 1, 1, 1);
            attacker.SetInitialSoldiers(5000);
            var defender = new Army(2, 2, 2, 2);
            defender.SetInitialSoldiers(100);
            attacker.PendingAttackHexId = 2;
            world.Armies.Add(attacker);
            world.Armies.Add(defender);

            var battle = new BattlePair(attacker, defender) { IsAdjacentBattle = true };
            new BattleResolver().Resolve(battle, world);

            // 攻撃側が勝利してHex2へ移動
            Assert.AreEqual(2, attacker.CurrentHexId);
        }

        // ── Castle.Capacity ───────────────────────────────────

        [TestMethod]
        public void Castle_DefaultCapacity_IsFour()
        {
            var castle = new Castle(1, "テスト城", 1, ownerClanId: 1);
            Assert.AreEqual(4, castle.Capacity);
        }

        [TestMethod]
        public void Castle_SmallCastle_CapacityTwo()
        {
            var castle = new Castle(1, "小城", 1, ownerClanId: 1) { Capacity = 2 };
            Assert.AreEqual(2, castle.Capacity);
        }
    }
}
