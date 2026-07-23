using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.World;
using System.Linq;

namespace BattleCore.AI
{
    /// <summary>
    /// 兵力・食糧が十分 + 敵城が視界内 → CaptureCastle（4ターン）。
    /// 最も近い敵城を目標にする。
    /// </summary>
    public sealed class CaptureCastleRule : IStrategyRule
    {
        public bool Matches(Army army, Clan clan, WorldState world)
        {
            if (army.Soldiers < 500 || army.Food < 40) return false;
            var visibleHexes = world.Visions.TryGetValue(army.Id, out var v)
                ? v.VisibleHexes : new System.Collections.Generic.HashSet<int>();
            return world.Castles.Any(c =>
                c.OwnerClanId != clan.Id &&
                !world.AreAllied(clan.Id, c.OwnerClanId) &&
                visibleHexes.Contains(c.HexId));
        }

        public CampaignPlan CreatePlan(Army army, Clan clan, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId)!;
            var visibleHexes = world.Visions.TryGetValue(army.Id, out var v)
                ? v.VisibleHexes : new System.Collections.Generic.HashSet<int>();
            var target = world.Castles
                .Where(c => c.OwnerClanId != clan.Id &&
                            !world.AreAllied(clan.Id, c.OwnerClanId) &&
                            visibleHexes.Contains(c.HexId))
                .OrderBy(c => HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!))
                .First();
            return new CampaignPlan(CampaignGoal.CaptureCastle, target.HexId, 4);
        }
    }

    /// <summary>
    /// 兵力が少ない OR 食糧が少ない → Withdraw（3ターン）。
    /// 最寄りの自軍城へ撤退して回復する。
    /// </summary>
    public sealed class WithdrawRule : IStrategyRule
    {
        public bool Matches(Army army, Clan clan, WorldState world)
            => (army.Soldiers < 400 || army.Food < 25) &&
               world.Castles.Any(c => c.OwnerClanId == clan.Id);

        public CampaignPlan CreatePlan(Army army, Clan clan, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId)!;
            var castle = world.Castles
                .Where(c => c.OwnerClanId == clan.Id)
                .OrderBy(c => HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!))
                .First();
            return new CampaignPlan(CampaignGoal.Withdraw, castle.HexId, 3);
        }
    }

    /// <summary>
    /// 敵Campが視界内 → DisruptSupply（3ターン）。
    /// 敵の補給拠点を破壊しに向かう。
    /// </summary>
    public sealed class DisruptSupplyRule : IStrategyRule
    {
        public bool Matches(Army army, Clan clan, WorldState world)
        {
            if (army.Soldiers < 600) return false;
            var visibleHexes = world.Visions.TryGetValue(army.Id, out var v)
                ? v.VisibleHexes : new System.Collections.Generic.HashSet<int>();
            return world.Structures.Any(s =>
                s.Type == StructureType.Camp &&
                s.OwnerClanId != clan.Id &&
                !world.AreAllied(clan.Id, s.OwnerClanId) &&
                visibleHexes.Contains(s.HexId));
        }

        public CampaignPlan CreatePlan(Army army, Clan clan, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId)!;
            var visibleHexes = world.Visions.TryGetValue(army.Id, out var v)
                ? v.VisibleHexes : new System.Collections.Generic.HashSet<int>();
            var target = world.Structures
                .Where(s => s.Type == StructureType.Camp &&
                            s.OwnerClanId != clan.Id &&
                            !world.AreAllied(clan.Id, s.OwnerClanId) &&
                            visibleHexes.Contains(s.HexId))
                .OrderBy(s => HexDistance.Calculate(currentHex, world.Map.GetHexById(s.HexId)!))
                .First();
            return new CampaignPlan(CampaignGoal.DisruptSupply, target.HexId, 3);
        }
    }

    /// <summary>
    /// 自軍城が包囲中 + 城から離れている → Consolidate（3ターン）。
    /// 包囲されている城へ集結して防衛する。
    /// </summary>
    public sealed class ConsolidateRule : IStrategyRule
    {
        public bool Matches(Army army, Clan clan, WorldState world)
        {
            var siegedCastle = world.Castles.FirstOrDefault(c =>
                c.OwnerClanId == clan.Id && c.SiegeTick > 0);
            if (siegedCastle == null) return false;
            var currentHex = world.Map.GetHexById(army.CurrentHexId);
            var castleHex  = world.Map.GetHexById(siegedCastle.HexId);
            return currentHex != null && castleHex != null &&
                   HexDistance.Calculate(currentHex, castleHex) > 1;
        }

        public CampaignPlan CreatePlan(Army army, Clan clan, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId)!;
            var castle = world.Castles
                .Where(c => c.OwnerClanId == clan.Id && c.SiegeTick > 0)
                .OrderBy(c => HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!))
                .First();
            return new CampaignPlan(CampaignGoal.Consolidate, castle.HexId, 3);
        }
    }

    /// <summary>
    /// 視界内に未確保の Well または Shrine がある → SecureStrategicPoint（2ターン）。
    /// 戦略資源を確保しに向かう。
    /// </summary>
    public sealed class SecureStrategicPointRule : IStrategyRule
    {
        public bool Matches(Army army, Clan clan, WorldState world)
        {
            if (army.Soldiers < 400) return false;
            var visibleHexes = world.Visions.TryGetValue(army.Id, out var v)
                ? v.VisibleHexes : new System.Collections.Generic.HashSet<int>();
            return world.Structures.Any(s =>
                (s.Type == StructureType.Well || s.Type == StructureType.Shrine) &&
                s.OwnerClanId != clan.Id &&
                visibleHexes.Contains(s.HexId));
        }

        public CampaignPlan CreatePlan(Army army, Clan clan, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId)!;
            var visibleHexes = world.Visions.TryGetValue(army.Id, out var v)
                ? v.VisibleHexes : new System.Collections.Generic.HashSet<int>();
            var target = world.Structures
                .Where(s =>
                    (s.Type == StructureType.Well || s.Type == StructureType.Shrine) &&
                    s.OwnerClanId != clan.Id &&
                    visibleHexes.Contains(s.HexId))
                .OrderBy(s => HexDistance.Calculate(currentHex, world.Map.GetHexById(s.HexId)!))
                .First();
            return new CampaignPlan(CampaignGoal.SecureStrategicPoint, target.HexId, 2);
        }
    }

    /// <summary>
    /// 敵が視界外 → Reconnaissance（2ターン）。
    /// 敵城方向へ前進して情報を収集する。
    /// </summary>
    public sealed class ReconnaissanceRule : IStrategyRule
    {
        public bool Matches(Army army, Clan clan, WorldState world)
        {
            var visibleHexes = world.Visions.TryGetValue(army.Id, out var v)
                ? v.VisibleHexes : new System.Collections.Generic.HashSet<int>();
            bool noVisibleEnemy = !world.Armies.Any(a =>
                a.ClanId != clan.Id && a.Soldiers > 0 &&
                !world.AreAllied(clan.Id, a.ClanId) &&
                visibleHexes.Contains(a.CurrentHexId));
            return noVisibleEnemy &&
                   world.Castles.Any(c => c.OwnerClanId != clan.Id &&
                                         !world.AreAllied(clan.Id, c.OwnerClanId));
        }

        public CampaignPlan CreatePlan(Army army, Clan clan, WorldState world)
        {
            var currentHex = world.Map.GetHexById(army.CurrentHexId)!;
            var target = world.Castles
                .Where(c => c.OwnerClanId != clan.Id && !world.AreAllied(clan.Id, c.OwnerClanId))
                .OrderBy(c => HexDistance.Calculate(currentHex, world.Map.GetHexById(c.HexId)!))
                .First();
            return new CampaignPlan(CampaignGoal.Reconnaissance, target.HexId, 2);
        }
    }
}
