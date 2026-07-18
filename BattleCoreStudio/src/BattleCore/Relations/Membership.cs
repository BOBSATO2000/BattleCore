using BattleCore.Entities;

namespace BattleCore.Relations
{
    /// <summary>
    /// 武将と勢力の所属関係を表すエンティティ。
    /// Officer と Clan を直接参照で結ばず、この中間クラスで管理することで
    /// 「勢力を渡り歩く武将」「複数勢力への二重仕官」などを表現できる。
    /// </summary>
    public class Membership : Entity
    {
        /// <summary>所属する武将のID。</summary>
        public int OfficerId { get; }

        /// <summary>所属する勢力のID。</summary>
        public int ClanId { get; }

        /// <summary>
        /// この勢力への忠誠心（0〜100）。
        /// Officer.Loyalty とは別に、勢力ごとの忠誠を管理する。
        /// 初期値50（中立）。
        /// </summary>
        public int Loyalty { get; set; }

        public Membership(int id, int officerId, int clanId)
            : base(id)
        {
            OfficerId = officerId;
            ClanId = clanId;
            Loyalty = 50;
        }
    }
}
