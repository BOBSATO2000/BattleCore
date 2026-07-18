namespace BattleCore.Entities
{
    /// <summary>
    /// 武将エンティティ。
    /// 会話履歴の設計方針：「数字を操作するゲームではなく人間ドラマをシミュレーションする」
    /// 能力値の高低だけでなく、性格・関係・状況によってAIが判断を変える基盤となる。
    /// </summary>
    public class Officer : Entity
    {
        /// <summary>武将名。</summary>
        public string Name { get; }

        /// <summary>
        /// 忠誠心。裏切り・離反判定に使用する。
        /// 主君との関係・最近の扱われ方・派閥などで動的に変化させる予定。
        /// </summary>
        public int Loyalty { get; set; }

        /// <summary>
        /// 知略。外交・謀略・戦術判断に影響する。
        /// </summary>
        public int Intelligence { get; set; }

        /// <summary>
        /// 野心。自立・謀反の判断に影響する。
        /// 高いほど独立行動を取りやすい。
        /// </summary>
        public int Ambition { get; set; }

        /// <summary>
        /// 統率力。兵を率いる力。戦闘時の兵力補正に影響する。
        /// </summary>
        public int Leadership { get; set; }

        /// <summary>
        /// 戦術能力。戦場での判断・陣形・奇襲などに影響する。
        /// </summary>
        public int Strategy { get; set; }

        /// <summary>
        /// 武勇。一騎打ち・突撃など個人戦闘能力に影響する。
        /// 会話履歴（2.txt）で能力値として挙げられていたが当初実装から漏れていたため追加。
        /// </summary>
        public int Courage { get; set; }

        /// <summary>戦闘勝利回数。成長システムで使用する。</summary>
        public int BattleWins { get; set; }

        public Officer(int id, string name) : base(id)
        {
            Name = name;
        }
    }
}
