using System.Text.Json;

namespace BattleCore.AI
{
    /// <summary>
    /// ai_params.json を読み込んで AiParams を返す。
    /// ファイルが存在しない場合はデフォルト値を返す。
    /// </summary>
    public static class AiParamsLoader
    {
        private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        /// <summary>指定パスの JSON ファイルを読み込む。ファイルが存在しない・パース失敗時はデフォルト値を返す。</summary>
        public static AiParams Load(string path)
        {
            if (!File.Exists(path)) return AiParams.Default;
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AiParams>(json, _opts) ?? AiParams.Default;
            }
            catch
            {
                return AiParams.Default;
            }
        }

        /// <summary>実行ファイルと同じディレクトリの ai_params.json を読み込む。</summary>
        public static AiParams LoadFromBaseDir()
            => Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ai_params.json"));
    }
}
