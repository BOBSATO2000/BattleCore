namespace BattleCore.AI
{
    /// <summary>
    /// 数ターン単位の作戦目標。
    /// StrategyLayer が CampaignPlan に設定し、AggressiveClanStrategy が参照する。
    /// </summary>
    public enum CampaignGoal
    {
        /// <summary>指定城を包囲・占領する（3〜5ターン）。</summary>
        CaptureCastle,

        /// <summary>指定地点に集結して防衛線を構築する（2〜4ターン）。</summary>
        Consolidate,

        /// <summary>敵補給線を断つため敵Camp周辺に展開する（2〜3ターン）。</summary>
        DisruptSupply,

        /// <summary>兵力・食糧を回復するため自城へ撤退する（2〜3ターン）。</summary>
        Withdraw,

        /// <summary>敵の動向を把握するため前線を偵察する（1〜2ターン）。</summary>
        Reconnaissance,

        /// <summary>井戸・神社などの戦略資源を確保・防衛する（2〜3ターン）。</summary>
        SecureStrategicPoint,
    }
}
