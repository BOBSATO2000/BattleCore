using BattleCore.Events;

namespace BattleCore.Simulation
{
    /// <summary>
    /// シミュレーション実行中の統計を収集する。UI非依存。
    /// </summary>
    public sealed class SimulationStats
    {
        /// <summary>実行済みターン数。</summary>
        public int TurnsExecuted    { get; private set; }

        /// <summary>発生した戦闘数。</summary>
        public int BattleCount      { get; private set; }

        /// <summary>発生した補給イベント数。</summary>
        public int SupplyCount      { get; private set; }

        /// <summary>発生した裏切り数。</summary>
        public int BetrayalCount    { get; private set; }

        /// <summary>発生した命令拒否数。</summary>
        public int RefusalCount     { get; private set; }

        /// <summary>発生した独断行動数。</summary>
        public int IndependentCount { get; private set; }

        /// <summary>発生した全イベント数。</summary>
        public int TotalEvents      { get; private set; }

        /// <summary>勝利した勢力ID。null の場合は未終了または引き分け。</summary>
        public int? WinnerClanId    { get; private set; }

        /// <summary>ゲーム終了理由のメッセージ。</summary>
        public string WinReason     { get; private set; } = "";

        /// <summary>発火したシナリオイベントID一覧。</summary>
        public HashSet<string> FiredEvents { get; } = new();

        /// <summary>武将別統計。Key = OfficerId。</summary>
        public Dictionary<int, OfficerRunStats> OfficerStats { get; } = new();

        /// <summary>1ターン分のイベントキューを処理して統計を更新する。</summary>
        public void Collect(SimulationContext context)
        {
            TurnsExecuted++;

            while (context.EventQueue.Count > 0)
            {
                TotalEvents++;
                switch (context.EventQueue.Dequeue())
                {
                    case ScenarioEvent se:
                        FiredEvents.Add(se.TriggerId); break;
                    case BattleLogEvent:
                        BattleCount++; break;
                    case SupplyEvent:
                        SupplyCount++; break;
                    case BetrayalEvent:
                        BetrayalCount++; break;
                    case OfficerRefusedOrderEvent rf:
                        RefusalCount++;
                        var o1 = context.World.Officers.FirstOrDefault(x => x.Id == rf.OfficerId);
                        GetOrAdd(rf.OfficerId, rf.OfficerName, o1?.Personality.ToString() ?? "").RefusalCount++;
                        break;
                    case DecisionExplanationEvent de when de.Summary == "独断行動":
                        IndependentCount++;
                        var o2 = context.World.Officers.FirstOrDefault(x => x.Id == de.OfficerId);
                        GetOrAdd(de.OfficerId, de.OfficerName, o2?.Personality.ToString() ?? "").IndependentCount++;
                        break;
                    case GameOverEvent go:
                        WinnerClanId = go.WinnerClanId;
                        WinReason    = go.Reason;
                        break;
                }
            }

            // 兵力0の武将を戦死扱いにする
            foreach (var army in context.World.Armies.Where(a => a.Soldiers == 0 && a.OfficerId.HasValue))
            {
                var o = context.World.Officers.FirstOrDefault(x => x.Id == army.OfficerId!.Value);
                if (o != null)
                    GetOrAdd(o.Id, o.Name, o.Personality.ToString()).Survived = false;
            }
        }

        private OfficerRunStats GetOrAdd(int id, string name, string personality)
        {
            if (!OfficerStats.TryGetValue(id, out var s))
                OfficerStats[id] = s = new OfficerRunStats { OfficerId = id, Name = name, Personality = personality };
            return s;
        }

        public void IncrementTurn() => TurnsExecuted++;
    }
}
