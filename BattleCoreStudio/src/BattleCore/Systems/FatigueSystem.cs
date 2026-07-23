using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 疲労システム。毎Tick実行。
    /// - 移動した軍（前Tickと現在のHexが異なる）は Fatigue +FatiguePerMove
    /// - 移動しなかった軍は Fatigue -FatigueRecovery（休息）
    /// - Fatigue が高いほど Morale にペナルティ（MoraleSystem と連携）
    /// - Fatigue は 0〜100 にクランプ
    /// </summary>
    public class FatigueSystem : ISimulationSystem
    {
        public const int FatiguePerMove   = 15;
        public const int FatigueRecovery  = 10;
        public const int MoralePenaltyThreshold = 50; // 疲労50以上でMoraleペナルティ

        public void Update(SimulationContext context)
        {
            var world = context.World;

            foreach (var army in world.Armies)
            {
                if (army.Soldiers == 0) continue;

                if (army.MovedThisTick)
                {
                    army.Fatigue = System.Math.Min(100, army.Fatigue + FatiguePerMove);
                }
                else
                {
                    army.Fatigue = System.Math.Max(0, army.Fatigue - FatigueRecovery);
                }

                // 疲労→士気ペナルティ
                if (army.Fatigue >= MoralePenaltyThreshold)
                {
                    int penalty = (army.Fatigue - MoralePenaltyThreshold) / 10; // 10疲労ごとに-1
                    army.Morale = System.Math.Max(0, army.Morale - penalty);
                }
            }
        }
    }
}
