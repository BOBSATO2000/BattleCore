namespace BattleCore.Map
{
    /// <summary>
    /// Hexの地形種別。MovementSystem の移動可否判定に使用する。
    /// 将来的には地形コスト（移動力消費）・戦闘補正にも影響させる予定。
    /// </summary>
    public enum TerrainType
    {
        /// <summary>平地。移動コスト1。補正なし。</summary>
        Plain,

        /// <summary>森。移動コスト2。防御補正あり。</summary>
        Forest,

        /// <summary>山岳。移動不可。防御補正大。</summary>
        Mountain,

        /// <summary>
        /// 河川。移動コスト+2（渡河）。
        /// 同Hexに Bridge がある場合は通常コストで通過可能。
        /// </summary>
        River,
    }
}
