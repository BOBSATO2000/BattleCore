namespace BattleCore.Simulation
{
    /// <summary>
    /// 天気の種別。
    ///   Sunny: 通常。補正なし。
    ///   Rain : 雨。Forest進入コスト+1、戦闘ダメージ10%減。
    ///   Fog  : 霧。AIの移動判断を妨害（将来実装）。
    /// </summary>
    public enum Weather
    {
        Sunny,
        Rain,
        Fog,
    }
}
