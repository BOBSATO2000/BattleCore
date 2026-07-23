namespace BattleCore.Simulation
{
    /// <summary>
    /// バッチ実行N回分の集計。勢力別勝率・性格別統計を提供する。
    /// </summary>
    public sealed class BattleRunSummary
    {
        private readonly List<BattleRunRecord> _records = [];

        /// <summary>全実行レコードの読み取り専用リスト。</summary>
        public IReadOnlyList<BattleRunRecord> Records => _records;

        /// <summary>実行回数。</summary>
        public int TotalRuns => _records.Count;

        /// <summary>実行レコードを追加する。</summary>
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

        /// <summary>
        /// イベント別統計。Key = TriggerId。
        /// 発火率と、発火した回の勝者分布を返す。
        /// </summary>
        public Dictionary<string, EventStat> EventStats()
        {
            var result = new Dictionary<string, EventStat>();
            foreach (var rec in _records)
            {
                foreach (var id in rec.FiredEvents)
                {
                    if (!result.TryGetValue(id, out var es))
                        result[id] = es = new EventStat { TriggerId = id };
                    es.FiredCount++;
                    if (rec.WinnerClanId.HasValue)
                    {
                        es.WinsByClan.TryGetValue(rec.WinnerClanId.Value, out int w);
                        es.WinsByClan[rec.WinnerClanId.Value] = w + 1;
                    }
                }
            }
            return result;
        }

        /// <summary>指定フィールドの統計（平均/最小/最大/標準偏差）を返す。</summary>
        public FieldStats CalcStats(Func<BattleRunRecord, int> selector)
        {
            if (_records.Count == 0) return new FieldStats();
            var values = _records.Select(r => (double)selector(r)).ToList();
            double avg  = values.Average();
            double std  = Math.Sqrt(values.Average(v => (v - avg) * (v - avg)));
            return new FieldStats
            {
                Avg = avg,
                Min = (int)values.Min(),
                Max = (int)values.Max(),
                StdDev = std,
            };
        }

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
        /// <summary>性格名。</summary>
        public string Personality    { get; init; } = "";

        /// <summary>対象武将の総数。</summary>
        public int    TotalOfficers  { get; set; }

        /// <summary>命令拒否の総数。</summary>
        public int    RefusalCount   { get; set; }

        /// <summary>独断行動の総数。</summary>
        public int    IndependentCount { get; set; }

        /// <summary>戦死数。</summary>
        public int    DeathCount     { get; set; }
    }

    public sealed class FieldStats
    {
        /// <summary>平均値。</summary>
        public double Avg    { get; init; }

        /// <summary>最小値。</summary>
        public int    Min    { get; init; }

        /// <summary>最大値。</summary>
        public int    Max    { get; init; }

        /// <summary>標準偏差。</summary>
        public double StdDev { get; init; }
        public override string ToString() => $"{Avg:F1} (min:{Min} max:{Max} σ:{StdDev:F1})";
    }

    /// <summary>イベント別統計。</summary>
    public sealed class EventStat
    {
        public string TriggerId  { get; init; } = "";
        public int    FiredCount { get; set; }
        /// <summary>発火時の勝者分布。Key = ClanId。</summary>
        public Dictionary<int, int> WinsByClan { get; } = new();

        public double FiredRate(int totalRuns)
            => totalRuns > 0 ? FiredCount * 100.0 / totalRuns : 0;
    }
}
