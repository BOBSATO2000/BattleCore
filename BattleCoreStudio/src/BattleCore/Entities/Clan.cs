using System.Collections.Generic;

namespace BattleCore.Entities
{
    /// <summary>
    /// 勢力エンティティ。大名家・武将集団を表す。
    /// BattleSystem はこの ClanId を使って味方・敵を判定する。
    /// 将来的には ClanDecisionSystem が勢力単位でAI判断を行う。
    /// </summary>
    public class Clan : Entity
    {
        private static int _nextId = 1;

        /// <summary>勢力名。</summary>
        public string Name { get; set; } = "";

        /// <summary>資金。内政・雇用・外交に使用する（将来実装）。</summary>
        public int Gold { get; set; }

        /// <summary>所属武将IDリスト。</summary>
        public List<int> OfficerIds { get; } = new();

        /// <summary>所属軍IDリスト。</summary>
        public List<int> ArmyIds { get; } = new();

        /// <summary>IDを直接指定するコンストラクタ。</summary>
        public Clan(int id) : base(id) { }

        /// <summary>名前を指定するコンストラクタ。IDは自動採番。</summary>
        public Clan(string name) : base(_nextId++)
        {
            Name = name;
        }
    }
}
