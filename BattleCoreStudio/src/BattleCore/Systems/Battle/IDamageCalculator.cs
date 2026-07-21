using BattleCore.Entities;

namespace BattleCore.Systems.Battle
{
	/// <summary>
	/// 戦闘ダメージ計算のインターフェース。
	/// DamageCalculator をテストでモックに差し替える際に使用する。
	/// </summary>
	public interface IDamageCalculator
	{
		/// <summary>地形・天気・武将能力値なしの簡易計算。</summary>
		BattleResult Calculate(
			Army attacker,
			Army defender);
	}
}
