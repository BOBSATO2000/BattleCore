using BattleCore.Entities;

namespace BattleCore.Systems.Battle
{
    /// <summary>
    /// 1件の戦闘結果を表す不変クラス。
    /// Breakdown に各補正の内訳が格納される。
    /// </summary>
    public sealed class BattleResult
    {
        public Army            Winner       { get; }
        public Army            Loser        { get; }
        public int             WinnerLosses { get; }
        public int             LoserLosses  { get; }
        public BattleBreakdown Breakdown    { get; }

        public BattleResult(Army winner, Army loser, int winnerLosses, int loserLosses, BattleBreakdown breakdown)
        {
            Winner       = winner;
            Loser        = loser;
            WinnerLosses = winnerLosses;
            LoserLosses  = loserLosses;
            Breakdown    = breakdown;
        }
    }
}
