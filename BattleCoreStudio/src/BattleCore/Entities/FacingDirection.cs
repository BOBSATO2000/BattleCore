namespace BattleCore.Entities
{
    /// <summary>
    /// 軍の向き。HexDirection と対応する6方向。
    /// MovementSystem が移動時に更新し、DamageCalculator が側面・背後補正に使用する。
    /// </summary>
    public enum FacingDirection
    {
        East,
        West,
        NorthEast,
        NorthWest,
        SouthEast,
        SouthWest,
    }
}
