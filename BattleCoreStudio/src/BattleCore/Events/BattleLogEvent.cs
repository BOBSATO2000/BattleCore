using BattleCore.Systems.Battle;

namespace BattleCore.Events
{
    /// <summary>
    /// 戦闘結果の詳細ログイベント。
    /// BattleResolver が生成し、BattleSystem が EventQueue に積む。
    /// Breakdown に各補正の内訳が格納され、UIがプレイヤーに表示する。
    /// </summary>
    public class BattleLogEvent : IGameEvent
    {
        public string          WinnerName    { get; }
        public string          LoserName     { get; }
        public int             WinnerLosses  { get; }
        public int             LoserLosses   { get; }
        public BattleBreakdown Breakdown     { get; }
        public string?         GrowthDetail  { get; }

        public BattleLogEvent(
            string          winnerName,
            string          loserName,
            int             winnerLosses,
            int             loserLosses,
            BattleBreakdown breakdown,
            string?         growthDetail = null)
        {
            WinnerName   = winnerName;
            LoserName    = loserName;
            WinnerLosses = winnerLosses;
            LoserLosses  = loserLosses;
            Breakdown    = breakdown;
            GrowthDetail = growthDetail;
        }

        /// <summary>
        /// ログ表示用の1行サマリー。
        /// 例: ⚔ 信長 勝利 (+320損害) vs 義元 (-800損害) [背後攻撃 +50% / 総補正 ×1.50]
        /// </summary>
        public string ToLogLine()
        {
            var bd = Breakdown.Entries.Count > 0 ? $" [{Breakdown.ToLogString()}]" : "";
            var growth = GrowthDetail != null ? $" ★{GrowthDetail}" : "";
            return $"⚔ {WinnerName} 勝利 (-{WinnerLosses}) vs {LoserName} (-{LoserLosses}){bd}{growth}";
        }
    }
}
