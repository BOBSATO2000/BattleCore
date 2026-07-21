namespace BattleCoreStudio
{
    /// <summary>
    /// デバッグオーバーレイの表示状態を管理する。
    /// F1-F5 で各レイヤーを切り替える。
    /// </summary>
    internal sealed class DebugOverlay
    {
        public bool DebugConsole { get; private set; } = true;  // F1
        public bool Path         { get; private set; } = true;  // F2
        public bool FogOfWar     { get; private set; } = false; // F3
        public bool AIDecision   { get; private set; } = true;  // F4
        public bool Vision       { get; private set; } = false; // F5

        /// <summary>キーに対応するレイヤーを切り替える。切り替えた場合 true を返す。</summary>
        public bool HandleKey(Keys key)
        {
            switch (key)
            {
                case Keys.F1: DebugConsole = !DebugConsole; return true;
                case Keys.F2: Path         = !Path;         return true;
                case Keys.F3: FogOfWar     = !FogOfWar;     return true;
                case Keys.F4: AIDecision   = !AIDecision;   return true;
                case Keys.F5: Vision       = !Vision;       return true;
                default: return false;
            }
        }

        public string StatusText =>
            $"[F1:Debug={B(DebugConsole)}] [F2:Path={B(Path)}] [F3:Fog={B(FogOfWar)}] " +
            $"[F4:AI={B(AIDecision)}] [F5:Vision={B(Vision)}]";

        private static string B(bool v) => v ? "ON" : "off";
    }
}
