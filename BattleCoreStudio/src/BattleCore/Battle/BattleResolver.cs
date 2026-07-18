using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Systems.Battle;
using BattleCore.World;
using System;
using System.Linq;

namespace BattleCore.Battle
{
    /// <summary>
    /// 1件の戦闘を解決し、兵力を更新する。
    /// Officer が配属されている場合は Leadership / Strategy / Courage を DamageCalculator へ渡す。
    /// 攻撃側・防御側のOfficer間のTrustが高い場合（>=60）、勝者損害を5%軽減する。
    /// </summary>
    public class BattleResolver
    {
        private readonly DamageCalculator calculator = new();

        public const int TrustBonusThreshold = 60;
        private const double TrustBonusFactor = 0.95;
        private const int GrowthCap = 200;

        public void Resolve(Battle battle, WorldState world)
        {
            var attackerOfficer = GetOfficer(battle.Attacker, world);
            var defenderOfficer = GetOfficer(battle.Defender, world);

            var defenderHex = world.Map.GetHexById(battle.Defender.CurrentHexId);
            var defenderTerrain = defenderHex?.Terrain ?? TerrainType.Plain;

            var result = calculator.Calculate(
                battle.Attacker, battle.Defender,
                attackerOfficer, defenderOfficer,
                defenderTerrain,
                world.Weather);

            var winnerLosses = result.WinnerLosses;

            // Trust>=60 の場合、勝者損害を5%軽減
            if (attackerOfficer != null && defenderOfficer != null)
            {
                var winnerOfficer = result.Winner == battle.Attacker ? attackerOfficer : defenderOfficer;
                var loserOfficer  = result.Winner == battle.Attacker ? defenderOfficer : attackerOfficer;
                var rel = world.Relationships.FirstOrDefault(
                    r => r.FromOfficerId == winnerOfficer.Id && r.ToOfficerId == loserOfficer.Id);
                if (rel != null && rel.Trust >= TrustBonusThreshold)
                    winnerLosses = (int)(winnerLosses * TrustBonusFactor);
            }

            battle.Attacker.LoseSoldiers(
                battle.Attacker == result.Winner ? winnerLosses : result.LoserLosses);

            battle.Defender.LoseSoldiers(
                battle.Defender == result.Winner ? winnerLosses : result.LoserLosses);

            // 勝者Officerの成長：勝利毎に Leadership / Strategy を交互に+1（上限200）
            var winner = result.Winner == battle.Attacker ? attackerOfficer : defenderOfficer;
            if (winner != null)
            {
                winner.BattleWins++;
                if (winner.BattleWins % 2 == 1)
                    winner.Leadership = Math.Min(GrowthCap, winner.Leadership + 1);
                else
                    winner.Strategy   = Math.Min(GrowthCap, winner.Strategy   + 1);
            }
        }

        private static Officer? GetOfficer(Army army, WorldState world)
        {
            if (army.OfficerId == null) return null;
            return world.Officers.FirstOrDefault(o => o.Id == army.OfficerId);
        }
    }
}
