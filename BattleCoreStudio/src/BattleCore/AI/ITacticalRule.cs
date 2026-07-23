using BattleCore.Commands;

namespace BattleCore.AI
{
    /// <summary>
    /// 「条件 → Order」の評価ルール。
    /// TacticalEvaluator がルール群を順番に評価し、最初に Matches した Order を採用する。
    /// 優先度は登録順で決まる。
    /// </summary>
    public interface ITacticalRule
    {
        /// <summary>この状況でこのルールが適用されるか。</summary>
        bool Matches(TacticalSituation situation, TacticalParams p);

        /// <summary>適用される場合に発行する Order。</summary>
        ICommand? CreateOrder(TacticalSituation situation);
    }
}
