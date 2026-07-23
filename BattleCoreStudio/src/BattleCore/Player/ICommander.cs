using BattleCore.Entities;
using BattleCore.World;
using System.Collections.Generic;

namespace BattleCore.Player
{
    /// <summary>
    /// 勢力の命令生成者。AICommander と PlayerCommander の共通インターフェース。
    ///
    /// AICommander    : AggressiveClanStrategy を使って毎Tick自動生成
    /// PlayerCommander: UI入力を待ち、積まれた命令を返す（入力なければ空）
    ///
    /// CommanderSystem が各勢力の ICommander を呼び出し、
    /// 返された CommanderOrder を OfficerDecision に通して CommandQueue へ流す。
    /// </summary>
    public interface ICommander
    {
        /// <summary>この Commander が担当する勢力ID。</summary>
        int ClanId { get; }

        /// <summary>
        /// 今Tickの命令リストを返す。
        /// PlayerCommander は入力がなければ空リストを返す（NoOrder）。
        /// AICommander は毎Tick自動生成する。
        /// </summary>
        IEnumerable<CommanderOrder> GenerateOrders(Clan clan, WorldState world);
    }
}
