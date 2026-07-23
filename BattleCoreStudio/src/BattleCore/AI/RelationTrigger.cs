using BattleCore.Entities;

namespace BattleCore.AI
{
    /// <summary>
    /// 2武将間の関係値が特定の条件を満たしたとき確率でイベントを発火するトリガー。
    /// RelationshipSystem.Update() の末尾で評価される。
    /// </summary>
    public sealed class RelationTrigger
    {
        /// <summary>トリガーを識別するID。デバッグ・ログ用途。</summary>
        public string Id { get; init; } = "";

        /// <summary>
        /// 発火条件。Relationship を受け取り条件を満たす場合に true を返す。
        /// 例：rel.Trust >= 80 &amp;&amp; rel.Dislike &lt; 10
        /// </summary>
        public Func<Relationship, bool> Condition { get; init; } = _ => false;

        /// <summary>発火確率（0.0〜1.0）。</summary>
        public float Probability { get; init; }

        /// <summary>
        /// 発火時に生成するイベントのファクトリ関数。
        /// 関係の起点となる武将と対象の武将を受け取り IGameEvent を返す。
        /// </summary>
        public Func<Officer, Officer, IGameEvent> EventFactory { get; init; }
            = (_, _) => throw new NotImplementedException("EventFactory が未設定です。");
    }
}
