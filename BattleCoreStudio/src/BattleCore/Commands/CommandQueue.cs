using System.Collections;
using System.Collections.Generic;

namespace BattleCore.Commands
{
    /// <summary>
    /// ゲーム内命令キュー。
    /// AI（ClanDecisionSystem）とプレイヤー（PlayerController）の両方が Enqueue し、
    /// CommandExecutionSystem が Dequeue して実行する。
    ///
    /// Queue&lt;ICommand&gt; を直接公開せずこのクラスでラップすることで、
    /// 将来の優先度付きキュー・命令キャンセル・ログ記録などへ拡張できる。
    /// IEnumerable&lt;ICommand&gt; を実装することでテストの Assert.HasCount / Assert.IsEmpty に対応する。
    /// </summary>
    public class CommandQueue : IEnumerable<ICommand>
    {
        private readonly Queue<ICommand> _queue = new();

        /// <summary>キューに積まれている命令の数。</summary>
        public int Count => _queue.Count;

        /// <summary>命令をキューに積む。</summary>
        public void Enqueue(ICommand command) => _queue.Enqueue(command);

        /// <summary>先頭の命令を取り出す。</summary>
        public ICommand Dequeue() => _queue.Dequeue();

        /// <summary>キューを空にする。</summary>
        public void Clear() => _queue.Clear();

        /// <inheritdoc/>
        public IEnumerator<ICommand> GetEnumerator() => _queue.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();
    }
}
