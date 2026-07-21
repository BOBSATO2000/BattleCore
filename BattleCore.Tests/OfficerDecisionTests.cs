using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Systems;
using BattleCore.World;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Tests
{
    [TestClass]
    public class OfficerDecisionTests
    {
        private static (WorldState world, Clan clan, Army army, Officer officer) BuildBase(
            OfficerPersonality personality = OfficerPersonality.Loyal,
            int loyalty = 80,
            int soldiers = 1000)
        {
            var world = new WorldState();
            world.Map.AddHex(new Hex(1, 0, 0));
            world.Map.AddHex(new Hex(2, 1, 0));
            world.Map.AddHex(new Hex(3, 2, 0));

            var clan = new Clan(1) { Name = "織田" };
            world.Clans.Add(clan);

            var officer = new Officer(1, "テスト武将")
            {
                Personality = personality,
                Loyalty     = loyalty,
            };
            world.Officers.Add(officer);

            var army = new Army(1, 0, 1, 1);
            army.AssignOfficer(1);
            army.LoseSoldiers(1000 - soldiers);
            world.Armies.Add(army);

            world.Memberships.Add(new Membership(1, 1, 1) { Loyalty = loyalty });

            return (world, clan, army, officer);
        }

        // ── ① 命令拒否 ──────────────────────────────────────────

        [TestMethod]
        public void LowLoyalty_NonLoyal_RefusesOrder()
        {
            var (world, clan, army, _) = BuildBase(
                personality: OfficerPersonality.Ambitious, loyalty: 15);

            var od  = new OfficerDecision(refusalLoyaltyThreshold: 20);
            var cmd = new MoveArmyCommand(army.Id, 2);

            var (cmds, evs) = od.Filter(new[] { cmd }, clan, world);

            Assert.AreEqual(0, cmds.Count);
            Assert.AreEqual(1, evs.OfType<OfficerRefusedOrderEvent>().Count());
        }

        [TestMethod]
        public void LowLoyalty_Loyal_StillFollowsOrder()
        {
            // Loyal性格は忠誠が低くても命令を拒否しない
            var (world, clan, army, _) = BuildBase(
                personality: OfficerPersonality.Loyal, loyalty: 10);

            var od  = new OfficerDecision(refusalLoyaltyThreshold: 20);
            var cmd = new MoveArmyCommand(army.Id, 2);

            var (cmds, evs) = od.Filter(new[] { cmd }, clan, world);

            Assert.AreEqual(1, cmds.Count);
            Assert.AreEqual(0, evs.OfType<OfficerRefusedOrderEvent>().Count());
        }

        // ── ② 慎重性格の撤退進言 ────────────────────────────────

        [TestMethod]
        public void Cautious_LowSoldiers_RequestsRetreat()
        {
            var (world, clan, army, _) = BuildBase(
                personality: OfficerPersonality.Cautious, soldiers: 200);

            // 自城はHex3（軍はHex1にいるので別Hex）
            world.Castles.Add(new Castle(1, "自城", 3, 1, 50));

            var od  = new OfficerDecision(cautiousRetreatSoldiers: 300);
            var cmd = new MoveArmyCommand(army.Id, 2); // 前進命令

            var (cmds, evs) = od.Filter(new[] { cmd }, clan, world);

            Assert.AreEqual(1, cmds.Count);
            var move = cmds[0] as MoveArmyCommand;
            Assert.AreEqual(DecisionReason.CautiousRetreat, move!.Reason);
            Assert.AreEqual(1, evs.OfType<OfficerRequestedRetreatEvent>().Count());
        }

        [TestMethod]
        public void Cautious_HighSoldiers_FollowsOrder()
        {
            var (world, clan, army, _) = BuildBase(
                personality: OfficerPersonality.Cautious, soldiers: 800);

            var od  = new OfficerDecision(cautiousRetreatSoldiers: 300);
            var cmd = new MoveArmyCommand(army.Id, 3);

            var (cmds, evs) = od.Filter(new[] { cmd }, clan, world);

            Assert.AreEqual(1, cmds.Count);
            Assert.AreEqual(DecisionReason.Advance, (cmds[0] as MoveArmyCommand)!.Reason);
            Assert.AreEqual(0, evs.OfType<OfficerRequestedRetreatEvent>().Count());
        }

        // ── ③ 野心的独断行動 ────────────────────────────────────

        [TestMethod]
        public void Ambitious_LowLoyalty_TakesIndependentAction()
        {
            var (world, clan, army, _) = BuildBase(
                personality: OfficerPersonality.Ambitious, loyalty: 30);

            // 敵城を追加
            world.Castles.Add(new Castle(1, "敵城", 3, 2, 50));

            var od  = new OfficerDecision(independentActionLoyalty: 35);
            var cmd = new MoveArmyCommand(army.Id, 2);

            var (cmds, evs) = od.Filter(new[] { cmd }, clan, world);

            Assert.AreEqual(1, cmds.Count);
            Assert.AreEqual(DecisionReason.IndependentAction,
                (cmds[0] as MoveArmyCommand)!.Reason);
        }

        // ── ④ 主君への不満 ──────────────────────────────────────

        [TestMethod]
        public void Dissatisfied_HighDislike_ChangesCommand()
        {
            var (world, clan, army, officer) = BuildBase();

            // 主君を設定
            var daimyo = new Officer(99, "主君");
            world.Officers.Add(daimyo);
            clan.DaimyoOfficerId = 99;

            // 主君への Dislike を高く設定
            world.Relationships.Add(new Relationship(1, officer.Id, 99)
                { Dislike = 70 });

            // 自城を追加（撤退先）
            world.Castles.Add(new Castle(1, "自城", 3, 1, 50));

            var od  = new OfficerDecision(dissatisfiedDislikeThreshold: 60);
            var cmd = new MoveArmyCommand(army.Id, 2);

            var (cmds, evs) = od.Filter(new[] { cmd }, clan, world);

            Assert.AreEqual(1, cmds.Count);
            Assert.AreEqual(DecisionReason.Dissatisfied,
                (cmds[0] as MoveArmyCommand)!.Reason);
        }

        // ── ⑤ 通常（変更なし）───────────────────────────────────

        [TestMethod]
        public void Normal_HighLoyalty_PassesThrough()
        {
            var (world, clan, army, _) = BuildBase(loyalty: 90);

            var od  = new OfficerDecision();
            var cmd = new MoveArmyCommand(army.Id, 2);

            var (cmds, evs) = od.Filter(new[] { cmd }, clan, world);

            Assert.AreEqual(1, cmds.Count);
            Assert.AreEqual(DecisionReason.Advance,
                (cmds[0] as MoveArmyCommand)!.Reason);
            Assert.AreEqual(0, evs.Count);
        }

        // ── DecisionReason ───────────────────────────────────────

        [TestMethod]
        public void MoveArmyCommand_DefaultReason_IsAdvance()
        {
            var cmd = new MoveArmyCommand(1, 2);
            Assert.AreEqual(DecisionReason.Advance, cmd.Reason);
        }

        [TestMethod]
        public void MoveArmyCommand_ExplicitReason_IsPreserved()
        {
            var cmd = new MoveArmyCommand(1, 2, DecisionReason.Retreat);
            Assert.AreEqual(DecisionReason.Retreat, cmd.Reason);
        }

        // ── ClanDecisionSystem 2層統合 ───────────────────────────

        [TestMethod]
        public void ClanDecisionSystem_OfficerFiltersCommand()
        {
            // 忠誠が低い Ambitious 武将は命令を拒否する
            var (world, clan, army, _) = BuildBase(
                personality: OfficerPersonality.Ambitious, loyalty: 10);

            // 敵勢力を追加
            var clanB = new Clan(2) { Name = "武田" };
            world.Clans.Add(clanB);
            var enemyArmy = new Army(2, 0, 2, 3);
            world.Armies.Add(enemyArmy);

            // Visions を設定（Fog of War 対応）
            var v = new BattleCore.Vision.VisionData(army.Id);
            v.VisibleHexes.Add(1); v.VisibleHexes.Add(2); v.VisibleHexes.Add(3);
            world.Visions[army.Id] = v;

            var context = new BattleCore.Simulation.SimulationContext(world);
            var system  = new ClanDecisionSystem(
                new AggressiveClanStrategy(),
                new OfficerDecision(refusalLoyaltyThreshold: 20));

            system.Update(context);

            // 命令拒否されたので CommandQueue は空
            Assert.AreEqual(0, context.CommandQueue.Count);
            // OfficerRefusedOrderEvent が積まれている
            Assert.AreEqual(1, context.EventQueue.OfType<OfficerRefusedOrderEvent>().Count());
        }
    }
}
