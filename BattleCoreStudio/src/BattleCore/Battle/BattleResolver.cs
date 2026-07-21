using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems.Battle;
using BattleCore.World;
using System;
using System.Linq;

namespace BattleCore.Battle
{
    /// <summary>
    /// 1件の戦闘を解決し、兵力更新・武将成長・イベント生成を行う。
    /// BattleSystem から呼び出され、BattleFinder が見つけた戦闘ペアを受け取る。
    /// </summary>
    public class BattleResolver
    {
        private readonly DamageCalculator calculator = new();

        /// <summary>Trustボーナスを適用する信頼度の閾値。</summary>
        public const int TrustBonusThreshold = 60;

        /// <summary>Trustボーナス適用時の勝者損害軽減係数。</summary>
        private const double TrustBonusFactor = 0.95;

        /// <summary>武将の能力値成長の上限。</summary>
        private const int GrowthCap = 200;

        /// <summary>
        /// 戦闘を解決し BattleLogEvent を返す。
        /// 地形・天気・城・Trust・武将成長を全て適用する。
        /// </summary>
        public BattleLogEvent Resolve(Battle battle, WorldState world)
        {
            var attackerOfficer = GetOfficer(battle.Attacker, world);
            var defenderOfficer = GetOfficer(battle.Defender, world);

            var defenderHex     = world.Map.GetHexById(battle.Defender.CurrentHexId);
            var defenderTerrain = defenderHex?.Terrain ?? TerrainType.Plain;
            var hasCastle       = world.Castles.Any(c => c.HexId == battle.Defender.CurrentHexId
                                                      && c.OwnerClanId == battle.Defender.ClanId);

            var result = calculator.Calculate(
                battle.Attacker, battle.Defender,
                attackerOfficer, defenderOfficer,
                defenderTerrain,
                world.Weather,
                hasCastle);

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

            // 勝者Officerの成長
            string? growthDetail = null;
            var winnerOff = result.Winner == battle.Attacker ? attackerOfficer : defenderOfficer;
            if (winnerOff != null)
            {
                winnerOff.BattleWins++;
                if (winnerOff.BattleWins % 2 == 1)
                {
                    winnerOff.Leadership = Math.Min(GrowthCap, winnerOff.Leadership + 1);
                    growthDetail = $"{winnerOff.Name} 統率+1({winnerOff.Leadership})";
                }
                else
                {
                    winnerOff.Strategy = Math.Min(GrowthCap, winnerOff.Strategy + 1);
                    growthDetail = $"{winnerOff.Name} 戦術+1({winnerOff.Strategy})";
                }
            }

            var winnerName = (result.Winner == battle.Attacker ? attackerOfficer : defenderOfficer)?.Name
                             ?? (result.Winner == battle.Attacker
                                 ? world.Clans.FirstOrDefault(c => c.Id == battle.Attacker.ClanId)?.Name
                                 : world.Clans.FirstOrDefault(c => c.Id == battle.Defender.ClanId)?.Name)
                             ?? "?";
            var loserName  = (result.Loser  == battle.Attacker ? attackerOfficer : defenderOfficer)?.Name
                             ?? (result.Loser == battle.Attacker
                                 ? world.Clans.FirstOrDefault(c => c.Id == battle.Attacker.ClanId)?.Name
                                 : world.Clans.FirstOrDefault(c => c.Id == battle.Defender.ClanId)?.Name)
                             ?? "?";

            return new BattleLogEvent(
                winnerName, loserName,
                winnerLosses, result.LoserLosses,
                defenderTerrain, world.Weather,
                terrainBonus: defenderTerrain != TerrainType.Plain,
                rainPenalty:  world.Weather == Weather.Rain,
                castleBonus:  hasCastle,
                growthDetail);
        }

        /// <summary>Army に配属された Officer を返す。未配属の場合は null。</summary>
        private static Officer? GetOfficer(Army army, WorldState world)
        {
            if (army.OfficerId == null) return null;
            return world.Officers.FirstOrDefault(o => o.Id == army.OfficerId);
        }
    }
}
