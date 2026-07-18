using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.World;
using System.Collections.Generic;

namespace BattleCore.AI
{
    /// <summary>
    /// 勢力単位の戦略決定インターフェース。
    /// IArmyDecision が Army 単位で動くのに対し、こちらは Clan 全体の戦略を担当する。
    /// 
    /// 将来の実装例：
    ///   AggressiveClanStrategy  - 最も近い敵勢力へ全軍で進軍する
    ///   DefensiveClanStrategy   - 領地を守り敵が来たら迎撃する
    ///   ExpansionClanStrategy   - 弱い敵から順に攻める
    /// </summary>
    public interface IClanStrategy
    {
        /// <summary>
        /// 勢力の戦略を決定し、命令リストを返す。
        /// 複数の Army に対して命令を出せるよう IEnumerable で返す。
        /// </summary>
        IEnumerable<ICommand> Decide(Clan clan, WorldState world);
    }
}
