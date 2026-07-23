using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Simulation
{
    /// <summary>
    /// シミュレーションエンジンの中核。
    /// 登録された ISimulationSystem を順番に実行し、世界を1ステップ進める。
    /// UIは Step() を呼ぶだけでよく、ゲームロジックを知る必要がない。
    /// 
    /// 推奨登録順：
    ///   1.  CastleSystem          - 城の状態更新（補給・耐久）
    ///   2.  ClanDecisionSystem    - 勢力AIが命令を考える
    ///   3.  CommandExecutionSystem- 命令をArmyに適用する
    ///   4.  MovementSystem        - Armyを移動させる
    ///   5.  BattleSystem          - 同Hexの敵と戦闘する
    ///   6.  LoyaltySystem         - 武将の忠誠度を更新する
    ///   7.  RecruitmentSystem     - 武将の仕官・離反を処理する
    ///   8.  SupplySystem          - 補給・兵力回復を処理する
    ///   9.  RelationshipSystem    - 武将間の関係値を更新する
    ///   10. DiplomacySystem       - 同盟・外交を処理する
    ///   11. EventTriggerSystem    - シナリオイベントを発火する
    ///   12. VictorySystem         - 勝利条件を判定する
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

        /// <summary>システムを登録する。登録順が実行順になる。</summary>
        public void Register(ISimulationSystem system) => systems.Add(system);

        /// <summary>Register の別名（既存テストとの互換用）。</summary>
        public void RegisterSystem(ISimulationSystem system) => systems.Add(system);

        /// <summary>
        /// 世界を1ステップ進める。
        /// 全Systemを登録順に実行し、最後に GameTime を進める。
        /// </summary>
        public void Step()
        {
            // ターン開始時に全軍のAP・Stanceをリセット
            foreach (var army in Context.World.Armies)
            {
                army.ResetActionPoints();
                army.Stance        = army.Stance == BattleCore.Entities.ArmyStance.Entrenched
                    ? BattleCore.Entities.ArmyStance.Entrenched
                    : BattleCore.Entities.ArmyStance.Normal;
                army.ScoutingBonus    = false;
                army.Marching         = false;
                army.IsDecoy          = false;
                army.PendingAttackHexId = null;
            }

            foreach (var system in systems)
                system.Update(Context);

            Context.Time.Advance();
            Context.World.Weather = Context.Time.Weather;
        }

        /// <summary>
        /// 現在のフェーズに対応するSystemだけを実行し、次のフェーズへ進む。
        /// TurnPhase.Victory の後は GameTime を進めて PlayerPhase に戻る。
        /// 
        /// 使い方：
        ///   各フェーズのSystemを RegisterForPhase() で登録し、
        ///   UIのボタン押下などで StepPhase() を呼ぶ。
        /// </summary>
        public void StepPhase()
        {
            var phaseSystems = systems
                .OfType<IPhasedSystem>()
                .Where(s => s.Phase == Context.CurrentPhase)
                .Cast<ISimulationSystem>();

            // フェーズ対応Systemがなければ全System実行（後方互換）
            var toRun = phaseSystems.Any()
                ? phaseSystems
                : (IEnumerable<ISimulationSystem>)systems;

            foreach (var system in toRun)
                system.Update(Context);

            // フェーズを進める
            Context.CurrentPhase = Context.CurrentPhase == TurnPhase.Victory
                ? TurnPhase.PlayerPhase
                : Context.CurrentPhase + 1;

            // ターン終了時（Victory→PlayerPhase に戻る時）に時間を進める
            if (Context.CurrentPhase == TurnPhase.PlayerPhase)
            {
                foreach (var army in Context.World.Armies)
                    army.ResetActionPoints();
                Context.Time.Advance();
                Context.World.Weather = Context.Time.Weather;
            }
        }
    }
}
