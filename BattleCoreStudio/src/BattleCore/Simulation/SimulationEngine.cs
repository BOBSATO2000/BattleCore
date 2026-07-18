using BattleCore.World;
using System.Collections.Generic;

namespace BattleCore.Simulation
{
    /// <summary>
    /// シミュレーションエンジンの中核。
    /// 登録された ISimulationSystem を順番に実行し、世界を1ステップ進める。
    /// UIは Step() を呼ぶだけでよく、ゲームロジックを知る必要がない。
    /// 
    /// 推奨登録順（会話履歴 7.txt より）：
    ///   1. DecisionSystem   - AIが命令を考える
    ///   2. CommandExecutionSystem - 命令をArmyに適用する
    ///   3. MovementSystem   - Armyを移動させる
    ///   4. BattleSystem     - 同Hexの敵と戦闘する
    /// </summary>
    public class SimulationEngine
    {
        private readonly List<ISimulationSystem> systems = new();

        /// <summary>現在のシミュレーションコンテキスト。UIからの参照用。</summary>
        public SimulationContext Context { get; }

        /// <summary>WorldState からエンジンを生成する。</summary>
        public SimulationEngine(WorldState world)
        {
            Context = new SimulationContext(world);
        }

        /// <summary>既存の SimulationContext からエンジンを生成する（テスト用途）。</summary>
        public SimulationEngine(SimulationContext context)
        {
            Context = context;
        }

        /// <summary>システムを登録する（Register の別名。既存テストとの互換用）。</summary>
        public void RegisterSystem(ISimulationSystem system) => systems.Add(system);

        /// <summary>システムを登録する。登録順が実行順になる。</summary>
        public void Register(ISimulationSystem system) => systems.Add(system);

        /// <summary>
        /// 世界を1ステップ進める。
        /// 全Systemを登録順に実行し、最後に GameTime を進める。
        /// </summary>
        public void Step()
        {
            foreach (var system in systems)
                system.Update(Context);

            Context.Time.Advance();
            Context.World.Weather = Context.Time.Weather;
        }
    }
}
