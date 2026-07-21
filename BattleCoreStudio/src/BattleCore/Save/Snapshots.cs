using BattleCore.Simulation;
using System.Collections.Generic;

namespace BattleCore.Save
{
    /// <summary>GameTime のシリアライズ用スナップショット。</summary>
    public class GameTimeSnapshot
    {
        public int Tick { get; set; }
        public int Year { get; set; }
        public string Season { get; set; } = "Spring";
        public string Weather { get; set; } = "Sunny";
    }

    /// <summary>WorldState のシリアライズ用スナップショット。</summary>
    public class WorldSnapshot
    {
        public List<ClanSnapshot> Clans { get; set; } = new();
        public List<OfficerSnapshot> Officers { get; set; } = new();
        public List<ArmySnapshot> Armies { get; set; } = new();
        public List<CastleSnapshot> Castles { get; set; } = new();
        public List<AllianceSnapshot> Alliances { get; set; } = new();
        public List<RelationshipSnapshot> Relationships { get; set; } = new();
        public List<MembershipSnapshot> Memberships { get; set; } = new();
        public List<HexSnapshot> Hexes { get; set; } = new();
        public string Weather { get; set; } = "Sunny";
    }

    public class ClanSnapshot
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Gold { get; set; }
        public int? DaimyoOfficerId { get; set; }
        public bool IsPlayerControlled { get; set; }
    }

    public class OfficerSnapshot
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Loyalty { get; set; }
        public int Intelligence { get; set; }
        public int Ambition { get; set; }
        public int Leadership { get; set; }
        public int Strategy { get; set; }
        public int Courage { get; set; }
        public int BattleWins { get; set; }
        public string Personality { get; set; } = "Loyal";
    }

    public class ArmySnapshot
    {
        public int Id { get; set; }
        public int ClanId { get; set; }
        public int? OfficerId { get; set; }
        public int CurrentHexId { get; set; }
        public int? DestinationHexId { get; set; }
        public int Soldiers { get; set; }
        public int MoveCooldown { get; set; }
        public int ActionPoints { get; set; }
    }

    public class CastleSnapshot
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int HexId { get; set; }
        public int OwnerClanId { get; set; }
        public int ReinforcementPerTick { get; set; }
    }

    public class AllianceSnapshot
    {
        public int ClanId1 { get; set; }
        public int ClanId2 { get; set; }
    }

    public class RelationshipSnapshot
    {
        public int Id { get; set; }
        public int FromOfficerId { get; set; }
        public int ToOfficerId { get; set; }
        public int Trust { get; set; }
        public int Respect { get; set; }
        public int Dislike { get; set; }
    }

    public class MembershipSnapshot
    {
        public int OfficerId { get; set; }
        public int ClanId { get; set; }
    }

    public class HexSnapshot
    {
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Terrain { get; set; } = "Plain";
    }
}
