using BattleCore.Entities;
using BattleCore.Relations;
using BattleCore.World;

namespace BattleCore.AI
{
    /// <summary>
    /// 突発イベントの発火ルール1件。
    /// 条件・確率・確率補正・イベント生成関数をひとまとめにする。
    /// SpontaneousEventTable に登録して使用する。
    /// </summary>
    public sealed class SpontaneousEventRule
    {
        /// <summary>ルールを識別するID。デバッグ・ログ用途。</summary>
        public string Id { get; init; } = "";

        /// <summary>
        /// 発火条件。Officer・主君Officer・Membership・WorldState を受け取り
        /// 条件を満たす場合に true を返す。全条件AND。
        /// </summary>
        public Func<Officer, Officer?, Membership?, WorldState, bool> Condition { get; init; }
            = (_, _, _, _) => false;

        /// <summary>
        /// 基本発火確率（0.0〜1.0）。
        /// ProbabilityModifier が設定されている場合はその戻り値を乗算する。
        /// </summary>
        public float BaseProbability { get; init; }

        /// <summary>
        /// 確率を動的に補正する関数（省略可）。
        /// Officer と主君へのRelationship を受け取り、確率の乗数を返す。
        /// 例：Dislikeが高いほど確率を上げる場合は 1f + rel.Dislike / 100f を返す。
        /// </summary>
        public Func<Officer, Relationship?, float>? ProbabilityModifier { get; init; }

        /// <summary>
        /// 発火時に生成するイベントのファクトリ関数。
        /// Officer と WorldState を受け取り IGameEvent を返す。
        /// </summary>
        public Func<Officer, WorldState, IGameEvent> EventFactory { get; init; }
            = (_, _) => throw new NotImplementedException("EventFactory が未設定です。");
    }

    /// <summary>
    /// 突発イベントテーブル。
    /// 登録された SpontaneousEventRule を毎ターン評価し、
    /// 条件を満たしたルールを確率で発火する。
    /// OfficerDecision に注入して使用する。
    /// </summary>
    public sealed class SpontaneousEventTable
    {
        private readonly List<SpontaneousEventRule> _rules = new();
        private readonly Random _rng;

        /// <summary>登録されているルールの読み取り専用リスト。</summary>
        public IReadOnlyList<SpontaneousEventRule> Rules => _rules;

        /// <summary>
        /// コンストラクタ。乱数インスタンスを外部から注入する。
        /// seed を固定すると再現性のあるテストが可能になる。
        /// </summary>
        public SpontaneousEventTable(Random rng) => _rng = rng;

        /// <summary>ルールをテーブルに追加する。</summary>
        public void Add(SpontaneousEventRule rule) => _rules.Add(rule);

        /// <summary>
        /// 指定した武将に対して全ルールを評価し、発火したイベントを返す。
        /// 条件を満たしたルールを確率判定し、通過したものだけ EventFactory を呼ぶ。
        /// </summary>
        /// <param name="officer">評価対象の武将。</param>
        /// <param name="daimyo">所属勢力の主君。未設定の場合は null。</param>
        /// <param name="membership">この勢力への所属情報。未所属の場合は null。</param>
        /// <param name="relToDaimyo">主君への Relationship。未設定の場合は null。</param>
        /// <param name="world">現在の WorldState。</param>
        public IEnumerable<IGameEvent> Evaluate(
            Officer officer,
            Officer? daimyo,
            Membership? membership,
            Relationship? relToDaimyo,
            WorldState world)
        {
            foreach (var rule in _rules)
            {
                if (!rule.Condition(officer, daimyo, membership, world)) continue;

                var prob = rule.BaseProbability;
                if (rule.ProbabilityModifier != null)
                    prob *= rule.ProbabilityModifier(officer, relToDaimyo);

                prob = Math.Clamp(prob, 0f, 1f);

                if (_rng.NextSingle() < prob)
                    yield return rule.EventFactory(officer, world);
            }
        }
    }
}
