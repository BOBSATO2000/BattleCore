using BattleCore.Map;
using BattleCore.Simulation;

namespace BattleCore.Events
{
    /// <summary>
    /// 戦闘結果の詳細ログイベント。
    /// BattleResolver が生成し、BattleSystem が EventQueue に積む。
    /// </summary>
    public class BattleLogEvent : IGameEvent
    {
        public string WinnerName    { get; }
        public string LoserName     { get; }
        public int    WinnerLosses  { get; }
        public int    LoserLosses   { get; }
        public TerrainType Terrain  { get; }
        public Weather Weather      { get; }
        public bool   TerrainBonus  { get; }
        public bool   RainPenalty   { get; }
        public string? GrowthDetail { get; }  // 例: "謙信 統率+1(156)"

        public BattleLogEvent(
            string winnerName, string loserName,
            int winnerLosses, int loserLosses,
            TerrainType terrain, Weather weather,
            bool terrainBonus, bool rainPenalty,
            string? growthDetail)
        {
            WinnerName   = winnerName;
            LoserName    = loserName;
            WinnerLosses = winnerLosses;
            LoserLosses  = loserLosses;
            Terrain      = terrain;
            Weather      = weather;
            TerrainBonus = terrainBonus;
            RainPenalty  = rainPenalty;
            GrowthDetail = growthDetail;
        }
    }
}
