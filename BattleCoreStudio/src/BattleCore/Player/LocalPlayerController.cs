using BattleCore.Commands;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Player
{
	/// <summary>
	/// WinForms UI からのプレイヤー入力を管理する IPlayerController 実装。
	/// マウスクリックで積まれた命令を保持し、Step 直前に FlushTo() で CommandQueue へ移す。
	/// </summary>
	public sealed class LocalPlayerController : IPlayerController
	{
		private readonly Queue<ICommand> pendingCommands = new();

		/// <summary>現在キューに積まれている命令（読み取り専用）。UI表示用。</summary>
		public IEnumerable<ICommand> PendingCommands => pendingCommands;

		public void EnqueueCommand(ICommand command)
		{
			pendingCommands.Enqueue(command);
		}

		public void FlushTo(CommandQueue commandQueue)
		{
			while (pendingCommands.Count > 0)
			{
				commandQueue.Enqueue(pendingCommands.Dequeue());
			}
		}

		/// <summary>指定部隊の未実行命令をキューから削除する。命令上書き・取り消し用。</summary>
		public void CancelCommand(int armyId)
		{
			var remaining = pendingCommands.Where(c => !(c is MoveArmyCommand m && m.ArmyId == armyId)).ToList();
			pendingCommands.Clear();
			foreach (var cmd in remaining)
				pendingCommands.Enqueue(cmd);
		}
	}
}
