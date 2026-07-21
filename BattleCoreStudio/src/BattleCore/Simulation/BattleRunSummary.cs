namespace BattleCore.Simulation
{
    /// <summary>
    /// バッチ実行N回分の集計。勢力別勝率・性格別統計を提供する。
    /// </summary>
    public sealed class BattleRunSummary
    {
        private readonly List<BattleRunRecord> _records = [];

        public IReadOnlyList<BattleRunRecord> Records => _records;
        public int TotalRuns => _records.Count;

        public void Add(BattleRunRecord record) => _records.Add(record);

        /// <summary>勢力別勝利数。Key = ClanId。</summary>
        public Dictionary<int, int> WinsByClan()
            => _records
                .Where(r => r.WinnerClanId.HasValue)
                .GroupBy(r => r.WinnerClanId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

        /// <summary>性格別統計。Key = Personality名。</summary>
        public Dictionary<string, PersonalityStats> StatsByPersonality()
        {
            var result = new Dictionary<string, PersonalityStats>();
            foreach (var rec in _records)
                foreach (var os in rec.OfficerStats.Values)
                {
                    if (!result.TryGetValue(os.Personality, out var ps))
                        result[os.Personality] = ps = new PersonalityStats { Personality = os.Personality };
                    ps.TotalOfficers++;
                    ps.RefusalCount    += os.RefusalCount;
                    ps.IndependentCount += os.IndependentCount;
                    if (!os.Survived) ps.DeathCount++;
                }
            return result;
        }

        public double AvgTurns()
            => _records.Count == 0 ? 0 : _records.Average(r => r.TurnsExecuted);

        public double AvgBattles()
            => _records.Count == 0 ? 0 : _records.Average(r => r.BattleCount);

        /// <summary>CSV全行（ヘッダー含む）を返す。</summary>
        public IEnumerable<string> ToCsvLines(Func<int?, string> clanName)
        {
            yield return "Run,Seed,Winner,Turns,Battles,Supplies,Betrayals,Refusals,Independent";
            foreach (var r in _records)
                yield return r.ToCsvRow(clanName(r.WinnerClanId));
        }
    }

    public sealed class PersonalityStats
    {
        public string Personality    { get; init; } = "";
        public int    TotalOfficers  { get; set; }
        public int    RefusalCount   { get; set; }
        public int    IndependentCount { get; set; }
        public int    DeathCount     { get; set; }
    }
}
