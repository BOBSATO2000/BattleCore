namespace BattleCore.Entities
{
    /// <summary>
    /// 兵種ごとの係数データ。
    /// MovementSystem・DamageCalculator が UnitType から係数を取得するために使用する。
    /// ルールを追加する場合はここだけ変更すればよい。
    /// </summary>
    public static class UnitTypeData
    {
        /// <summary>
        /// 平地移動コスト係数。1.0=通常、0.0=AP消費なし（騎馬）。
        /// </summary>
        public static double PlainMoveCostFactor(UnitType type) => type switch
        {
            UnitType.Cavalry => 0.0,  // 平地はAP消費なし
            _                => 1.0,
        };

        /// <summary>
        /// 攻撃時の先制係数。相手の実効兵力にこの値を乗算してから勝敗判定する。
        /// 1.0=先制なし、0.9=鉄砲先制（相手10%削減）。
        /// </summary>
        public static double PreemptiveFactor(UnitType type) => type switch
        {
            UnitType.Arquebus => 0.90,
            _                 => 1.00,
        };

        /// <summary>
        /// 騎馬突撃係数。Cavalry が Spear 以外に攻撃する場合に適用。
        /// </summary>
        public static double CavalryChargeFactor(UnitType defenderType) => defenderType switch
        {
            UnitType.Spear => 1.0,  // 槍は突撃を無効化
            _              => 1.3,
        };
    }
}
