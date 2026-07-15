namespace BattleCore;
using System.Collections.Generic;

public sealed class SimulationContext
{
    public int Tick {get;set;}
    public World World {get;}=new();
    public Queue<ICommand> CommandQueue {get;}=new();
    public List<IGameEvent> Events {get;}=new();
}
