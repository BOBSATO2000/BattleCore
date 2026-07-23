using BattleCore.AI;
using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BattleCore.Tests
{
    /// <summary>
    /// 包囲 → 食糧不足 → 士気低下 → 忠誠低下 → 命令拒否
    /// の連鎖が一本につながることを検証する統合テスト。
    /// </summary>
    [TestClass]
    public class SiegeChainIntegrationTests
    {
        [TestMethod]
        public void SiegeChain_FoodStarves_MoraleFalls_LoyaltyDrops_OrderRefused()
        {
            // ── セットアップ ──────────────────────────────────────
            var world = new WorldState();

            // 城Hex(1)と隣接3Hex(2,3,4)
            world.Map.AddHex(new Hex(1,  0,  0, TerrainType.Plain)); // 城Hex
            world.Map.AddHex(new Hex(2,  1,  0, TerrainType.Plain)); // East
            world.Map.AddHex(new Hex(3, -1,  0, TerrainType.Plain)); // West
            world.Map.AddHex(new Hex(4,  1, -1, TerrainType.Plain)); // NorthEast

            // 守備側：勢力1、城Hexに駐屯
            var defender = new Officer(1, "守将") { Ambition = 20, Loyalty = 40, Personality = OfficerPersonality.Cautious };
            world.Officers.Add(defender);
            world.Memberships.Add(new Membership(1, defender.Id, clanId: 1) { Loyalty = 40 });

            var defArmy = new Army(1, 1, 1, 1);
            defArmy.AssignOfficer(defender.Id);
            defArmy.Food   = 15; // 食糧わずか（2Tick以内に枯渇）
            defArmy.Morale = 35; // 士気も低め
            world.Armies.Add(defArmy);

            // 城（勢力1所有）
            var castle = new Castle(1, "テスト城", hexId: 1, ownerClanId: 1, reinforcementPerTick: 0);
            world.Castles.Add(castle);

            // 攻囲側：勢力2、隣接3Hexを全て封鎖
            world.Armies.Add(new Army(2, 2, 2, 2));
            world.Armies.Add(new Army(3, 3, 2, 3));
            world.Armies.Add(new Army(4, 4, 2, 4));

            var context = new SimulationContext(world);
            var engine  = new SimulationEngine(context);

            // 連鎖に必要なSystemを登録（実際のゲームと同じ順序）
            engine.Register(new SiegeSystem());
            engine.Register(new FoodSystem());
            engine.Register(new MoraleSystem());
            engine.Register(new LoyaltySystem(
                betrayalThreshold:   200,  // 離反はさせない（命令拒否だけ確認）
                winLoyaltyBonus:     0,
                springBonus:         0));

            // ── Step1：包囲開始・食糧消費 ─────────────────────────
            engine.Step();

            Assert.AreEqual(1, castle.SiegeTick, "Step1: 包囲が成立していること");
            Assert.IsTrue(defArmy.Food < 15,     "Step1: 食糧が消費されていること");

            // ── Step2：食糧枯渇・士気低下 ─────────────────────────
            engine.Step();

            Assert.AreEqual(0, defArmy.Food,     "Step2: 食糧が枯渇していること");
            Assert.IsTrue(defArmy.Morale < 35,   "Step2: 士気が低下していること");

            // ── Step3以降：士気→忠誠の低下を確認 ────────────────
            var membershipBefore = world.Memberships
                .First(m => m.OfficerId == defender.Id).Loyalty;

            engine.Step();
            engine.Step();

            var membershipAfter = world.Memberships
                .First(m => m.OfficerId == defender.Id).Loyalty;

            Assert.IsTrue(membershipAfter < membershipBefore,
                $"士気低下が忠誠に波及していること（{membershipBefore}→{membershipAfter}）");

            // ── 命令拒否：忠誠が低い状態でOfficerDecisionを評価 ──
            var membership = world.Memberships.First(m => m.OfficerId == defender.Id);
            var od = new OfficerDecision(refusalLoyaltyThreshold: membership.Loyalty + 10);

            var clan = new Clan(1) { Name = "守備勢力" };
            world.Clans.Add(clan);

            var moveCmd = new Commands.MoveArmyCommand(defArmy.Id, 2);
            var results = od.Evaluate(new[] { moveCmd }, clan, world).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.IsFalse(results[0].Accepted,
                $"忠誠{membership.Loyalty}で命令が拒否されること");
            Assert.IsInstanceOfType(results[0].Event, typeof(OfficerRefusedOrderEvent));
        }
    }
}
