namespace BattleCore.Player
{
    /// <summary>
    /// 大名（Commander）が武将に伝える「意図」の種類。
    /// 「どう実行するか」はOfficerDecisionが決める。
    /// </summary>
    public enum IntentType
    {
        Attack,    // 敵を攻撃したい（最寄り敵城へ）
        Defend,    // 守りたい（防御態勢）
        Retreat,   // 退きたい（最寄り自城へ）
        Siege,     // 包囲したい（指定城）
        Scout,     // 偵察したい
        Supply,    // 補給したい
        Fortify,   // 築城したい
        MoveTo,    // 指定Hexへ移動したい
        Wait,      // 待機したい
    }

    /// <summary>
    /// Commander が生成する「意図」。
    /// ICommand（実行方法）は持たない。OfficerDecisionがIntentをCommandに変換する。
    ///
    /// Priority ルール：
    ///   100 = AI緊急割り込み（Food=0, Morale≤10）
    ///    50 = AI戦術判断
    ///    10 = プレイヤー命令（デフォルト）
    ///     0 = 待機
    ///
    /// Lifetime：
    ///   OneShot    = 1Tick実行で消える（偵察・待機など）
    ///   Persistent = 目標到達 or キャンセルまで毎Tick継続（移動・包囲など）
    /// </summary>
    public sealed class Intent
    {
        public int        ArmyId        { get; }
        public IntentType Type          { get; }
        public int        Priority      { get; }
        public OrderLifetime Lifetime   { get; }

        /// <summary>MoveTo 時の目標HexId。</summary>
        public int? TargetHexId    { get; }
        /// <summary>Siege 時の対象城Id。</summary>
        public int? TargetCastleId { get; }

        public Intent(int armyId, IntentType type,
            int priority = 10, OrderLifetime lifetime = OrderLifetime.OneShot,
            int? targetHexId = null, int? targetCastleId = null)
        {
            ArmyId         = armyId;
            Type           = type;
            Priority       = priority;
            Lifetime       = lifetime;
            TargetHexId    = targetHexId;
            TargetCastleId = targetCastleId;
        }
    }
}
