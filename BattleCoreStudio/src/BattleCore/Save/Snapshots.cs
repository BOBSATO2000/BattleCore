using BattleCore.Simulation;
using System.Collections.Generic;

namespace BattleCore.Save
{
    /// <summary>GameTime のシリアライズ用スナップショット。</summary>
    public class GameTimeSnapshot
    {
        /// <summary>経過ステップ数。</summary>
        public int Tick { get; set; }

        /// <summary>現在の年。</summary>
        public int Year { get; set; }

        /// <summary>現在の季節（文字列）。</summary>
        public string Season { get; set; } = "Spring";

        /// <summary>現在の天気（文字列）。</summary>
        public string Weather { get; set; } = "Sunny";
    }

    /// <summary>WorldState のシリアライズ用スナップショット。</summary>
    public class WorldSnapshot
    {
        /// <summary>勢力スナップショットのリスト。</summary>
        public List<ClanSnapshot> Clans { get; set; } = new();

        /// <summary>武将スナップショットのリスト。</summary>
        public List<OfficerSnapshot> Officers { get; set; } = new();

        /// <summary>軍隊スナップショットのリスト。</summary>
        public List<ArmySnapshot> Armies { get; set; } = new();

        /// <summary>城スナップショットのリスト。</summary>
        public List<CastleSnapshot> Castles { get; set; } = new();

        /// <summary>同盟スナップショットのリスト。</summary>
        public List<AllianceSnapshot> Alliances { get; set; } = new();

        /// <summary>人間関係スナップショットのリスト。</summary>
        public List<RelationshipSnapshot> Relationships { get; set; } = new();

        /// <summary>所属関係スナップショットのリスト。</summary>
        public List<MembershipSnapshot> Memberships { get; set; } = new();

        /// <summary>Hexスナップショットのリスト。</summary>
        public List<HexSnapshot> Hexes { get; set; } = new();

        /// <summary>現在の天気（文字列）。</summary>
        public string Weather { get; set; } = "Sunny";
    }

    public class ClanSnapshot
    {
        /// <summary>勢力ID。</summary>
        public int Id { get; set; }

        /// <summary>勢力名。</summary>
        public string Name { get; set; } = "";

        /// <summary>資金。</summary>
        public int Gold { get; set; }

        /// <summary>君主の Officer ID。</summary>
        public int? DaimyoOfficerId { get; set; }

        /// <summary>プレイヤー操作かどうか。</summary>
        public bool IsPlayerControlled { get; set; }
    }

    public class OfficerSnapshot
    {
        /// <summary>武将ID。</summary>
        public int Id { get; set; }

        /// <summary>武将名。</summary>
        public string Name { get; set; } = "";

        /// <summary>忠誠心。</summary>
        public int Loyalty { get; set; }

        /// <summary>知略。</summary>
        public int Intelligence { get; set; }

        /// <summary>野心。</summary>
        public int Ambition { get; set; }

        /// <summary>統率力。</summary>
        public int Leadership { get; set; }

        /// <summary>戦術能力。</summary>
        public int Strategy { get; set; }

        /// <summary>武勇。</summary>
        public int Courage { get; set; }

        /// <summary>戦闘勝利回数。</summary>
        public int BattleWins { get; set; }

        /// <summary>性格（文字列）。</summary>
        public string Personality { get; set; } = "Loyal";
    }

    public class ArmySnapshot
    {
        /// <summary>軍隊ID。</summary>
        public int Id { get; set; }

        /// <summary>所属勢力ID。</summary>
        public int ClanId { get; set; }

        /// <summary>指挥官の Officer ID。</summary>
        public int? OfficerId { get; set; }

        /// <summary>現在位置HexID。</summary>
        public int CurrentHexId { get; set; }

        /// <summary>移動目標HexID。null の場合は待機中。</summary>
        public int? DestinationHexId { get; set; }

        /// <summary>現在兵力。</summary>
        public int Soldiers { get; set; }

        /// <summary>兵力上限。</summary>
        public int MaxSoldiers { get; set; }

        /// <summary>移動クールダウン。</summary>
        public int MoveCooldown { get; set; }

        /// <summary>行動力（AP）。</summary>
        public int ActionPoints { get; set; }
    }

    public class CastleSnapshot
    {
        /// <summary>城ID。</summary>
        public int Id { get; set; }

        /// <summary>城の名前。</summary>
        public string Name { get; set; } = "";

        /// <summary>城が配置されたHexID。</summary>
        public int HexId { get; set; }

        /// <summary>占領勢力ID。</summary>
        public int OwnerClanId { get; set; }

        /// <summary>毎ティック補充兵力。</summary>
        public int ReinforcementPerTick { get; set; }
    }

    public class AllianceSnapshot
    {
        /// <summary>同盟に関与する勢力ID（1）。</summary>
        public int ClanId1 { get; set; }

        /// <summary>同盟に関与する勢力ID（2）。</summary>
        public int ClanId2 { get; set; }

        /// <summary>同盟の残り有効Tick数。</summary>
        public int RemainingTicks { get; set; }
    }

    public class RelationshipSnapshot
    {
        /// <summary>関係ID。</summary>
        public int Id { get; set; }

        /// <summary>関係の起点となる武将ID。</summary>
        public int FromOfficerId { get; set; }

        /// <summary>関係の対象となる武将ID。</summary>
        public int ToOfficerId { get; set; }

        /// <summary>信頼度（0～100）。</summary>
        public int Trust { get; set; }

        /// <summary>尊敬度（0～100）。</summary>
        public int Respect { get; set; }

        /// <summary>反感度（0～100）。</summary>
        public int Dislike { get; set; }
    }

    public class MembershipSnapshot
    {
        /// <summary>武将ID。</summary>
        public int OfficerId { get; set; }

        /// <summary>勢力ID。</summary>
        public int ClanId { get; set; }

        /// <summary>この勢力への忠誠心（0～100）。</summary>
        public int Loyalty { get; set; }
    }

    public class HexSnapshot
    {
        /// <summary>HexID。</summary>
        public int Id { get; set; }

        /// <summary>X座標（東西）。</summary>
        public int X { get; set; }

        /// <summary>Y座標（南北）。</summary>
        public int Y { get; set; }

        /// <summary>地形種別（文字列）。</summary>
        public string Terrain { get; set; } = "Plain";
    }
}
