using BattleCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BattleCore.Tests
{
	[TestClass]
	public class OfficerTests
	{
		[TestMethod]
		public void ArmyCanHaveOfficer()
		{
			var army =
				new Army
				{
					Id = 1
				};


			army.AssignOfficer(10);


			Assert.AreEqual(
				10,
				army.OfficerId);
		}
	}
}
