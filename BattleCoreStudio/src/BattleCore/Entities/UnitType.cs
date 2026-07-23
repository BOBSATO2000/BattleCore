namespace BattleCore.Entities
{
    /// <summary>
    /// 兵種。Army が持つ属性。
    /// 各Systemが兵種を参照して補正を適用する。
    /// </summary>
    public enum UnitType
    {
        /// <summary>足軽。標準的な歩兵。補正なし。</summary>
        Ashigaru,
        /// <summary>騎馬。平地移動+1AP。突撃成功で士気+5。</summary>
        Cavalry,
        /// <summary>鉄砲。戦闘で先制攻撃（攻撃側の損害を先に与える）。</summary>
        Arquebus,
        /// <summary>弓。遠距離支援（将来拡張用）。</summary>
        Archer,
        /// <summary>槍。防御時に騎馬の突撃ボーナスを無効化。</summary>
        Spear,
    }
}
