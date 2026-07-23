using System.Collections.Generic;

namespace BattleCore.Scenario
{
    /// <summary>シナリオファイル全体のルートDTO。JSONデシリアライズ対象。</summary>
    public class ScenarioData
    {
        /// <summary>シナリオタイトル。ウィンドウタイトルに表示する。</summary>
        public string Title     { get; set; } = "";

        /// <summary>開始年。GameTime の初期値に使用する（将来実装）。</summary>
        public int    StartYear { get; set; } = 1560;

        /// <summary>マップHexのリスト。</summary>
        public List<HexData>          Map           { get; set; } = new();

        /// <summary>勢力のリスト。</summary>
        public List<ClanData>         Clans         { get; set; } = new();

        /// <summary>武将のリスト。</summary>
        public List<OfficerData>      Officers      { get; set; } = new();

        /// <summary>武将と勢力の所属関係リスト。</summary>
        public List<MembershipData>   Memberships   { get; set; } = new();

        /// <summary>軍隊のリスト。</summary>
        public List<ArmyData>         Armies        { get; set; } = new();

        /// <summary>武将間の関係値リスト。</summary>
        public List<RelationshipData> Relationships { get; set; } = new();

        /// <summary>初期同盟リスト。</summary>
        public List<AllianceData>     Alliances     { get; set; } = new();

        /// <summary>城・拠点リスト。</summary>
        public List<CastleData>       Castles       { get; set; } = new();

        /// <summary>シナリオイベントトリガーリスト。</summary>
        public List<EventTriggerData> EventTriggers { get; set; } = new();
    }

    /// <summary>HexマップのDTO。</summary>
    public class HexData
    {
        /// <summary>HexのID。</summary>
        public int    Id      { get; set; }

        /// <summary>X座標（東西）。</summary>
        public int    Q       { get; set; }

        /// <summary>Y座標（南北）。</summary>
        public int    R       { get; set; }

        /// <summary>地形種別文字列。"Plain" / "Forest" / "Mountain"。</summary>
        public string Terrain { get; set; } = "Plain";

        /// <summary>高度（0〜3）。未指定の場合は0。</summary>
        public int Height { get; set; } = 0;
    }

    /// <summary>勢力のDTO。</summary>
    public class ClanData
    {
        /// <summary>勢力ID。</summary>
        public int    Id                 { get; set; }

        /// <summary>勢力名。</summary>
        public string Name               { get; set; } = "";

        /// <summary>君主となる Officer の ID。未設定の場合は null。</summary>
        public int?   DaimyoOfficerId    { get; set; }

        /// <summary>プレイヤー操作の勢力かどうか。true の場合 AI 自動操作をスキップする。</summary>
        public bool   IsPlayerControlled { get; set; } = false;
    }

    /// <summary>武将のDTO。</summary>
    public class OfficerData
    {
        /// <summary>武将ID。</summary>
        public int Id           { get; set; }

        /// <summary>武将名。</summary>
        public string Name      { get; set; } = "";

        /// <summary>統率力。</summary>
        public int Leadership   { get; set; } = 100;

        /// <summary>戦術能力。</summary>
        public int Strategy     { get; set; } = 100;

        /// <summary>武勇。</summary>
        public int Courage      { get; set; } = 100;

        /// <summary>忠誠心。</summary>
        public int Loyalty      { get; set; } = 80;

        /// <summary>知略。</summary>
        public int Intelligence { get; set; } = 100;

        /// <summary>野心。</summary>
        public int Ambition     { get; set; } = 50;

        /// <summary>性格。未指定の場合は Loyal として扱われる。</summary>
        public string Personality { get; set; } = "";
    }

    /// <summary>武将と勢力の所属関係のDTO。</summary>
    public class MembershipData
    {
        /// <summary>所属関係ID。</summary>
        public int Id        { get; set; }

        /// <summary>武将ID。</summary>
        public int OfficerId { get; set; }

        /// <summary>勢力ID。</summary>
        public int ClanId    { get; set; }

        /// <summary>この勢力への忠誠心（0〜100）。</summary>
        public int Loyalty   { get; set; } = 80;
    }

    /// <summary>軍隊のDTO。</summary>
    public class ArmyData
    {
        /// <summary>軍ID。</summary>
        public int  Id           { get; set; }

        /// <summary>所属勢力ID。</summary>
        public int  ClanId       { get; set; }

        /// <summary>初期配置HexID。</summary>
        public int  CurrentHexId { get; set; }

        /// <summary>初期兵力。</summary>
        public int  Soldiers     { get; set; } = 1000;

        /// <summary>指揮官武将ID。未配属の場合は null。</summary>
        public int? OfficerId    { get; set; }

        /// <summary>兵種。未指定の場合は Ashigaru。</summary>
        public string UnitType   { get; set; } = "Ashigaru";
    }

    /// <summary>武将間の関係値のDTO。</summary>
    public class RelationshipData
    {
        /// <summary>関係ID。</summary>
        public int Id            { get; set; }

        /// <summary>関係の起点となる武将ID。</summary>
        public int FromOfficerId { get; set; }

        /// <summary>関係の対象となる武将ID。</summary>
        public int ToOfficerId   { get; set; }

        /// <summary>信頼度（0〜100）。</summary>
        public int Trust         { get; set; }

        /// <summary>尊敬度（0〜100）。</summary>
        public int Respect       { get; set; }

        /// <summary>反感度（0〜100）。</summary>
        public int Dislike       { get; set; }
    }

    /// <summary>初期同盟のDTO。</summary>
    public class AllianceData
    {
        /// <summary>同盟ID。</summary>
        public int Id            { get; set; }

        /// <summary>同盟に関与する勢力ID（1）。</summary>
        public int ClanId1       { get; set; }

        /// <summary>同盟に関与する勢力ID（2）。</summary>
        public int ClanId2       { get; set; }

        /// <summary>同盟の有効Tick数。</summary>
        public int DurationTicks { get; set; } = 20;
    }

    /// <summary>城・拠点のDTO。</summary>
    public class CastleData
    {
        /// <summary>城ID。</summary>
        public int    Id                   { get; set; }

        /// <summary>城の名前。</summary>
        public string Name                 { get; set; } = "";

        /// <summary>城が配置されたHexID。</summary>
        public int    HexId                { get; set; }

        /// <summary>初期占領勢力ID。0=中立。</summary>
        public int    OwnerClanId          { get; set; } = 0;

        /// <summary>毎ティック補充兵力。</summary>
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
        /// <summary>
        /// 条件: 指定武将の軍がこのOfficerIDの軍と隣接Hexにいる。
        /// 例: 「信長と光秀が隣接したら発火」
        /// </summary>
        public int?   AdjacentToOfficerId { get; set; }
        public string Message    { get; set; } = "";
    }
}
