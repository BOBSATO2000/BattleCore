namespace BattleCore.Events
{
    /// <summary>スパイが敵情報を入手したときに発火するイベント。</summary>
    public class IntelEvent : IGameEvent
    {
        public string SpyClanName    { get; }
        public string TargetClanName { get; }
        public string Info           { get; }   // 例: "武田軍 兵力:320"

        public IntelEvent(string spyClanName, string targetClanName, string info)
        {
            SpyClanName    = spyClanName;
            TargetClanName = targetClanName;
            Info           = info;
        }
    }
}
