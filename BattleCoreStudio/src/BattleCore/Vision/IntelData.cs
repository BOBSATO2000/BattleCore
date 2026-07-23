namespace BattleCore.Vision
{
    /// <summary>
    /// 諜報で判明した敵軍の既知位置情報。
    /// VisionData（現在見えている）とは別の情報層。
    ///
    /// Vision  = 今まさに見えている
    /// Intel   = 情報として知っている（最後に判明した位置）
    ///
    /// AIはまずVisionを参照し、見えなければIntelを参照する。
    /// これにより「見失った敵を最後に見た位置まで追う」動作が生まれる。
    /// </summary>
    public class IntelData
    {
        /// <summary>情報を持つ勢力ID。</summary>
        public int OwnerClanId { get; }

        /// <summary>情報対象の敵軍ID。</summary>
        public int EnemyArmyId { get; }

        /// <summary>最後に判明した敵軍のHexID。</summary>
        public int LastKnownHexId { get; set; }

        /// <summary>情報を取得したTick。古い情報の判定に使用（将来拡張用）。</summary>
        public int AcquiredTick { get; set; }

        public IntelData(int ownerClanId, int enemyArmyId, int lastKnownHexId, int acquiredTick)
        {
            OwnerClanId    = ownerClanId;
            EnemyArmyId    = enemyArmyId;
            LastKnownHexId = lastKnownHexId;
            AcquiredTick   = acquiredTick;
        }
    }
}
