namespace BattleCore.Entities
{
    /// <summary>
    /// 陣形。FormationOrder で設定し、BattleModifier・MovementSystem が参照する。
    /// </summary>
    public enum ArmyFormation
    {
        /// <summary>通常。補正なし。</summary>
        Normal,
        /// <summary>横陣。側面耐性+、前後弱。</summary>
        Line,
        /// <summary>縦陣。前進速度+、側面弱。</summary>
        Column,
        /// <summary>鶴翼。包囲攻撃+、防御弱。</summary>
        Crane,
        /// <summary>魚鱗。正面突破+、包囲弱。</summary>
        Wedge,
        /// <summary>方円。全方位防御+、機動力-。</summary>
        Circle,
    }
}
