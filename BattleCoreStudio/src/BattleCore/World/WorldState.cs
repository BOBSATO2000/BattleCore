using BattleCore.AI;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Simulation;
using BattleCore.Vision;

namespace BattleCore.World
{
    /// <summary>
    /// ゲーム世界全体の状態を保持するクラス。
    /// SimulationEngine が保持し、全 ISimulationSystem から参照される。
    /// 「世界のスナップショット」として機能し、直接変更は各Systemが担当する。
    /// </summary>
    public class WorldState
    {
        public List<Officer>      Officers    { get; } = new();
        public List<Clan>         Clans       { get; } = new();
        public List<Membership>   Memberships { get; } = new();
        public List<Relationship> Relationships { get; } = new();
        public List<Army>         Armies      { get; } = new();
        public List<Alliance>     Alliances   { get; } = new();
        public List<Ceasefire>    Ceasefires  { get; } = new();
        public GameMap            Map         { get; } = new();
        public List<Castle>       Castles     { get; } = new();
        public List<Structure>    Structures  { get; } = new();
        public Weather            Weather     { get; set; } = Weather.Sunny;

        /// <summary>
        /// 各軍の索敵情報。キーは ArmyId。
        /// VisionSystem が毎Step更新する。「今まさに見えている」情報。
        /// </summary>
        public Dictionary<int, VisionData> Visions { get; } = new();

        /// <summary>
        /// 諜報情報。キーは (ownerClanId, enemyArmyId)。
        /// IntelSystem が諜報成功時に更新する。「情報として知っている」情報。
        /// AIはVisionで見えない場合にこちらを参照する。
        /// </summary>
        public Dictionary<(int ownerClanId, int enemyArmyId), IntelData> Intel { get; } = new();

        /// <summary>
        /// 各軍の作戦計画。キーは ArmyId。
        /// StrategyEvaluator が毎ターン更新する。
        /// </summary>
        public Dictionary<int, CampaignPlan> CampaignPlans { get; } = new();

        public Army? GetArmyById(int id)
            => Armies.FirstOrDefault(a => a.Id == id);

        public bool AreAllied(int clanId1, int clanId2)
            => Alliances.Any(a =>
                (a.ClanId1 == clanId1 && a.ClanId2 == clanId2) ||
                (a.ClanId1 == clanId2 && a.ClanId2 == clanId1));

        public bool IsInCeasefire(int clanId1, int clanId2)
            => Ceasefires.Any(c => c.Involves(clanId1) && c.Involves(clanId2));

        public Relationship GetOrCreateRelationship(int fromId, int toId)
        {
            var rel = Relationships.FirstOrDefault(
                r => r.FromOfficerId == fromId && r.ToOfficerId == toId);
            if (rel != null) return rel;
            var newId = Relationships.Count > 0 ? Relationships.Max(r => r.Id) + 1 : 1;
            rel = new Relationship(newId, fromId, toId);
            Relationships.Add(rel);
            return rel;
        }
    }
}
