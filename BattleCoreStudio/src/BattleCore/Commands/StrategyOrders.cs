using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Simulation;
using BattleCore.Vision;
using System.Linq;

namespace BattleCore.Commands
{
    internal static class StrategyHelper
    {
        internal static bool ConsumeStrategyPoint(Officer? officer, int cost)
        {
            if (officer == null || officer.StrategyPoint < cost) return false;
            officer.StrategyPoint -= cost;
            return true;
        }
    }

    /// <summary>
    /// 火計。Forest/Camp/Palisade のある隣接Hexに火を放つ。
    /// 対象Hex上の敵軍: Food-30, Morale-15。構造物(Camp/Palisade)を破壊する。消費SP: 3。
    /// </summary>
    public sealed class FireStrategyOrder : ICommand
    {
        public int ArmyId      { get; }
        public int TargetHexId { get; }
        public FireStrategyOrder(int armyId, int targetHexId) { ArmyId = armyId; TargetHexId = targetHexId; }

        public void Execute(SimulationContext context)
        {
            var army    = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            var officer = army.OfficerId.HasValue
                ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
            if (!StrategyHelper.ConsumeStrategyPoint(officer, 3)) return;

            var toDestroy = context.World.Structures
                .Where(s => s.HexId == TargetHexId &&
                            (s.Type == StructureType.Camp || s.Type == StructureType.Palisade) &&
                            s.OwnerClanId != army.ClanId)
                .ToList();
            foreach (var s in toDestroy) context.World.Structures.Remove(s);

            foreach (var enemy in context.World.Armies.Where(a =>
                a.Soldiers > 0 && a.ClanId != army.ClanId && a.CurrentHexId == TargetHexId))
            {
                enemy.Food   = System.Math.Max(0, enemy.Food - 30);
                enemy.Morale = System.Math.Max(0, enemy.Morale - 15);
            }

            context.EventQueue.Enqueue(new StrategyEvent(
                ArmyId, officer?.Name ?? $"軍{ArmyId}", "火計", $"Hex{TargetHexId}に火を放つ"));
        }
    }

    /// <summary>
    /// 流言。対象勢力の武将の忠誠を低下させる。成功率: 発動者Intelligence vs 対象Intelligence。消費SP: 5。
    /// </summary>
    public sealed class RumorStrategyOrder : ICommand
    {
        public int ArmyId       { get; }
        public int TargetClanId { get; }
        public RumorStrategyOrder(int armyId, int targetClanId) { ArmyId = armyId; TargetClanId = targetClanId; }

        public void Execute(SimulationContext context)
        {
            var army    = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            var officer = army.OfficerId.HasValue
                ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
            if (!StrategyHelper.ConsumeStrategyPoint(officer, 5)) return;

            int myIntel = officer?.Intelligence ?? 50;
            var rng = new System.Random();

            foreach (var m in context.World.Memberships.Where(m => m.ClanId == TargetClanId))
            {
                var tgt = context.World.Officers.FirstOrDefault(o => o.Id == m.OfficerId);
                int tgtIntel = tgt?.Intelligence ?? 50;
                if (rng.NextDouble() < (double)myIntel / (myIntel + tgtIntel))
                    m.Loyalty = System.Math.Max(0, m.Loyalty - 10);
            }

            context.EventQueue.Enqueue(new StrategyEvent(
                ArmyId, officer?.Name ?? $"軍{ArmyId}", "流言", $"勢力{TargetClanId}の忠誠を揺さぶる"));
        }
    }

    /// <summary>
    /// 偽情報。AIが偽のHexへ向かうよう Intel を書き換える。消費SP: 4。
    /// </summary>
    public sealed class FakeIntelStrategyOrder : ICommand
    {
        public int ArmyId       { get; }
        public int TargetClanId { get; }
        public int FakeHexId    { get; }
        public FakeIntelStrategyOrder(int armyId, int targetClanId, int fakeHexId)
        { ArmyId = armyId; TargetClanId = targetClanId; FakeHexId = fakeHexId; }

        public void Execute(SimulationContext context)
        {
            var army    = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            var officer = army.OfficerId.HasValue
                ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
            if (!StrategyHelper.ConsumeStrategyPoint(officer, 4)) return;

            var key = (TargetClanId, army.Id);
            context.World.Intel[key] = new IntelData(TargetClanId, army.Id, FakeHexId, context.Time.Tick);

            context.EventQueue.Enqueue(new StrategyEvent(
                ArmyId, officer?.Name ?? $"軍{ArmyId}", "偽情報", $"勢力{TargetClanId}にHex{FakeHexId}の偽情報を流す"));
        }
    }

    /// <summary>
    /// 夜襲。夜のみ発動可能。奇襲態勢+同Hex敵士気-10。消費SP: 3。
    /// </summary>
    public sealed class NightRaidStrategyOrder : ICommand
    {
        public int ArmyId { get; }
        public NightRaidStrategyOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            if (!context.Time.IsNight) return;
            var army    = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            var officer = army.OfficerId.HasValue
                ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
            if (!StrategyHelper.ConsumeStrategyPoint(officer, 3)) return;

            army.Stance = ArmyStance.Ambush;
            foreach (var enemy in context.World.Armies.Where(a =>
                a.Soldiers > 0 && a.ClanId != army.ClanId && a.CurrentHexId == army.CurrentHexId))
                enemy.Morale = System.Math.Max(0, enemy.Morale - 10);

            context.EventQueue.Enqueue(new StrategyEvent(
                ArmyId, officer?.Name ?? $"軍{ArmyId}", "夜襲", "夜陰に乗じて奇襲"));
        }
    }

    /// <summary>
    /// 鼓舞。味方士気+15。消費SP: 2。
    /// </summary>
    public sealed class InspireStrategyOrder : ICommand
    {
        public int ArmyId { get; }
        public InspireStrategyOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army    = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            var officer = army.OfficerId.HasValue
                ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
            if (!StrategyHelper.ConsumeStrategyPoint(officer, 2)) return;

            army.Morale = System.Math.Min(100, army.Morale + 15);
            context.EventQueue.Enqueue(new StrategyEvent(
                ArmyId, officer?.Name ?? $"軍{ArmyId}", "鼓舞", "士気+15"));
        }
    }

    /// <summary>
    /// 内応。包囲中の城の忠誠が低い武将を寝返らせる。消費SP: 6。
    /// </summary>
    public sealed class OpenGateStrategyOrder : ICommand
    {
        public int ArmyId   { get; }
        public int CastleId { get; }
        public OpenGateStrategyOrder(int armyId, int castleId) { ArmyId = armyId; CastleId = castleId; }

        public void Execute(SimulationContext context)
        {
            var army    = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            var officer = army.OfficerId.HasValue
                ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
            if (!StrategyHelper.ConsumeStrategyPoint(officer, 6)) return;

            var castle = context.World.Castles.FirstOrDefault(c => c.Id == CastleId);
            if (castle == null || castle.SiegeTick == 0) return;

            var weakest = context.World.Memberships
                .Where(m => m.ClanId == castle.OwnerClanId)
                .OrderBy(m => m.Loyalty)
                .FirstOrDefault();
            if (weakest == null || weakest.Loyalty > 30) return;

            castle.OwnerClanId = army.ClanId;
            castle.SiegeTick   = 0;
            context.EventQueue.Enqueue(new StrategyEvent(
                ArmyId, officer?.Name ?? $"軍{ArmyId}", "内応", $"{castle.Name}の門が開く"));
        }
    }
}
