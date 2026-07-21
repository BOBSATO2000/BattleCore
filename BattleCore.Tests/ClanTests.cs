using BattleCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCore.Tests
{
	[TestClass]
	public class ClanTests
	{
		[TestMethod]
		public void ClanCanOwnArmy()
		{
			var clan =
				new Clan(1);


			var army =
				new Army(10);


			clan.ArmyIds.Add(
				army.Id);


			Assert.HasCount(1, clan.ArmyIds);
		}
	}
}
