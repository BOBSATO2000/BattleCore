using BattleCore.Entities;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Commands
{
    /// <summary>
    /// 防御態勢命令。被ダメ-20%、このTickは移動しない。
    /// BattleModifiers の DefendModifier が参照する。
    /// </summary>
    public sealed class DefendOrder : ICommand
    {
        public int ArmyId { get; }
        public DefendOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            army.Stance = ArmyStance.Defend;
            army.ClearDestination();
        }
    }

    /// <summary>
    /// 迎撃命令。隣接Hexに敵が来たら自動突撃（先制ボーナス）。
    /// BattleFinder が迎撃態勢の軍を優先的に攻撃側として扱う。
    /// </summary>
    public sealed class InterceptOrder : ICommand
    {
        public int ArmyId { get; }
        public InterceptOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            army.Stance = ArmyStance.Intercept;
            army.ClearDestination();
        }
    }

    /// <summary>
    /// 包囲命令。指定した城の隣接Hexへ移動する。
    /// </summary>
    public sealed class SiegeOrder : ICommand
    {
        public int ArmyId    { get; }
        public int CastleId  { get; }
        public SiegeOrder(int armyId, int castleId) { ArmyId = armyId; CastleId = castleId; }

        public void Execute(SimulationContext context)
        {
            var army   = context.World.GetArmyById(ArmyId);
            var castle = context.World.Castles.FirstOrDefault(c => c.Id == CastleId);
            if (army == null || castle == null) return;

            // 城の隣接Hexのうち、まだ自軍がいないHexへ移動
            var neighbors = context.World.Map.GetNeighbors(castle.HexId);
            var target = neighbors.FirstOrDefault(h =>
                !context.World.Armies.Any(a => a.ClanId == army.ClanId && a.CurrentHexId == h.Id));
            if (target != null)
            {
                army.OrderMove(target.Id);
                army.Stance = ArmyStance.Siege;
            }
        }
    }

    /// <summary>
    /// 撤退命令。最寄りの自軍城へ移動する。城がなければ現在地で待機。
    /// </summary>
    public sealed class RetreatOrder : ICommand
    {
        public int ArmyId { get; }
        public RetreatOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;

            var friendlyCastle = context.World.Castles
                .Where(c => c.OwnerClanId == army.ClanId)
                .OrderBy(c => BattleCore.Map.HexDistance.Calculate(
                    context.World.Map.GetHexById(army.CurrentHexId)!,
                    context.World.Map.GetHexById(c.HexId)!))
                .FirstOrDefault();

            if (friendlyCastle != null)
                army.OrderMove(friendlyCastle.HexId);

            army.Stance = ArmyStance.Retreating;
        }
    }

    /// <summary>
    /// 奇襲命令。Forest/Mountain に潜伏し、先制ボーナスを得る。
    /// AmbushModifier が Stance=Ambush かつ地形が Forest/Mountain の場合に先制補正を適用する。
    /// </summary>
    public sealed class AmbushOrder : ICommand
    {
        public int ArmyId { get; }
        public AmbushOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            army.Stance = ArmyStance.Ambush;
            army.ClearDestination();
        }
    }

    /// <summary>
    /// 構造物建設命令。現在のHexに指定した構造物を建設する。
    /// </summary>
    public sealed class BuildOrder : ICommand
    {
        public int           ArmyId        { get; }
        public StructureType StructureType { get; }
        public BuildOrder(int armyId, StructureType type) { ArmyId = armyId; StructureType = type; }

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null || army.Soldiers == 0) return;

            // 同Hexに同種の構造物が既にある場合はスキップ
            bool exists = context.World.Structures.Any(s =>
                s.HexId == army.CurrentHexId && s.Type == StructureType && s.OwnerClanId == army.ClanId);
            if (exists) return;

            int newId = context.World.Structures.Count > 0
                ? context.World.Structures.Max(s => s.Id) + 1 : 1;
            context.World.Structures.Add(new Structure(newId, StructureType, army.CurrentHexId, army.ClanId));

            var officer = army.OfficerId.HasValue
                ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
            context.EventQueue.Enqueue(new BattleCore.Events.StructureEvent(
                army.Id, officer?.Name ?? $"軍{army.Id}",
                BattleCore.Events.StructureEventType.Built,
                StructureType.ToString(), army.CurrentHexId));
        }
    }

    /// <summary>
    /// 構造物破壊命令。現在のHexにある敵の構造物を破壊する。
    /// </summary>
    public sealed class DestroyOrder : ICommand
    {
        public int ArmyId { get; }
        public DestroyOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;

            var target = context.World.Structures.FirstOrDefault(s =>
                s.HexId == army.CurrentHexId && s.OwnerClanId != army.ClanId);
            if (target == null) return;

            context.World.Structures.Remove(target);

            var officer = army.OfficerId.HasValue
                ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
            context.EventQueue.Enqueue(new BattleCore.Events.StructureEvent(
                army.Id, officer?.Name ?? $"軍{army.Id}",
                BattleCore.Events.StructureEventType.Destroyed,
                target.Type.ToString(), army.CurrentHexId));
        }
    }

    /// <summary>
    /// 補給命令。最寄りの自軍 Camp へ移動して食糧を補充する。
    /// Camp がなければ最寄りの自軍城へ移動する。
    /// </summary>
    public sealed class SupplyOrder : ICommand
    {
        public int ArmyId { get; }
        public SupplyOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;

            var currentHex = context.World.Map.GetHexById(army.CurrentHexId);
            if (currentHex == null) return;

            // 最寄りの自軍 Camp
            var camp = context.World.Structures
                .Where(s => s.Type == StructureType.Camp && s.OwnerClanId == army.ClanId)
                .OrderBy(s =>
                {
                    var h = context.World.Map.GetHexById(s.HexId);
                    return h == null ? int.MaxValue : BattleCore.Map.HexDistance.Calculate(currentHex, h);
                })
                .FirstOrDefault();

            if (camp != null) { army.OrderMove(camp.HexId); return; }

            // Camp がなければ最寄りの自軍城
            var castle = context.World.Castles
                .Where(c => c.OwnerClanId == army.ClanId)
                .OrderBy(c =>
                {
                    var h = context.World.Map.GetHexById(c.HexId);
                    return h == null ? int.MaxValue : BattleCore.Map.HexDistance.Calculate(currentHex, h);
                })
                .FirstOrDefault();

            if (castle != null) army.OrderMove(castle.HexId);
        }
    }

    /// <summary>
    /// 偵察命令。移動せず視界+3・Intel成功率+50%。
    /// VisionSystem が Scouting 態勢を参照し、IntelSystem が ScoutingBonus を参照する。
    /// </summary>
    public sealed class ScoutOrder : ICommand
    {
        public int ArmyId { get; }
        public ScoutOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            army.Stance        = ArmyStance.Scouting;
            army.ScoutingBonus = true;
            army.ClearDestination();
        }
    }

    /// <summary>
    /// 強行軍命令。AP+1、士気-3、視界-1。
    /// 速度を優先するが消耗が大きい命令。
    /// </summary>
    public sealed class MarchOrder : ICommand
    {
        public int ArmyId { get; }
        public MarchOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            army.Marching = true;
            army.ActionPoints = System.Math.Min(
                army.ActionPoints + 1, Army.MaxActionPoints + 1);
            army.Morale = System.Math.Max(0, army.Morale - 3);
        }
    }

    /// <summary>
    /// 築城命令。現在Hexに Palisade を建設し、士気+5。
    /// BuildOrder(Palisade) の特化版。
    /// </summary>
    public sealed class FortifyOrder : ICommand
    {
        public int ArmyId { get; }
        public FortifyOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null || army.Soldiers == 0) return;

            bool exists = context.World.Structures.Any(s =>
                s.HexId == army.CurrentHexId &&
                s.Type == StructureType.Palisade &&
                s.OwnerClanId == army.ClanId);
            if (!exists)
            {
                int newId = context.World.Structures.Count > 0
                    ? context.World.Structures.Max(s => s.Id) + 1 : 1;
                context.World.Structures.Add(
                    new Structure(newId, StructureType.Palisade, army.CurrentHexId, army.ClanId));
                var officer = army.OfficerId.HasValue
                    ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
                context.EventQueue.Enqueue(new BattleCore.Events.StructureEvent(
                    army.Id, officer?.Name ?? $"軍{army.Id}",
                    BattleCore.Events.StructureEventType.Built, "Palisade", army.CurrentHexId));
            }
            army.Morale = System.Math.Min(100, army.Morale + 5);
            army.Stance = ArmyStance.Defend;
            army.ClearDestination();
        }
    }

    /// <summary>
    /// 塩壕命令。被ダメ追加-10%、移動不可2Tick。
    /// EntrenchModifier が Entrenched 態勢を参照する。
    /// </summary>
    public sealed class EntrenchOrder : ICommand
    {
        public int ArmyId { get; }
        public EntrenchOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            army.Stance       = ArmyStance.Entrenched;
            army.EntrenchTick = 2;
            army.ClearDestination();
        }
    }

    /// <summary>
    /// 兵粮焼き命令。現在Hexまたは隣接Hexの敵 Camp を破壊し、敵軍の Food-30。
    /// </summary>
    public sealed class BurnFieldOrder : ICommand
    {
        public int ArmyId { get; }
        public BurnFieldOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;

            var reachable = new System.Collections.Generic.HashSet<int> { army.CurrentHexId };
            foreach (var n in context.World.Map.GetNeighbors(army.CurrentHexId))
                reachable.Add(n.Id);

            // 敵 Camp を破壊
            var targets = context.World.Structures
                .Where(s => s.Type == StructureType.Camp &&
                            s.OwnerClanId != army.ClanId &&
                            reachable.Contains(s.HexId))
                .ToList();
            foreach (var t in targets)
            {
                context.World.Structures.Remove(t);
                var officer = army.OfficerId.HasValue
                    ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
                context.EventQueue.Enqueue(new BattleCore.Events.StructureEvent(
                    army.Id, officer?.Name ?? $"軍{army.Id}",
                    BattleCore.Events.StructureEventType.Destroyed, "Camp", t.HexId));
            }

            // 敵軍の Food-30
            foreach (var enemy in context.World.Armies
                .Where(a => a.Soldiers > 0 &&
                            a.ClanId != army.ClanId &&
                            reachable.Contains(a.CurrentHexId)))
                enemy.Food = System.Math.Max(0, enemy.Food - 30);
        }
    }

    /// <summary>
    /// 陽動命令。敵AIの目標を自軍に引き付ける。
    /// AggressiveClanStrategy が IsDecoy=true の軍を優先目標にする。
    /// </summary>
    public sealed class ScreenOrder : ICommand
    {
        public int ArmyId { get; }
        public ScreenOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            army.Stance   = ArmyStance.Screening;
            army.IsDecoy  = true;
        }
    }

    /// <summary>
    /// 籠城命令。城 Hex にいる場合のみ有効。城ボーナス重複。
    /// GarrisonModifier が Garrisoning 態勢を参照する。
    /// </summary>
    public sealed class GarrisonOrder : ICommand
    {
        public int ArmyId { get; }
        public GarrisonOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;

            bool onCastle = context.World.Castles.Any(c =>
                c.HexId == army.CurrentHexId && c.OwnerClanId == army.ClanId);
            if (!onCastle) return;

            army.Stance = ArmyStance.Garrisoning;
            army.ClearDestination();
        }
    }

    /// <summary>
    /// 段階撤退命令。毻Tick1Hex後退しながら戦闘継続。
    /// MovementSystem が PhaseRetreating 態勢を見て自軍城方向へ強制移動する。
    /// </summary>
    public sealed class PhaseRetreatOrder : ICommand
    {
        public int ArmyId { get; }
        public PhaseRetreatOrder(int armyId) => ArmyId = armyId;

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;

            army.Stance = ArmyStance.PhaseRetreating;

            var castle = context.World.Castles
                .Where(c => c.OwnerClanId == army.ClanId)
                .OrderBy(c =>
                {
                    var h = context.World.Map.GetHexById(army.CurrentHexId);
                    var ch = context.World.Map.GetHexById(c.HexId);
                    return (h == null || ch == null) ? int.MaxValue
                        : BattleCore.Map.HexDistance.Calculate(h, ch);
                })
                .FirstOrDefault();

            if (castle != null)
                army.OrderMove(castle.HexId);
        }
    }
}
