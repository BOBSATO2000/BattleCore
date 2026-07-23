using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.AI
{
    /// <summary>
    /// 1軍の戦況スナップショット。
    /// ITacticalRule が評価に使用する。WorldState を直接渡さず
    /// 必要な値だけ抽出することで、ルールの実装を単純に保つ。
    /// </summary>
    public sealed class TacticalSituation
    {
        public Army   Army              { get; }
        public int    NearestEnemyDist  { get; }
        public int    NearestCastleDist { get; }
        public bool   HasFriendlyCastle  { get; }
        public bool   IsOnFriendlyCastle { get; }
        public bool   EnemyNearby        { get; }   // 視界内に敵がいる
        public bool   UnderSiege         { get; }   // 自軍城が包囲中
        public bool   HasCamp            { get; }   // 現在Hexに自軍Camp
        public bool   HasPalisade        { get; }   // 現在Hexに自軍Palisade
        public bool   HasNearbyEnemyCamp { get; }   // 隣接Hexに敵Camp
        public int    VisibleEnemyCount  { get; }
        public TerrainType CurrentTerrain { get; }

        public TacticalSituation(Army army, Clan clan, WorldState world)
        {
            Army = army;

            var currentHex = world.Map.GetHexById(army.CurrentHexId);
            CurrentTerrain = currentHex?.Terrain ?? TerrainType.Plain;

            var visibleHexes = world.Visions.TryGetValue(army.Id, out var vision)
                ? vision.VisibleHexes : new HashSet<int>();

            var visibleEnemies = world.Armies
                .Where(a => a.ClanId != clan.Id && a.Soldiers > 0
                         && !world.AreAllied(clan.Id, a.ClanId)
                         && visibleHexes.Contains(a.CurrentHexId))
                .ToList();

            VisibleEnemyCount = visibleEnemies.Count;
            EnemyNearby       = visibleEnemies.Any();

            NearestEnemyDist = visibleEnemies.Any() && currentHex != null
                ? visibleEnemies.Min(e =>
                {
                    var h = world.Map.GetHexById(e.CurrentHexId);
                    return h == null ? int.MaxValue : HexDistance.Calculate(currentHex, h);
                })
                : int.MaxValue;

            var friendlyCastles = world.Castles.Where(c => c.OwnerClanId == clan.Id).ToList();
            HasFriendlyCastle   = friendlyCastles.Any();
            IsOnFriendlyCastle  = friendlyCastles.Any(c => c.HexId == army.CurrentHexId);

            NearestCastleDist = friendlyCastles.Any() && currentHex != null
                ? friendlyCastles.Min(c =>
                {
                    var h = world.Map.GetHexById(c.HexId);
                    return h == null ? int.MaxValue : HexDistance.Calculate(currentHex, h);
                })
                : int.MaxValue;

            UnderSiege = world.Castles.Any(c =>
                c.OwnerClanId == clan.Id && c.SiegeTick > 0);

            HasCamp = world.Structures.Any(s =>
                s.Type == StructureType.Camp &&
                s.HexId == army.CurrentHexId &&
                s.OwnerClanId == clan.Id);

            HasPalisade = world.Structures.Any(s =>
                s.Type == StructureType.Palisade &&
                s.HexId == army.CurrentHexId &&
                s.OwnerClanId == clan.Id);

            // 現在Hexまたは隣接Hexに敵Campがあるか
            var reachable = new HashSet<int> { army.CurrentHexId };
            foreach (var n in world.Map.GetNeighbors(army.CurrentHexId))
                reachable.Add(n.Id);

            HasNearbyEnemyCamp = world.Structures.Any(s =>
                s.Type == StructureType.Camp &&
                s.OwnerClanId != clan.Id &&
                !world.AreAllied(clan.Id, s.OwnerClanId) &&
                reachable.Contains(s.HexId));
        }
    }
}
