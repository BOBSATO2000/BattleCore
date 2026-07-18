using BattleCore.Commands;
using BattleCore.World;
using System.Collections.Generic;

namespace BattleCore.Simulation
{
    /// <summary>
    /// 1ステップ分のシミュレーション実行コンテキスト。
    /// 全 ISimulationSystem に渡され、World・Time・CommandQueue への統一アクセス口となる。
    /// System同士が直接依存しないよう、このクラスを介してデータをやり取りする。
    /// </summary>
    public class SimulationContext
    {
        /// <summary>ゲーム内時間。Step ごとに Advance() される。</summary>
        public GameTime Time { get; }

        /// <summary>ゲーム世界の全状態。</summary>
        public WorldState World { get; }

        /// <summary>
        /// コマンドキュー。
        /// DecisionSystem が命令を Enqueue し、CommandExecutionSystem が Dequeue して実行する。
        /// 「考えるターン」と「実行するターン」を分離するための仕組み。
        /// </summary>
        public Queue<ICommand> CommandQueue { get; } = new();

        /// <summary>
        /// イベントキュー。
        /// LoyaltySystem などが発生させたイベントを積む。
        /// UIやログシステムが参照して演出・通知に使用する。
        /// </summary>
        public Queue<IGameEvent> EventQueue { get; } = new();

        public SimulationContext(WorldState world)
        {
            Time = new GameTime();
            World = world;
        }
    }
}
