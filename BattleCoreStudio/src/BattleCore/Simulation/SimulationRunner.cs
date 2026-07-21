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

                        TurnCompleted?.Invoke(stats);

                        // 勝利判定
                        if (stats.WinnerClanId.HasValue || stats.WinReason.Length > 0)
                            break;

                        // ターン上限
                        if (maxTurns > 0 && stats.TurnsExecuted >= maxTurns)
                            break;

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

        public void Pause()
        {
            _cts?.Cancel();
            IsRunning = false;
        }

        public void Stop() => Pause();
    }
}
