namespace BattleCore.Events
{
    public enum DiplomacyEventType
    {
        CeasefireProposed,   // 停戦申し入れ
        CeasefireAccepted,   // 停戦成立
        CeasefireExpired,    // 停戦期限切れ
        ReinforcementSent,   // 援軍派遣
        AllianceBetrayed,    // 同盟裏切り
    }

    /// <summary>外交イベント。</summary>
    public class DiplomacyEvent : IGameEvent
    {
        public DiplomacyEventType Type      { get; }
        public string             ClanName  { get; }
        public string             TargetName { get; }
        public string             Detail    { get; }

        public DiplomacyEvent(DiplomacyEventType type, string clanName, string targetName, string detail = "")
        {
            Type       = type;
            ClanName   = clanName;
            TargetName = targetName;
            Detail     = detail;
        }
    }
}
