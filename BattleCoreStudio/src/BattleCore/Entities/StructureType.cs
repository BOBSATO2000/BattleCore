namespace BattleCore.Entities
{
    /// <summary>
    /// 構造物の種類。各Systemが参照して効果を適用する。
    /// </summary>
    public enum StructureType
    {
        /// <summary>物見櫓。VisionSystem が視界+2を適用する。</summary>
        Tower,
        /// <summary>柵。SiegeSystem が包囲時間+5Tickを適用する。</summary>
        Palisade,
        /// <summary>野営地。FoodSystem が毎Tick食糧+15を補給する。</summary>
        Camp,
        /// <summary>橋。MovementSystem が River 地形を通常コストで通過可能にする。</summary>
        Bridge,
        /// <summary>
        /// 井戸。包囲時の食糧消費-50%。
        /// 破壊されると効果がなくなる。
        /// </summary>
        Well,
        /// <summary>
        /// 街道。同Hexの移動AP消費なし（騎馬は元々無消費なので変化なし）。
        /// </summary>
        Road,
        /// <summary>
        /// 神社・寺。同Hexの自軍の士気+3/Tick。
        /// </summary>
        Shrine,
    }
}
