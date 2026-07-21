namespace BattleCore.Simulation
{
    /// <summary>
    /// ゲーム内時間を管理するクラス。
    /// Step ごとに Advance() が呼ばれ、季節→年が進む。
    /// </summary>
    public class GameTime
    {
        private readonly Random _rng;

        /// <summary>経過ステップ数。テスト・デバッグ用途。</summary>
        public int Tick { get; private set; }

        /// <summary>現在の年。初期値は1560年（戦国時代）。</summary>
        public int Year { get; private set; }

        /// <summary>現在の季節。</summary>
        public Season Season { get; private set; }

        /// <summary>現在の天気。毎Tick確率で変化する。</summary>
        public Weather Weather { get; private set; } = Weather.Sunny;

        public GameTime(int? seed = null)
        {
            Year   = 1560;
            Season = Season.Spring;
            _rng   = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// セーブデータから状態を復元する。SaveSystem から呼ぶ。
        /// </summary>
        public void RestoreFrom(int tick, int year, Season season, Weather weather)
        {
            Tick    = tick;
            Year    = year;
            Season  = season;
            Weather = weather;
        }

        /// <summary>
        /// 時間を1ステップ進める。季節・天気を更新する。
        /// 天気遷移確率: Sunny→Rain 25%、Rain→Fog 20%、Fog→Sunny 50%、それ以外は維持。
        /// </summary>
        public void Advance()
        {
            Tick++;

            switch (Season)
            {
                case Season.Spring: Season = Season.Summer; break;
                case Season.Summer: Season = Season.Autumn; break;
                case Season.Autumn: Season = Season.Winter; break;
                case Season.Winter:
                    Season = Season.Spring;
                    Year++;
                    break;
            }

            Weather = Weather switch
            {
                Weather.Sunny => _rng.NextDouble() < 0.25 ? Weather.Rain  : Weather.Sunny,
                Weather.Rain  => _rng.NextDouble() < 0.20 ? Weather.Fog   : Weather.Rain,
                Weather.Fog   => _rng.NextDouble() < 0.50 ? Weather.Sunny : Weather.Fog,
                _             => Weather.Sunny,
            };
        }
    }
}
