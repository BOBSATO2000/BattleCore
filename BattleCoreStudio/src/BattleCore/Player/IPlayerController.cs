using BattleCore.Commands;

namespace BattleCore.Player
{
    /// <summary>
    /// プレイヤー入力から命令を生成する層のインターフェース。
    /// IsPlayerControlled == true の Clan に対して、
    /// AI（ClanDecisionSystem）の代わりに命令を CommandQueue へ流す役割を持つ。
    ///
    /// 将来の実装例：
    ///   LocalPlayerController   - WinForms / UI からのマウス・キー入力
    ///   NetworkPlayerController - ネットワーク対戦での受信命令
    ///   ReplayController        - リプレイファイルからの命令再生
    /// </summary>
    public interface IPlayerController
    {
        /// <summary>
        /// プレイヤーの命令を内部キューに積む。
        /// UI や外部入力層がこのメソッドを呼ぶ。
        /// </summary>
        void EnqueueCommand(ICommand command);

        /// <summary>
        /// 内部キューに積まれた命令を CommandQueue へ移す。
        /// SimulationEngine.Step() の直前に呼ぶこと。
        /// </summary>
        void FlushTo(CommandQueue commandQueue);
    }
}
