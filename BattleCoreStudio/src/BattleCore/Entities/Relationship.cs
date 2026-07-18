namespace BattleCore.Entities
{
    /// <summary>
    /// 武将間の人間関係を表すエンティティ。
    /// 会話履歴の設計方針：「忠誠80だから裏切らない」ではなく
    /// 主君との関係・最近の扱われ方・家臣同士の派閥・性格・状況でAIが判断する基盤。
    /// </summary>
    public class Relationship : Entity
    {
        /// <summary>関係の起点となる武将ID。</summary>
        public int FromOfficerId { get; }

        /// <summary>関係の対象となる武将ID。</summary>
        public int ToOfficerId { get; }

        /// <summary>信頼度。高いほど協力・援護行動を取りやすい。</summary>
        public int Trust { get; set; }

        /// <summary>尊敬度。命令への従順さに影響する。</summary>
        public int Respect { get; set; }

        /// <summary>反感度。高いほど対立・妨害行動を取りやすい。</summary>
        public int Dislike { get; set; }

        public Relationship(int id, int fromOfficerId, int toOfficerId)
            : base(id)
        {
            FromOfficerId = fromOfficerId;
            ToOfficerId = toOfficerId;
        }
    }
}
