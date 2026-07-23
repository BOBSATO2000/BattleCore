using BattleCore.Entities;

namespace BattleCore.Battle
{
    /// <summary>
    /// 1件の戦闘情報を表すクラス。
    /// </summary>
    public class Battle
    {
        public Army Attacker { get; }
        public Army Defender { get; }

        /// <summary>
        /// 隣接戦闘フラグ。
        /// true の場合、攻撃側が敵Hexへ侵入を試みた戦闘。
        /// 勝者が敵Hexを占領し、敗者は元のHexへ押し返される。
        /// </summary>
        public bool IsAdjacentBattle { get; set; } = false;

        public Battle(Army attacker, Army defender)
        {
            Attacker = attacker;
            Defender = defender;
        }
    }
}
