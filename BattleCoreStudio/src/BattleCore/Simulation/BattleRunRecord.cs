namespace BattleCore.Simulation
{
    /// <summary>
    /// バッチ実行1回分の結果。BattleRunSummary が集計に使用する。
    /// </summary>
    public sealed class BattleRunRecord
    {
        /// <summary>実行番号（1始まり）。</summary>
        public int  RunIndex      { get; init; }

        /// <summary>乱数シード。null の場合はランダム。</summary>
        public int? Seed          { get; init; }

        /// <summary>勝利した勢力ID。null の場合は引き分けまたは未終了。</summary>
        public int? WinnerClanId  { get; init; }

        /// <summary>実行ターン数。</summary>
        public int  TurnsExecuted { get; init; }

        /// <summary>発生戦闘数。</summary>
        public int  BattleCount   { get; init; }

        /// <summary>発生補給イベント数。</summary>
        public int  SupplyCount   { get; init; }

        /// <summary>発生袺切り数。</summary>
        public int  BetrayalCount { get; init; }

        /// <summary>発生命令拒否数。</summary>
        public int  RefusalCount  { get; init; }

        /// <summary>発生独断行動数。</summary>
        public int  IndependentCount { get; init; }

        /// <summary>発生全イベント数。</summary>
        public int  TotalEvents   { get; init; }

        /// <summary>発火したシナリオイベントID一覧。</summary>
        public IReadOnlySet<string> FiredEvents { get; init; } = new HashSet<string>();

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
                FiredEvents    = stats.FiredEvents,
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
        /// <summary>武将ID。</summary>
        public int OfficerId   { get; init; }

        /// <summary>武将名。</summary>
        public string Name     { get; init; } = "";

        /// <summary>性格名。</summary>
        public string Personality { get; init; } = "";

        /// <summary>命令拒否回数。</summary>
        public int RefusalCount   { get; set; }

        /// <summary>独断行動回数。</summary>
        public int IndependentCount { get; set; }

        /// <summary>生存したかどうか。false の場合は戦死。</summary>
        public bool Survived      { get; set; } = true;
    }
}
