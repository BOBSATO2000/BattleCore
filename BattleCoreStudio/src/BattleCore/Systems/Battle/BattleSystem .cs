using BattleCore.Battle;
using BattleCore.Simulation;

namespace BattleCore.Systems.Battle
{
    /// <summary>
    /// 戦闘システム。SimulationEngine に登録され毎Step実行される。
    /// 責務は「探す」「解決する」の2つを呼び出すだけ。
    /// 
    /// 設計（会話履歴 7.txt より）：
    ///   BattleFinder   - 同Hexにいる異なる勢力の軍ペアを探す
    ///   BattleResolver - 1件の戦闘を解決し兵力を更新する
    /// 
    /// この分離により：
    ///   - BattleFinder だけ変更すれば挟撃・援軍・多数対多数に対応できる
    ///   - BattleResolver だけ変更すれば士気・武将能力・地形・天候・夜戦・城攻めを追加できる
    /// </summary>
    public class BattleSystem : ISimulationSystem
    {
        private readonly BattleFinder finder = new();
        private readonly BattleResolver resolver = new();

        public void Update(SimulationContext context)
        {
            foreach (var battle in finder.Find(context.World))
            {
                var log = resolver.Resolve(battle, context.World);
                context.EventQueue.Enqueue(log);
            }
        }
    }
}
