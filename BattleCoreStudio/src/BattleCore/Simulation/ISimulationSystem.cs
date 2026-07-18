using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCore.Simulation
{
    public interface ISimulationSystem
    {
        void Update(SimulationContext context);
    }
}
