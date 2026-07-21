namespace BattleCore.Simulation
{
    /// <summary>
    /// バッチ実行1回分の結果。BattleRunSummary が集計に使用する。
    /// </summary>
    public sealed class BattleRunRecord
    {
        public int  RunIndex      { get; init; }
        public int? Seed          { get; init; }
        public int? WinnerClanId  { get; init; }
        public int  TurnsExecuted { get; init; }
        public int  BattleCount   { get; init; }
        public int  SupplyCount   { get; init; }
        public int  BetrayalCount { get; init; }
        public int  RefusalCount  { get; init; }
        public int  IndependentCount { get; init; }
        public int  TotalEvents   { get; init; }

        /// <summary>武将別統計。Key = OfficerId。</summary>
        public IReadOnlyDictionary<int, OfficerRunStats> OfficerStats { get; init; }
            = new Dictionary<int, OfficerRunStats>();

        public static BattleRunRecord From(int runIndex, int? seed, SimulationStats stats)
            => new()
            {
                RunIndex       = runIndex,
                Seed           = seed,
                WinnerClanId   = stats.WinnerClanId,
                TurnsExecuted  = stats.TurnsExecuted,
                BattleCount    = stats.BattleCount,
                SupplyCount    = stats.SupplyCount,
                BetrayalCount  = stats.BetrayalCount,
                RefusalCount   = stats.RefusalCount,
                IndependentCount = stats.IndependentCount,
                TotalEvents    = stats.TotalEvents,
                OfficerStats   = stats.OfficerStats,
            };

        /// <summary>CSV1行分を返す。</summary>
        public string ToCsvRow(string winnerName)
            => $"{RunIndex},{Seed?.ToString() ?? "rand"},{winnerName},{TurnsExecuted}," +
               $"{BattleCount},{SupplyCount},{BetrayalCount},{RefusalCount},{IndependentCount}";
    }

    /// <summary>武将1人分の実行統計。</summary>
    public sealed class OfficerRunStats
    {
        public int OfficerId   { get; init; }
        public string Name     { get; init; } = "";
        public string Personality { get; init; } = "";
        public int RefusalCount   { get; set; }
        public int IndependentCount { get; set; }
        public bool Survived      { get; set; } = true;
    }
}
