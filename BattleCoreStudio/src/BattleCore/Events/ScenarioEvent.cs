namespace BattleCore.Events
{
    /// <summary>シナリオトリガーが発火したときのイベント。UIログに表示する。</summary>
    public class ScenarioEvent : IGameEvent
    {
        public string TriggerId { get; }
        public string Message   { get; }

        public ScenarioEvent(string triggerId, string message)
        {
            TriggerId = triggerId;
            Message   = message;
        }
    }
}
