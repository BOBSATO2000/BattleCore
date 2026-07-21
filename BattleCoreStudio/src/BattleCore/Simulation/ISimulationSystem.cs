using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCore.Simulation
{
    /// <summary>
    /// シミュレーションシステムの共通インターフェース。
    /// SimulationEngine に登録され、毎 Step に Update() が呼ばれる。
    /// 登録順が実行順になるため、登録順序に注意すること。
    /// </summary>
    public interface ISimulationSystem
    {
        /// <summary>シミュレーションを 1 ステップ進める。SimulationContext 経由で World を参照・変更する。</summary>
        void Update(SimulationContext context);
    }
}
