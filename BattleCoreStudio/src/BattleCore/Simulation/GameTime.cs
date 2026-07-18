namespace BattleCore.Simulation
{
    /// <summary>
    /// ゲーム内時間を管理するクラス。
    /// Step ごとに Advance() が呼ばれ、季節→年が進む。
    /// 将来的には季節ごとの農業・補給・行軍速度への影響を実装する。
    /// </summary>
    public class GameTime
    {
        /// <summary>経過ステップ数。テスト・デバッグ用途。</summary>
        public int Tick { get; private set; }

        /// <summary>現在の年。初期値は1560年（戦国時代）。</summary>
        public int Year { get; private set; }

        /// <summary>現在の季節。</summary>
        public Season Season { get; private set; }

        public GameTime()
        {
            Year = 1560;
            Season = Season.Spring;
        }

        /// <summary>
        /// 時間を1ステップ進める。SimulationEngine.Step() の末尾で呼ばれる。
        /// 春→夏→秋→冬→春（年+1）の順で進む。
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
        }
    }
}
