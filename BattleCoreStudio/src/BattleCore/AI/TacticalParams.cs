namespace BattleCore.AI
{
    /// <summary>
    /// 戦術評価のしきい値パラメータ。
    /// AiParams に含まれ、ai_params.json から読み込む。
    /// コードを変更せずに AI の戦術判断を調整できる。
    /// </summary>
    public sealed class TacticalParams
    {
        /// <summary>食糧がこの値以下になったら補給命令を優先する。</summary>
        public int SupplyFoodThreshold    { get; set; } = 30;

        /// <summary>士気がこの値以下になったら防御態勢を取る。</summary>
        public int DefendMoraleThreshold  { get; set; } = 40;

        /// <summary>兵力がこの値以下になったら撤退する。</summary>
        public int RetreatSoldierThreshold { get; set; } = 300;

        /// <summary>敵がこの距離以内に来たら塹壕を掘る（Forest/Mountain時）。</summary>
        public int EntrenchEnemyDist      { get; set; } = 2;

        /// <summary>敵がこの距離以内に来たら築城する（城から遠い時）。</summary>
        public int FortifyEnemyDist       { get; set; } = 3;

        /// <summary>城からこの距離以上離れている場合に築城を検討する。</summary>
        public int FortifyMinCastleDist   { get; set; } = 4;

        /// <summary>敵がこの距離以内に来たら偵察命令を出す（視界外の場合）。</summary>
        public int ScoutEnemyDist         { get; set; } = 5;

        /// <summary>デフォルト値のインスタンスを返す。</summary>
        public static TacticalParams Default => new();
    }
}
