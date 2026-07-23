using BattleCore.Entities;
using BattleCore.World;
using System.Collections.Generic;

namespace BattleCore.Player
{
    /// <summary>
    /// 勢力の意図生成者。AICommander と PlayerCommander の共通インターフェース。
    ///
    /// Commander は「何をしたいか（Intent）」だけを返す。
    /// 「どう実行するか（ICommand）」は OfficerDecision が決める。
    ///
    /// 拡張例：
    ///   ReplayCommander   - リプレイ再生
    ///   NetworkCommander  - 通信対戦
    ///   ScriptCommander   - イベント専用AI
    ///   TutorialCommander - チュートリアル
    ///   DebugCommander    - デバッグ用
    /// </summary>
    public interface ICommander
    {
        /// <summary>この Commander が担当する勢力ID。</summary>
        int ClanId { get; }

        /// <summary>
        /// 今Tickの意図リストを返す。
        /// PlayerCommander は入力がなければ空リストを返す（NoOrder）。
        /// AICommander は毎Tick自動生成する。
        /// </summary>
        IEnumerable<Intent> GenerateIntents(Clan clan, WorldState world);
    }
}
