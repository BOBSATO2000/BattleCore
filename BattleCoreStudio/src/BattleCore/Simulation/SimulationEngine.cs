using BattleCore.Events;
using BattleCore.Player;
using BattleCore.Systems;
using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Simulation
{
    /// <summary>
    /// シミュレーションエンジンの中核。
    ///
    /// プレイヤーなし（観戦モード）：
    ///   Step() → 全System実行 → 時間進める
    ///
    /// プレイヤーあり：
    ///   Step() 呼び出し
    ///     → WaitingForPlayer=true、PlayerInputRequiredEvent 発行、中断
    ///   UI が命令入力
    ///   ConfirmPlayerInput() 呼び出し
    ///     → WaitingForPlayer=false、残りSystem実行、時間進める
    /// </summary>
    public class SimulationEngine
    {
        private readonly List<ISimulationSystem> systems = new();

        /// <summary>現在のシミュレーションコンテキスト。UIからの参照用。</summary>
        public SimulationContext Context { get; }

        /// <summary>
        /// プレイヤー入力待ち状態かどうか。
        /// true の間は Step() を呼んでも何もしない。
        /// ConfirmPlayerInput() で false に戻る。
        /// </summary>
        public bool WaitingForPlayer { get; private set; } = false;

        /// <summary>プレイヤー勢力が登録されているか（観戦モード判定用）。</summary>
        private bool _hasPlayerCommander = false;

        public SimulationEngine(WorldState world)
        {
            Context = new SimulationContext(world);
        }

        public SimulationEngine(SimulationContext context)
        {
            Context = context;
        }

        public void Register(ISimulationSystem system)
        {
            systems.Add(system);
            // PlayerCommander を持つ CommanderSystem が登録されたら記録
            if (system is CommanderSystem cs && cs.HasPlayerCommander)
                _hasPlayerCommander = true;
        }

        public void RegisterSystem(ISimulationSystem system) => Register(system);

        /// <summary>
        /// 世界を1ステップ進める。
        ///
        /// プレイヤーあり：
        ///   初回呼び出し → AP/Stanceリセット → PlayerInputRequiredEvent 発行 → WaitingForPlayer=true で中断
        ///   ConfirmPlayerInput() 後 → 全System実行 → 時間進める
        ///
        /// 観戦モード：
        ///   通常通り全System実行 → 時間進める
        /// </summary>
        public void Step()
        {
            if (WaitingForPlayer) return;

            // AP・Stanceリセット
            foreach (var army in Context.World.Armies)
            {
                army.ResetActionPoints();
                army.Stance = army.Stance == BattleCore.Entities.ArmyStance.Entrenched
                    ? BattleCore.Entities.ArmyStance.Entrenched
                    : BattleCore.Entities.ArmyStance.Normal;
                army.ScoutingBonus      = false;
                army.Marching           = false;
                army.IsDecoy            = false;
                army.PendingAttackHexId = null;
            }

            if (_hasPlayerCommander)
            {
                // プレイヤー入力待ちへ移行
                var playerClan = Context.World.Clans.FirstOrDefault(c => c.IsPlayerControlled);
                Context.EventQueue.Enqueue(
                    new PlayerInputRequiredEvent(playerClan?.Id ?? 0, Context.Time.Tick));
                WaitingForPlayer = true;
                return;
            }

            RunSystems();
        }

        /// <summary>
        /// プレイヤーが命令入力を確定した後に呼ぶ。
        /// WaitingForPlayer を false にして残りのSystem実行・時間進めを行う。
        /// </summary>
        public void ConfirmPlayerInput()
        {
            if (!WaitingForPlayer) return;
            WaitingForPlayer = false;
            RunSystems();
        }

        private void RunSystems()
        {
            foreach (var system in systems)
                system.Update(Context);

            Context.Time.Advance();
            Context.World.Weather = Context.Time.Weather;
        }

        // StepPhase は既存テスト互換のため残す
        public void StepPhase()
        {
            var phaseSystems = systems
                .OfType<IPhasedSystem>()
                .Where(s => s.Phase == Context.CurrentPhase)
                .Cast<ISimulationSystem>();

            var toRun = phaseSystems.Any()
                ? phaseSystems
                : (IEnumerable<ISimulationSystem>)systems;

            foreach (var system in toRun)
                system.Update(Context);

            Context.CurrentPhase = Context.CurrentPhase == TurnPhase.Victory
                ? TurnPhase.PlayerPhase
                : Context.CurrentPhase + 1;

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
