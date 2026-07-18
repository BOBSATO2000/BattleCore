using BattleCore.Entities;
using BattleCore.Systems.Battle;
using BattleCore.World;
using System.Linq;

namespace BattleCore.Battle
{
    /// <summary>
    /// 1件の戦闘を解決し、兵力を更新する。
    /// BattleSystem から利用され、「どう戦うか」のルールを担当する。
    /// Officer が配属されている場合は Leadership / Strategy / Courage を DamageCalculator へ渡す。
    /// </summary>
    public class BattleResolver
    {
        private readonly DamageCalculator calculator = new();

        /// <summary>
        /// 戦闘を解決する。WorldState から指揮官を取得し、Officer補正込みでダメージを計算する。
        /// </summary>
        public void Resolve(Battle battle, WorldState world)
        {
            var attackerOfficer = GetOfficer(battle.Attacker, world);
            var defenderOfficer = GetOfficer(battle.Defender, world);

            var result = calculator.Calculate(
                battle.Attacker, battle.Defender,
                attackerOfficer, defenderOfficer);

            battle.Attacker.LoseSoldiers(
                battle.Attacker == result.Winner ? result.WinnerLosses : result.LoserLosses);

            battle.Defender.LoseSoldiers(
                battle.Defender == result.Winner ? result.WinnerLosses : result.LoserLosses);
        }

        private static Officer? GetOfficer(Army army, WorldState world)
        {
            if (army.OfficerId == null) return null;
            return world.Officers.FirstOrDefault(o => o.Id == army.OfficerId);
        }
    }
}
