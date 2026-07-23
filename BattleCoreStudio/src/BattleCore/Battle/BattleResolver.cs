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
    /// 1件の戦闘を解決し、兵力更新・士気変化・武将成長・BattleLogEvent生成を行う。
    /// BattleResult.Breakdown を BattleLogEvent に渡すことで、
    /// プレイヤーが「なぜそうなったか」をログで確認できる。
    /// </summary>
    public class BattleResolver
    {
        private readonly DamageCalculator calculator = new();

        public const int TrustBonusThreshold = 60;
        private const double TrustBonusFactor = 0.95;
        private const int GrowthCap = 200;

        public BattleLogEvent Resolve(Battle battle, WorldState world)
        {
            var attackerOfficer = GetOfficer(battle.Attacker, world);
            var defenderOfficer = GetOfficer(battle.Defender, world);

            var defenderHex     = world.Map.GetHexById(battle.Defender.CurrentHexId);
            var defenderTerrain = defenderHex?.Terrain ?? TerrainType.Plain;
            var hasCastle       = world.Castles.Any(c =>
                c.HexId == battle.Defender.CurrentHexId && c.OwnerClanId == battle.Defender.ClanId);

            var result = calculator.Calculate(new BattleContext(
                battle.Attacker, battle.Defender,
                attackerOfficer, defenderOfficer,
                defenderTerrain, world.Weather, hasCastle,
                world.Map.GetHexById(battle.Attacker.CurrentHexId),
                defenderHex));

            var winnerLosses = result.WinnerLosses;

            // Trust>=60 の場合、勝者損害を5%軽減
            if (attackerOfficer != null && defenderOfficer != null)
            {
                var winnerOfficer = result.Winner == battle.Attacker ? attackerOfficer : defenderOfficer;
                var loserOfficer  = result.Winner == battle.Attacker ? defenderOfficer : attackerOfficer;
                var rel = world.Relationships.FirstOrDefault(
                    r => r.FromOfficerId == winnerOfficer.Id && r.ToOfficerId == loserOfficer.Id);
                if (rel != null && rel.Trust >= TrustBonusThreshold)
                {
                    winnerLosses = (int)(winnerLosses * TrustBonusFactor);
                    result.Breakdown.Add("信頼補正", TrustBonusFactor);
                }
            }

            battle.Attacker.LoseSoldiers(battle.Attacker == result.Winner ? winnerLosses : result.LoserLosses);
            battle.Defender.LoseSoldiers(battle.Defender == result.Winner ? winnerLosses : result.LoserLosses);

            result.Loser.Morale  = Math.Max(0,   result.Loser.Morale  - 15);
            result.Winner.Morale = Math.Min(100, result.Winner.Morale + 5);

            // 隔接戦闘：勝者が敵Hexを占領、敗者は撤退を試みる
            if (battle.IsAdjacentBattle)
            {
                if (result.Winner == battle.Attacker)
                {
                    int targetHex = battle.Attacker.PendingAttackHexId ?? battle.Defender.CurrentHexId;
                    battle.Attacker.MoveTo(targetHex);
                }
                if (result.Loser == battle.Defender && result.Loser.Soldiers > 0)
                {
                    var retreatCastle = world.Castles
                        .Where(c => c.OwnerClanId == result.Loser.ClanId)
                        .OrderBy(c =>
                        {
                            var h = world.Map.GetHexById(result.Loser.CurrentHexId);
                            var ch = world.Map.GetHexById(c.HexId);
                            return (h == null || ch == null) ? int.MaxValue
                                : HexDistance.Calculate(h, ch);
                        })
                        .FirstOrDefault();
                    if (retreatCastle != null)
                        result.Loser.OrderMove(retreatCastle.HexId);
                }
            }

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

            var winnerName = GetName(result.Winner, battle, attackerOfficer, defenderOfficer, world);
            var loserName  = GetName(result.Loser,  battle, attackerOfficer, defenderOfficer, world);

            return new BattleLogEvent(winnerName, loserName, winnerLosses, result.LoserLosses,
                result.Breakdown, growthDetail);
        }

        private static string GetName(Army army, Battle battle, Officer? attackerOfficer, Officer? defenderOfficer, WorldState world)
        {
            var officer = army == battle.Attacker ? attackerOfficer : defenderOfficer;
            return officer?.Name
                ?? world.Clans.FirstOrDefault(c => c.Id == army.ClanId)?.Name
                ?? "?";
        }

        private static Officer? GetOfficer(Army army, WorldState world)
        {
            if (army.OfficerId == null) return null;
            return world.Officers.FirstOrDefault(o => o.Id == army.OfficerId);
        }
    }
}
