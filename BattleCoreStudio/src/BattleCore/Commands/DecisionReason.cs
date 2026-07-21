namespace BattleCore.Commands
{
    /// <summary>
    /// 命令の判断理由。
    /// MoveArmyCommand に付与され、デバッグ・UI表示・将来のプレイヤー説明に使用する。
    /// </summary>
    public enum DecisionReason
    {
        /// <summary>通常の進軍命令。</summary>
        Advance,

        /// <summary>敵城を優先攻略。</summary>
        TargetCastle,

        /// <summary>兵力不足による撤退。</summary>
        Retreat,

        /// <summary>友軍救援のための移動。</summary>
        Reinforce,

        /// <summary>武将の性格（慎重）による撤退進言。</summary>
        CautiousRetreat,

        /// <summary>武将の独断行動（野心・低忠誠）。</summary>
        IndependentAction,

        /// <summary>主君への不満による命令変更。</summary>
        Dissatisfied,
    }
}
