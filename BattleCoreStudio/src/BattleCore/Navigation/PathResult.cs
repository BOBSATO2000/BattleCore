namespace BattleCore.Navigation
{
    /// <summary>
    /// A* 経路探索の結果。HexId列とステップごとのコストを保持する。
    /// DebugPanelBuilder / マップ描画で共通利用する。
    /// </summary>
    public sealed class PathResult
    {
        public static readonly PathResult Empty = new([], []);

        /// <summary>経路上の HexId 列（start 含む）。</summary>
        public IReadOnlyList<int> HexIds { get; }

        /// <summary>各ステップの移動コスト（HexIds[i-1]→HexIds[i]）。HexIds[0] は 0。</summary>
        public IReadOnlyList<int> StepCosts { get; }

        /// <summary>合計コスト。</summary>
        public int TotalCost { get; }

        public PathResult(IReadOnlyList<int> hexIds, IReadOnlyList<int> stepCosts)
        {
            HexIds    = hexIds;
            StepCosts = stepCosts;
            TotalCost = stepCosts.Sum();
        }
    }
}
