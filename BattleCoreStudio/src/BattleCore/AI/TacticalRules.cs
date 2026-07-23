using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Map;

namespace BattleCore.AI
{
    /// <summary>食糧不足 → 補給命令。</summary>
    public sealed class SupplyRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => s.Army.Food <= p.SupplyFoodThreshold;

        public ICommand? CreateOrder(TacticalSituation s)
            => new SupplyOrder(s.Army.Id);
    }

    /// <summary>士気低下 → 防御態勢。</summary>
    public sealed class LowMoraleDefendRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => s.Army.Morale <= p.DefendMoraleThreshold && s.EnemyNearby;

        public ICommand? CreateOrder(TacticalSituation s)
            => new DefendOrder(s.Army.Id);
    }

    /// <summary>兵力不足 → 撤退命令。</summary>
    public sealed class RetreatRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => s.Army.Soldiers <= p.RetreatSoldierThreshold && s.HasFriendlyCastle;

        public ICommand? CreateOrder(TacticalSituation s)
            => new RetreatOrder(s.Army.Id);
    }

    /// <summary>
    /// 敵が近い + Forest/Mountain + Palisadeなし → 塹壕命令。
    /// 地形を活かした防御。
    /// </summary>
    public sealed class EntrenchRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => s.NearestEnemyDist <= p.EntrenchEnemyDist
            && (s.CurrentTerrain == TerrainType.Forest || s.CurrentTerrain == TerrainType.Mountain)
            && !s.HasPalisade
            && s.Army.Stance != ArmyStance.Entrenched;

        public ICommand? CreateOrder(TacticalSituation s)
            => new EntrenchOrder(s.Army.Id);
    }

    /// <summary>
    /// 敵が近い + 城から遠い + Palisadeなし → 築城命令。
    /// 前線に防衛拠点を作る。
    /// </summary>
    public sealed class FortifyRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => s.NearestEnemyDist <= p.FortifyEnemyDist
            && s.NearestCastleDist >= p.FortifyMinCastleDist
            && !s.HasPalisade;

        public ICommand? CreateOrder(TacticalSituation s)
            => new FortifyOrder(s.Army.Id);
    }

    /// <summary>
    /// 自軍城が包囲中 + 城Hexにいる → 籠城命令。
    /// </summary>
    public sealed class GarrisonRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => s.UnderSiege && s.IsOnFriendlyCastle;

        public ICommand? CreateOrder(TacticalSituation s)
            => new GarrisonOrder(s.Army.Id);
    }

    /// <summary>
    /// 敵が視界外 + 敵城方向が不明 → 偵察命令。
    /// 情報収集を優先する。
    /// </summary>
    public sealed class ScoutRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => !s.EnemyNearby && s.Army.Stance == ArmyStance.Normal;

        public ICommand? CreateOrder(TacticalSituation s)
            => new ScoutOrder(s.Army.Id);
    }

    /// <summary>
    /// Campなし + 食糧が半分以下 → Camp建設命令。
    /// 補給拠点を確保する。
    /// </summary>
    public sealed class BuildCampRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => !s.HasCamp
            && s.Army.Food <= Army.MaxFood / 2
            && !s.EnemyNearby;

        public ICommand? CreateOrder(TacticalSituation s)
            => new BuildOrder(s.Army.Id, StructureType.Camp);
    }

    /// <summary>
    /// 敵が近い + Forest/Mountain + Palisadeなし + Ambush未設定 → 奇襲命令。
    /// 地形を活かして待ち伏せする。
    /// </summary>
    public sealed class AmbushRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => s.NearestEnemyDist <= p.EntrenchEnemyDist
            && (s.CurrentTerrain == TerrainType.Forest || s.CurrentTerrain == TerrainType.Mountain)
            && s.Army.Stance != ArmyStance.Ambush
            && s.Army.Stance != ArmyStance.Entrenched
            && !s.HasPalisade;

        public ICommand? CreateOrder(TacticalSituation s)
            => new AmbushOrder(s.Army.Id);
    }

    /// <summary>
    /// 敵Campが隣接 + 兵力十分 → 兵粮焼き命令。
    /// 敵の補給拠点を破壊して食糧を奪う。
    /// </summary>
    public sealed class BurnFieldRule : ITacticalRule
    {
        public bool Matches(TacticalSituation s, TacticalParams p)
            => s.HasNearbyEnemyCamp && s.Army.Soldiers >= 500;

        public ICommand? CreateOrder(TacticalSituation s)
            => new BurnFieldOrder(s.Army.Id);
    }
}
