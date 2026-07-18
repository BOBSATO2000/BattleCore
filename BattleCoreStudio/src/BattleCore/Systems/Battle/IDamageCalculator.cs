using BattleCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCore.Systems.Battle
{
	public interface IDamageCalculator
	{
		BattleResult Calculate(
			Army attacker,
			Army defender);
	}
}
