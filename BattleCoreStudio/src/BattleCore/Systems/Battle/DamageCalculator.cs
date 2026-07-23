using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using System;
using System.Collections.Generic;

namespace BattleCore.Systems.Battle
{
    /// <summary>
    /// 戦闘ダメージを計算するクラス。
    /// BattleContext を受け取り、登録された IBattleModifier を順番に適用する。
    /// 各 Modifier は BattleBreakdown に補正内容を記録する。
    /// </summary>
    public sealed class DamageCalculator : IDamageCalculator
    {
        private const int DefaultStat = 100;

        private readonly IReadOnlyList<IBattleModifier> modifiers = new List<IBattleModifier>
        {
            new ArquebusModifier(),
            new MoraleModifier(),
            new TerrainModifier(),
            new WeatherModifier(),
            new CastleModifier(),
            new FacingModifier(),
            new DefendModifier(),
            new AmbushModifier(),
            new InterceptModifier(),
            new HeightModifier(),
            new EntrenchModifier(),
            new GarrisonModifier(),
            new FatigueModifier(),
            new FormationModifier(),
        };

        public BattleResult Calculate(Army left, Army right)
            => Calculate(new BattleContext(left, right, null, null, TerrainType.Plain, Weather.Sunny, false));

        public BattleResult Calculate(Army left, Army right, Officer? leftOfficer, Officer? rightOfficer)
            => Calculate(new BattleContext(left, right, leftOfficer, rightOfficer, TerrainType.Plain, Weather.Sunny, false));

        public BattleResult Calculate(Army left, Army right, Officer? leftOfficer, Officer? rightOfficer, TerrainType defenderTerrain)
            => Calculate(new BattleContext(left, right, leftOfficer, rightOfficer, defenderTerrain, Weather.Sunny, false));

        public BattleResult Calculate(Army left, Army right, Officer? leftOfficer, Officer? rightOfficer, TerrainType defenderTerrain, Weather weather, bool hasCastle = false)
            => Calculate(new BattleContext(left, right, leftOfficer, rightOfficer, defenderTerrain, weather, hasCastle));

        public BattleResult Calculate(BattleContext ctx)
        {
            var breakdown = new BattleBreakdown();

            // 実効兵力 = 兵力 × Leadership補正 × Morale補正
            var leftPower  = ApplyLeadership(ctx.Attacker.Soldiers, ctx.AttackerOfficer) * (ctx.Attacker.Morale / 100.0);
            var rightPower = ApplyLeadership(ctx.Defender.Soldiers, ctx.DefenderOfficer) * (ctx.Defender.Morale / 100.0);

            // 鉄砲先制：相手の実効兵力を削る
            leftPower  *= UnitTypeData.PreemptiveFactor(ctx.Defender.UnitType);
            rightPower *= UnitTypeData.PreemptiveFactor(ctx.Attacker.UnitType);

            Army winner, loser;
            Officer? winnerOfficer;
            int loserSoldiers;

            if (leftPower >= rightPower)
            {
                winner = ctx.Attacker; winnerOfficer = ctx.AttackerOfficer;
                loser  = ctx.Defender; loserSoldiers = ctx.Defender.Soldiers;
            }
            else
            {
                winner = ctx.Defender; winnerOfficer = ctx.DefenderOfficer;
                loser  = ctx.Attacker; loserSoldiers = ctx.Attacker.Soldiers;
            }

            var winnerLosses = (int)(loserSoldiers / 3.0
                * GetStrategyFactor(winnerOfficer)
                * GetCourageFactor(winnerOfficer));
            winnerLosses = Math.Max(0, winnerLosses);

            foreach (var modifier in modifiers)
                (winnerLosses, loserSoldiers) = modifier.Apply(ctx, winnerLosses, loserSoldiers, breakdown);

            return new BattleResult(winner, loser, winnerLosses, loserSoldiers, breakdown);
        }

        private static double ApplyLeadership(int soldiers, Officer? officer)
        {
            var factor = Math.Clamp((officer?.Leadership ?? DefaultStat) / 100.0, 0.5, 1.5);
            return soldiers * factor;
        }

        private static double GetStrategyFactor(Officer? officer)
            => Math.Clamp((officer?.Strategy ?? DefaultStat) / 100.0, 0.7, 1.3);

        private static double GetCourageFactor(Officer? officer)
            => Math.Clamp((officer?.Courage ?? DefaultStat) / 100.0, 0.9, 1.1);
    }
}
