using BattleCore.Entities;

namespace BattleCore.Battle
{
    /// <summary>
    /// 1件の戦闘情報を表すクラス。
    /// 将来的には地形・天候・時刻・城・防御補正などの情報もここに追加できる。
    /// </summary>
    public class Battle
    {
        /// <summary>攻撃側の軍。</summary>
        public Army Attacker { get; }

        /// <summary>防御側の軍。</summary>
        public Army Defender { get; }

        public Battle(Army attacker, Army defender)
        {
            Attacker = attacker;
            Defender = defender;
        }
    }
}
