using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 計略システム。毎Tick実行。
    /// 武将の StrategyPoint を毎Tick+1 回復する（MaxStrategyPoint まで）。
    /// 計略Orderの実行はCommandExecutionSystemが担当し、SPを消費する。
    /// </summary>
    public class StrategySystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            foreach (var officer in context.World.Officers)
            {
                if (officer.StrategyPoint < officer.MaxStrategyPoint)
                    officer.StrategyPoint++;
            }
        }
    }
}
