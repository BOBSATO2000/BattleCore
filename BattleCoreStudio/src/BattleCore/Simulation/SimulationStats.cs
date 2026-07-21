using BattleCore.Events;

namespace BattleCore.Simulation
{
    /// <summary>
    /// シミュレーション実行中の統計を収集する。UI非依存。
    /// </summary>
    public sealed class SimulationStats
    {
        public int TurnsExecuted    { get; private set; }
        public int BattleCount      { get; private set; }
        public int SupplyCount      { get; private set; }
        public int BetrayalCount    { get; private set; }
        public int RefusalCount     { get; private set; }
        public int IndependentCount { get; private set; }
        public int TotalEvents      { get; private set; }
        public int? WinnerClanId    { get; private set; }
        public string WinReason     { get; private set; } = "";

        /// <summary>1ターン分のイベントキューを処理して統計を更新する。</summary>
        public void Collect(SimulationContext context)
        {
            TurnsExecuted++;

            // EventQueueを消費せずに走査（Peekできないので一時リストに退避）
            var events = new List<IGameEvent>();
            while (context.EventQueue.Count > 0)
                events.Add(context.EventQueue.Dequeue());

            foreach (var ev in events)
            {
                TotalEvents++;
                switch (ev)
                {
                    case BattleLogEvent:              BattleCount++;      break;
                    case SupplyEvent:                 SupplyCount++;      break;
                    case BetrayalEvent:               BetrayalCount++;    break;
                    case OfficerRefusedOrderEvent:    RefusalCount++;     break;
                    case DecisionExplanationEvent de
                        when de.Summary == "独断行動": IndependentCount++; break;
                    case GameOverEvent go:
                        WinnerClanId = go.WinnerClanId;
                        WinReason    = go.Reason;
                        break;
                }
            }

            // 消費したイベントを戻す（UIが後で読めるように）
            foreach (var ev in events)
                context.EventQueue.Enqueue(ev);
        }

        public void IncrementTurn() => TurnsExecuted++;
    }
}
