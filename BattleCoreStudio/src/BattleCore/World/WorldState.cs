using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Vision;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.World
{
    /// <summary>
    /// ゲーム世界全体の状態を保持するクラス。
    /// SimulationEngine が保持し、全 ISimulationSystem から参照される。
    /// 「世界のスナップショット」として機能し、直接変更は各Systemが担当する。
    /// </summary>
    public class WorldState
    {
        /// <summary>世界に存在する全武将。</summary>
        public List<Officer> Officers { get; } = new();

        /// <summary>世界に存在する全勢力。</summary>
        public List<Clan> Clans { get; } = new();

        /// <summary>武将と勢力の所属関係。Officer と Clan を直接結ばず中間クラスで管理。</summary>
        public List<Membership> Memberships { get; } = new();

        /// <summary>武将間の人間関係。人間ドラマシミュレーションの基盤。</summary>
        public List<Relationship> Relationships { get; } = new();

        /// <summary>世界に存在する全軍隊。BattleSystem / MovementSystem が参照する。</summary>
        public List<Army> Armies { get; } = new();

        /// <summary>勢力間の同盟リスト。DiplomacySystem が管理する。</summary>
        public List<Alliance> Alliances { get; } = new();

        /// <summary>ヘックスマップ。地理ルールを管理する。</summary>
        public GameMap Map { get; } = new();

        /// <summary>
        /// 各軍の索敵情報。キーは ArmyId。
        /// VisionSystem が毎Step更新し、AIが「見えている敵」だけを認識するために使用する。
        /// </summary>
        public Dictionary<int, VisionData> Visions { get; } = new();

        /// <summary>IDで軍を検索する。CommandExecutionSystem から使用。</summary>
        public Army? GetArmyById(int id)
            => Armies.FirstOrDefault(a => a.Id == id);

        /// <summary>2勢力が同盟中かどうかを返す。</summary>
        public bool AreAllied(int clanId1, int clanId2)
            => Alliances.Any(a =>
                (a.ClanId1 == clanId1 && a.ClanId2 == clanId2) ||
                (a.ClanId1 == clanId2 && a.ClanId2 == clanId1));

        /// <summary>
        /// 2武将間の Relationship を取得する。存在しない場合は新規作成して追加する。
        /// RelationshipSystem から使用。
        /// </summary>
        public Relationship GetOrCreateRelationship(int fromId, int toId)
        {
            var rel = Relationships.FirstOrDefault(
                r => r.FromOfficerId == fromId && r.ToOfficerId == toId);

            if (rel != null) return rel;

            rel = new Relationship(
                Relationships.Count + 1,
                fromId,
                toId);

            Relationships.Add(rel);
            return rel;
        }
    }
}
