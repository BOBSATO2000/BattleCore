using BattleCore.Simulation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCore.Commands
{
	public class CommandQueue
	{
		private readonly Queue<ICommand> commands = new();


		public void Add(ICommand command)
		{
			commands.Enqueue(command);
		}


		public void ExecuteAll(SimulationContext context)
		{
			while (commands.Count > 0)
			{
				var command = commands.Dequeue();
				command.Execute(context);
			}
		}
	}
}
