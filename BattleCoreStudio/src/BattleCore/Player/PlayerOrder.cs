namespace BattleCore.Player
{
    /// <summary>
    /// 大名がプレイヤーとして武将に与える方針命令の種類。
    /// 直接命令（Direct）は具体的なHex/対象を指定する。
    /// 方針命令（それ以外）は武将AIが具体的な行動を決定する。
    /// </summary>
    public enum PlayerOrderType
    {
        // ── 方針命令（武将AIが具体行動を決定）──
        Attack,      // 敵を攻撃せよ（最寄り敵城へ進軍）
        Defend,      // この城を守れ（最寄り自城で防御態勢）
        Retreat,     // 撤退せよ（最寄り自城へ）
        Siege,       // 包囲を維持せよ（指定城を包囲）
        Scout,       // 偵察せよ（現地で偵察態勢）
        Supply,      // 補給を優先せよ（最寄りCampへ）
        Fortify,     // 築城せよ（現地に柵を建設）
        // ── 直接命令（プレイヤーが具体的に指定）──
        DirectMove,  // 指定Hexへ移動
        DirectWait,  // 待機
    }

    /// <summary>
    /// プレイヤー（大名）が特定の軍に発行する命令。
    /// PlayerDecisionSystem がこれを受け取り、OfficerDecision を通して CommandQueue へ流す。
    /// </summary>
    public sealed class PlayerOrder
    {
        public int             ArmyId    { get; }
        public PlayerOrderType OrderType { get; }
        /// <summary>DirectMove 時の移動先HexId。それ以外は null。</summary>
        public int?            TargetHexId { get; }
        /// <summary>Siege 時の対象城Id。それ以外は null。</summary>
        public int?            TargetCastleId { get; }

        public PlayerOrder(int armyId, PlayerOrderType orderType,
            int? targetHexId = null, int? targetCastleId = null)
        {
            ArmyId         = armyId;
            OrderType      = orderType;
            TargetHexId    = targetHexId;
            TargetCastleId = targetCastleId;
        }
    }
}
