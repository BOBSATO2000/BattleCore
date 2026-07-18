using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.World;

namespace BattleCore.AI
{
    /// <summary>
    /// 軍の行動決定インターフェース。
    /// このインターフェースを実装することで AI を差し替え可能にする。
    /// 
    /// 将来の実装例（会話履歴 7.txt より）：
    ///   SimpleArmyDecision  - 隣接敵に近づく簡易AI
    ///   AggressiveAI        - 積極的に攻撃する
    ///   DefensiveAI         - 守りを固める
    ///   PlayerDecision      - プレイヤー入力
    ///   NetworkPlayer       - ネットワーク対戦
    ///   ScriptAI            - イベントスクリプト
    /// </summary>
    public interface IArmyDecision
    {
        /// <summary>
        /// 軍の行動を決定し、命令を返す。
        /// 何もしない場合は null を返す。
        /// </summary>
        ICommand? Decide(Army army, WorldState world);
    }
}
