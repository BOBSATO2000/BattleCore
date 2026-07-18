using System.Collections.Generic;

namespace BattleCore.Scenario
{
    public class ScenarioData
    {
        public string Title     { get; set; } = "";
        public int    StartYear { get; set; } = 1560;

        public List<HexData>          Map           { get; set; } = new();
        public List<ClanData>         Clans         { get; set; } = new();
        public List<OfficerData>      Officers      { get; set; } = new();
        public List<MembershipData>   Memberships   { get; set; } = new();
        public List<ArmyData>         Armies        { get; set; } = new();
        public List<RelationshipData> Relationships { get; set; } = new();
        public List<AllianceData>     Alliances     { get; set; } = new();
        public List<CastleData>       Castles       { get; set; } = new();
        public List<EventTriggerData> EventTriggers { get; set; } = new();
    }

    public class HexData
    {
        public int    Id      { get; set; }
        public int    Q       { get; set; }
        public int    R       { get; set; }
        public string Terrain { get; set; } = "Plain";
    }

    public class ClanData
    {
        public int    Id   { get; set; }
        public string Name { get; set; } = "";
    }

    public class OfficerData
    {
        public int Id           { get; set; }
        public string Name      { get; set; } = "";
        public int Leadership   { get; set; } = 100;
        public int Strategy     { get; set; } = 100;
        public int Courage      { get; set; } = 100;
        public int Loyalty      { get; set; } = 80;
        public int Intelligence { get; set; } = 100;
        public int Ambition     { get; set; } = 50;
    }

    public class MembershipData
    {
        public int Id        { get; set; }
        public int OfficerId { get; set; }
        public int ClanId    { get; set; }
        public int Loyalty   { get; set; } = 80;
    }

    public class ArmyData
    {
        public int  Id           { get; set; }
        public int  ClanId       { get; set; }
        public int  CurrentHexId { get; set; }
        public int  Soldiers     { get; set; } = 1000;
        public int? OfficerId    { get; set; }
    }

    public class RelationshipData
    {
        public int Id            { get; set; }
        public int FromOfficerId { get; set; }
        public int ToOfficerId   { get; set; }
        public int Trust         { get; set; }
        public int Respect       { get; set; }
        public int Dislike       { get; set; }
    }

    /// <summary>初期同盟のDTO。</summary>
    public class AllianceData
    {
        public int Id            { get; set; }
        public int ClanId1       { get; set; }
        public int ClanId2       { get; set; }
        public int DurationTicks { get; set; } = 20;
    }

    /// <summary>城・拠点のDTO。</summary>
    public class CastleData
    {
        public int    Id                   { get; set; }
        public string Name                 { get; set; } = "";
        public int    HexId                { get; set; }
        public int    OwnerClanId          { get; set; } = 0;
        public int    ReinforcementPerTick { get; set; } = 50;
    }

    /// <summary>
    /// イベントトリガーのDTO。条件が全て満たされたとき一度だけ発火する。
    ///
    /// 条件（全てオプション・AND条件）：
    ///   MinTick    : 指定Tick以降
    ///   OfficerId  : 対象武将ID（下記条件と併用）
    ///   MinDislike : 対象武将のDislike合計がこの値以上
    ///   MaxLoyalty : 対象武将のLoyaltyがこの値以下
    ///
    /// アクション：
    ///   Message    : イベントログに表示するメッセージ
    /// </summary>
    public class EventTriggerData
    {
        public string Id         { get; set; } = "";
        public int    MinTick    { get; set; } = 0;
        public int?   OfficerId  { get; set; }
        public int?   MinDislike { get; set; }
        public int?   MaxLoyalty { get; set; }
        public string Message    { get; set; } = "";
    }
}
