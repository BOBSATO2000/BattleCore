using BattleCore.Entities;

namespace BattleCore.Systems.Battle
{
    /// <summary>
    /// 1件の戦闘結果を表す不変クラス。
    /// BattleResolver が生成し、BattleSystem が兵力更新に使用する。
    /// </summary>
    public sealed class BattleResult
    {
        /// <summary>勝者の軍。</summary>
        public Army Winner { get; }

        /// <summary>敗者の軍。</summary>
        public Army Loser { get; }

        /// <summary>勝者が失った兵力数。</summary>
        public int WinnerLosses { get; }

        /// <summary>敗者が失った兵力数。</summary>
        public int LoserLosses { get; }

        public BattleResult(Army winner, Army loser, int winnerLosses, int loserLosses)
        {
            Winner = winner;
            Loser = loser;
            WinnerLosses = winnerLosses;
            LoserLosses = loserLosses;
        }
    }
}
