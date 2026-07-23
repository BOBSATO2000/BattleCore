namespace BattleCore.Entities
{
    /// <summary>
    /// 軍の現在の態勢。CommandSystem が設定し、BattleSystem・MovementSystem が参照する。
    /// </summary>
    public enum ArmyStance
    {
        Normal,
        /// <summary>防御態勢。被ダメ-20%、移動しない。</summary>
        Defend,
        /// <summary>迎撃態勢。先制+10%。</summary>
        Intercept,
        /// <summary>奇襲態勢。Forest/Mountainで先制+20%。</summary>
        Ambush,
        /// <summary>包囲命令実行中。</summary>
        Siege,
        /// <summary>撤退中。</summary>
        Retreating,
        /// <summary>偵察中。視界+3、Intel+50%。</summary>
        Scouting,
        /// <summary>塩壕態勢。被ダメ追加-10%、移動不可2Tick。</summary>
        Entrenched,
        /// <summary>陽動態勢。敵AIの目標を自軍に引き付ける。</summary>
        Screening,
        /// <summary>籠城態勢。城ボーナス重複。</summary>
        Garrisoning,
        /// <summary>段階撤退中。毻Tick1Hex後退しながら戦闘継続。</summary>
        PhaseRetreating,
    }
}
