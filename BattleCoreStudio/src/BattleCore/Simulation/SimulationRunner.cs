namespace BattleCore.Simulation
{
    /// <summary>
    /// シミュレーションの自動実行を管理する。UI非依存。
    /// WinForms / Unity どちらからも利用できる。
    /// </summary>
    public sealed class SimulationRunner
    {
        private readonly SimulationEngine _engine;
        private CancellationTokenSource? _cts;

        /// <summary>ターン間の待機ミリ秒。0 = MAX速度（Sleep なし）。</summary>
        public int IntervalMs { get; set; } = 1000;

        /// <summary>自動実行中かどうか。</summary>
        public bool IsRunning { get; private set; }

        /// <summary>1ターン完了時に発火。引数は現在の統計。</summary>
        public event Action<SimulationStats>? TurnCompleted;

        /// <summary>実行完了時に発火（停止・勝利・ターン上限）。</summary>
        public event Action<SimulationStats>? RunCompleted;

        public SimulationRunner(SimulationEngine engine)
        {
            _engine = engine;
        }

        /// <summary>指定ターン数だけ実行する。maxTurns=0 で勝利まで無制限。</summary>
        public Task RunAsync(int maxTurns = 0, CancellationToken external = default)
        {
            if (IsRunning) return Task.CompletedTask;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(external);
            var token = _cts.Token;
            IsRunning = true;

            return Task.Run(async () =>
            {
                var stats = new SimulationStats();
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        _engine.Step();
                        stats.Collect(_engine.Context);

                        // MAX速度時はUI更新を10ターンに1回に間引く
                        bool isLast = stats.WinnerClanId.HasValue
                            || stats.WinReason.Length > 0
                            || (maxTurns > 0 && stats.TurnsExecuted >= maxTurns);

                        if (IntervalMs > 0 || stats.TurnsExecuted % 10 == 0 || isLast)
                            TurnCompleted?.Invoke(stats);

                        if (isLast) break;

                        if (IntervalMs > 0)
                            await Task.Delay(IntervalMs, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    IsRunning = false;
                    RunCompleted?.Invoke(stats);
                }
            }, token);
        }

        /// <summary>実行を一時停止する。RunAsync の CancellationToken をキャンセルする。</summary>
        public void Pause()
        {
            _cts?.Cancel();
            IsRunning = false;
        }

        /// <summary>Pause の別名。</summary>
        public void Stop() => Pause();

        /// <summary>
        /// N回バッチ実行。各回はシナリオをロードし直して実行する。
        /// seed指定時は seed+runIndex で再現性を保証する。
        /// </summary>
        public Task<BattleRunSummary> RunBatchAsync(
            Func<int?, SimulationEngine> engineFactory,
            int runs,
            int maxTurns,
            int? baseSeed = null,
            Action<int, SimulationStats>? onRunCompleted = null,
            CancellationToken external = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(external);
            var token = _cts.Token;
            IsRunning = true;

            return Task.Run(async () =>
            {
                var summary = new BattleRunSummary();
                try
                {
                    for (int i = 0; i < runs && !token.IsCancellationRequested; i++)
                    {
                        int? seed = baseSeed.HasValue ? baseSeed.Value + i : null;
                        var eng   = engineFactory(seed);
                        var stats = new SimulationStats();

                        for (int t = 0; t < maxTurns && !token.IsCancellationRequested; t++)
                        {
                            eng.Step();
                            stats.Collect(eng.Context);
                            if (stats.WinnerClanId.HasValue || stats.WinReason.Length > 0) break;
                        }

                        var record = BattleRunRecord.From(i + 1, seed, stats);
                        summary.Add(record);
                        onRunCompleted?.Invoke(i + 1, stats);

                        // UIスレッドに制御を返す機会を与える
                        await Task.Delay(1, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    IsRunning = false;
                }
                return summary;
            }, token);
        }
    }
}
