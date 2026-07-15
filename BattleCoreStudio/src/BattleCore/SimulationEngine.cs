namespace BattleCore;
using System.Collections.Generic;

public sealed class SimulationEngine
{
    private readonly List<ISimulationSystem> _systems=new();
    public SimulationContext Context { get; }=new();

    public void Register(ISimulationSystem system)=>_systems.Add(system);

    public void Enqueue(ICommand command)=>Context.CommandQueue.Enqueue(command);

    public void Step()
    {
        foreach(var system in _systems)
            system.Update(Context);
        Context.Tick++;
    }
}
