using BattleCore.Simulation;

namespace BattleCore.Save
{
    /// <summary>
    /// セーブデータ本体。
    /// メタ情報 + シミュレーション状態の完全スナップショット。
    /// SaveSystem が JSON にシリアライズ／デシリアライズする。
    /// </summary>
    public class SaveData
    {
        /// <summary>メタ情報（バージョン・日時・シナリオ・ターン・フェーズ）。</summary>
        public SaveMetadata Metadata { get; set; } = new();

        /// <summary>ゲーム内時間のスナップショット。</summary>
        public GameTimeSnapshot Time { get; set; } = new();

        /// <summary>現在のターンフェーズ。</summary>
        public TurnPhase CurrentPhase { get; set; }

        /// <summary>WorldState のスナップショット。</summary>
        public WorldSnapshot World { get; set; } = new();
    }
}
