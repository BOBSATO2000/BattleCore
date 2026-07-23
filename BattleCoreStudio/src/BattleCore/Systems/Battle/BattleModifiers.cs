using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using System;

namespace BattleCore.Systems.Battle
{
    /// <summary>地形補正。Forest -20% / Mountain -30% 防御側損害軽減。</summary>
    public sealed class TerrainModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            double factor = ctx.DefenderTerrain switch
            {
                TerrainType.Forest   => 0.80,
                TerrainType.Mountain => 0.70,
                _                    => 1.00,
            };
            string label = ctx.DefenderTerrain switch
            {
                TerrainType.Forest   => "森林防御",
                TerrainType.Mountain => "山岳防御",
                _                    => "",
            };
            bd.Add(label, factor);
            return (winnerLosses, (int)(loserLosses * factor));
        }
    }

    /// <summary>天気補正。Rain時は双方の損害10%軽減。</summary>
    public sealed class WeatherModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            if (ctx.Weather != Weather.Rain) return (winnerLosses, loserLosses);
            bd.Add("雨天", 0.90);
            return ((int)(winnerLosses * 0.90), (int)(loserLosses * 0.90));
        }
    }

    /// <summary>城補正。防御側に城がある場合、敗者損害20%軽減。</summary>
    public sealed class CastleModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            if (!ctx.HasCastle) return (winnerLosses, loserLosses);
            bd.Add("城郭防御", 0.80);
            return (winnerLosses, (int)(loserLosses * 0.80));
        }
    }

    /// <summary>
    /// Facing補正。側面×1.2 / 背後×1.5。
    /// 騎馬突撃（Spear以外）×1.3。
    /// </summary>
    public sealed class FacingModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            var (factor, label) = GetFactorAndLabel(ctx);
            bd.Add(label, factor);
            return (winnerLosses, (int)(loserLosses * factor));
        }

        private static (double factor, string label) GetFactorAndLabel(BattleContext ctx)
        {
            if (ctx.Attacker.UnitType == UnitType.Cavalry)
            {
                double f = UnitTypeData.CavalryChargeFactor(ctx.Defender.UnitType);
                string l = ctx.Defender.UnitType == UnitType.Spear ? "" : "騎馬突撃";
                return (f, l);
            }

            int diff = Math.Abs((int)ctx.Defender.Facing - (int)ctx.Attacker.Facing) % 6;
            if (diff > 3) diff = 6 - diff;
            return diff switch
            {
                0 => (1.5, "背後攻撃"),
                1 => (1.2, "側面攻撃"),
                _ => (1.0, ""),
            };
        }
    }

    /// <summary>
    /// 鉄砲先制補正の記録用。
    /// 実際の先制計算は DamageCalculator の実効兵力計算時に行われるが、
    /// Breakdown への記録はここで行う。
    /// </summary>
    public sealed class ArquebusModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            if (ctx.Attacker.UnitType == UnitType.Arquebus)
                bd.Add("鉄砲先制", UnitTypeData.PreemptiveFactor(ctx.Attacker.UnitType));
            else if (ctx.Defender.UnitType == UnitType.Arquebus)
                bd.Add("鉄砲先制(防)", UnitTypeData.PreemptiveFactor(ctx.Defender.UnitType));
            return (winnerLosses, loserLosses);
        }
    }

    /// <summary>士気補正の記録用。実効兵力計算時に適用済み。</summary>
    public sealed class MoraleModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            double attackerMorale = ctx.Attacker.Morale / 100.0;
            if (Math.Abs(attackerMorale - 1.0) > 0.01)
                bd.Add($"士気({ctx.Attacker.Morale})", attackerMorale);
            return (winnerLosses, loserLosses);
        }
    }

    /// <summary>防御態勢補正。Defend態勢の防御側は被ダメ-20%。</summary>
    public sealed class DefendModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            if (ctx.DefenderStance != ArmyStance.Defend) return (winnerLosses, loserLosses);
            bd.Add("防御態勢", 0.80);
            return (winnerLosses, (int)(loserLosses * 0.80));
        }
    }

    /// <summary>奇襲補正。Ambush態勢の攻撃側がForest/Mountainにいる場合、先制+20%。</summary>
    public sealed class AmbushModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            if (ctx.AttackerStance != ArmyStance.Ambush) return (winnerLosses, loserLosses);
            bool inCover = ctx.DefenderTerrain == TerrainType.Forest
                        || ctx.DefenderTerrain == TerrainType.Mountain;
            if (!inCover) return (winnerLosses, loserLosses);
            bd.Add("奇襲先制", 1.20);
            return (winnerLosses, (int)(loserLosses * 1.20));
        }
    }

    /// <summary>迎撃補正。Intercept態勢の軍が攻撃側の場合、先制+10%。</summary>
    public sealed class InterceptModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            if (ctx.AttackerStance != ArmyStance.Intercept) return (winnerLosses, loserLosses);
            bd.Add("迎撃態勢", 1.10);
            return (winnerLosses, (int)(loserLosses * 1.10));
        }
    }

    /// <summary>
    /// 高度補正。攻撃側が高所にいる場合+20%、防御側が高所にいる場合-10%。
    /// 高度差が2以上の場合に適用。
    /// </summary>
    public sealed class HeightModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            int attackerHeight = ctx.AttackerHex?.Height ?? 0;
            int defenderHeight = ctx.DefenderHex?.Height ?? 0;
            int diff = attackerHeight - defenderHeight;

            if (diff >= 2)
            {
                bd.Add("高所攻撃", 1.20);
                return (winnerLosses, (int)(loserLosses * 1.20));
            }
            if (diff <= -2)
            {
                bd.Add("高所防御", 0.90);
                return (winnerLosses, (int)(loserLosses * 0.90));
            }
            return (winnerLosses, loserLosses);
        }
    }

    /// <summary>塹壕補正。Entrenched態勢の防御側は被ダメ追加-10%。</summary>
    public sealed class EntrenchModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            if (ctx.DefenderStance != ArmyStance.Entrenched) return (winnerLosses, loserLosses);
            bd.Add("塹壕防御", 0.90);
            return (winnerLosses, (int)(loserLosses * 0.90));
        }
    }

    /// <summary>籠城補正。Garrisoning態勢かつ城ありの防御側は追加-15%。</summary>
    public sealed class GarrisonModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            if (ctx.DefenderStance != ArmyStance.Garrisoning || !ctx.HasCastle)
                return (winnerLosses, loserLosses);
            bd.Add("籠城防御", 0.85);
            return (winnerLosses, (int)(loserLosses * 0.85));
        }
    }

    /// <summary>
    /// 疲労補正。疲労50以上の軍は戦闘力低下。
    /// 攻撃側疲労が高いほど敗者損害が減る（攻撃力低下）。
    /// 防御側疲労が高いほど敗者損害が増える（防御力低下）。
    /// </summary>
    public sealed class FatigueModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            if (ctx.Attacker.Fatigue >= FatigueSystem.MoralePenaltyThreshold)
            {
                double factor = 1.0 - (ctx.Attacker.Fatigue - FatigueSystem.MoralePenaltyThreshold) * 0.005;
                factor = Math.Max(0.7, factor);
                bd.Add($"攻撃疲労({ctx.Attacker.Fatigue})", factor);
                loserLosses = (int)(loserLosses * factor);
            }
            if (ctx.Defender.Fatigue >= FatigueSystem.MoralePenaltyThreshold)
            {
                double factor = 1.0 + (ctx.Defender.Fatigue - FatigueSystem.MoralePenaltyThreshold) * 0.005;
                factor = Math.Min(1.3, factor);
                bd.Add($"防御疲労({ctx.Defender.Fatigue})", factor);
                loserLosses = (int)(loserLosses * factor);
            }
            return (winnerLosses, loserLosses);
        }
    }

    /// <summary>
    /// 陣形補正。
    /// 横陣: 側面耐性+（側面被ダメ-15%）、正面弱（正面被ダメ+10%）。
    /// 縦陣: 前進速度+、側面弱（側面被ダメ+20%）。
    /// 鶴翼: 包囲攻撃+（正面攻撃+15%）、防御弱（被ダメ+10%）。
    /// 魚鳞: 正面突破+（正面攻撃+20%）、包囲弱（包囲時被ダメ+15%）。
    /// 方円: 全方位防御+（被ダメ-10%）、機動力-。
    /// </summary>
    public sealed class FormationModifier : IBattleModifier
    {
        public (int, int) Apply(BattleContext ctx, int winnerLosses, int loserLosses, BattleBreakdown bd)
        {
            // 攻撃側陣形による攻撃力補正
            double attackFactor = ctx.Attacker.Formation switch
            {
                ArmyFormation.Crane  => 1.15,
                ArmyFormation.Wedge  => 1.20,
                _                    => 1.00,
            };
            if (attackFactor != 1.0)
            {
                bd.Add($"陣形({ctx.Attacker.Formation})攻撃", attackFactor);
                loserLosses = (int)(loserLosses * attackFactor);
            }

            // 防御側陣形による防御力補正
            double defenseFactor = ctx.Defender.Formation switch
            {
                ArmyFormation.Circle => 0.90,
                ArmyFormation.Line   => 0.85, // 側面耐性（正面戦闘想定）
                ArmyFormation.Wedge  => 1.15, // 包囲弱
                ArmyFormation.Crane  => 1.10, // 防御弱
                _                    => 1.00,
            };
            if (defenseFactor != 1.0)
            {
                bd.Add($"陣形({ctx.Defender.Formation})防御", defenseFactor);
                loserLosses = (int)(loserLosses * defenseFactor);
            }

            return (winnerLosses, loserLosses);
        }
    }
}
