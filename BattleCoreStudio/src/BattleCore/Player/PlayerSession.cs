using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCore.Player
{
	/// <summary>
	/// プレイヤーのセッション情報。操作中の勢力 ID を保持する。
	/// 将来的にネットワーク対戦・リプレイ機能でプレイヤー識別に使用する予定。
	/// </summary>
	public sealed class PlayerSession
	{
		/// <summary>プレイヤーが操作する勢力のID。</summary>
		public int PlayerClanId { get; }

		public PlayerSession(int playerClanId)
		{
			PlayerClanId = playerClanId;
		}

		/// <summary>指定した勢力IDがプレイヤーの勢力かどうかを返す。</summary>
		public bool OwnsClan(int clanId)
			=> clanId == PlayerClanId;
	}
}
