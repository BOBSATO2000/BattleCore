using System;

namespace BattleCore.Save
{
    /// <summary>
    /// セーブデータのメタ情報。
    /// セーブ一覧表示・バージョン互換チェックに使用する。
    /// </summary>
    public class SaveMetadata
    {
        /// <summary>セーブデータのフォーマットバージョン。互換性チェックに使用。</summary>
        public int Version { get; set; } = SaveSystem.CurrentVersion;

        /// <summary>セーブ時のエンジンバージョン。「どのバージョンで作られたセーブか」を記録する。</summary>
        public string EngineVersion { get; set; } = SaveSystem.EngineVersion;

        /// <summary>セーブ日時（UTC）。</summary>
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        /// <summary>シナリオID（例: "sengoku1560"）。</summary>
        public string ScenarioId { get; set; } = "";

        /// <summary>セーブ時のターン数。</summary>
        public int Turn { get; set; }

        /// <summary>セーブ時のフェーズ名。</summary>
        public string Phase { get; set; } = "";
    }
}
