using BattleCore.Commands;
using System.Collections.Generic;

namespace BattleCore.Player
{
    /// <summary>
    /// ローカルプレイヤー用の命令入口。
    /// UI や外部入力層から EnqueueCommand() で命令を受け取り、
    /// Step 直前に FlushTo() で SimulationContext.CommandQueue へ移す。
    ///
    /// このクラス自体は ISimulationSystem ではなく SimulationEngine に登録しない。
    /// 呼び出しタイミングは UI 側（将来の LocalPlayerController 統合層）が管理する。
    /// </summary>
    public class PlayerController : IPlayerController
    {
        private readonly Queue<ICommand> _pending = new();

        /// <summary>
        /// プレイヤーの命令を内部キューに積む。
        /// UI のボタン操作・マウス入力などから呼ぶ。
        /// </summary>
        public void EnqueueCommand(ICommand command) => _pending.Enqueue(command);

        /// <summary>
        /// 内部キューの命令を全て CommandQueue へ移す。
        /// SimulationEngine.Step() の直前に呼ぶこと。
        /// </summary>
        public void FlushTo(CommandQueue commandQueue)
        {
            while (_pending.Count > 0)
                commandQueue.Enqueue(_pending.Dequeue());
        }

        /// <summary>未送信の命令数。デバッグ・テスト用途。</summary>
        public int PendingCount => _pending.Count;
    }
}
