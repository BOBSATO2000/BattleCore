namespace BattleCore.Events
{
    /// <summary>計略発動イベント。</summary>
    public class StrategyEvent : IGameEvent
    {
        public int    ArmyId      { get; }
        public string OfficerName { get; }
        public string StrategyName { get; }
        public string TargetDesc  { get; }

        public StrategyEvent(int armyId, string officerName, string strategyName, string targetDesc)
        {
            ArmyId       = armyId;
            OfficerName  = officerName;
            StrategyName = strategyName;
            TargetDesc   = targetDesc;
        }

        public string ToLogLine()
            => $"【計略】{OfficerName}が「{StrategyName}」を発動 → {TargetDesc}";
    }

    /// <summary>一騎討ちイベント。</summary>
    public class DuelEvent : IGameEvent
    {
        public string ChallengerName { get; }
        public string DefenderName   { get; }
        public bool   ChallengerWon  { get; }
        public int    MoraleDelta    { get; }

        public DuelEvent(string challengerName, string defenderName, bool challengerWon, int moraleDelta)
        {
            ChallengerName = challengerName;
            DefenderName   = defenderName;
            ChallengerWon  = challengerWon;
            MoraleDelta    = moraleDelta;
        }

        public string ToLogLine()
        {
            string result = ChallengerWon
                ? $"{ChallengerName}が{DefenderName}を打ち破る！敵士気{MoraleDelta}"
                : $"{DefenderName}が{ChallengerName}を退ける。味方士気{MoraleDelta}";
            return $"【一騎討ち】{result}";
        }
    }
}
