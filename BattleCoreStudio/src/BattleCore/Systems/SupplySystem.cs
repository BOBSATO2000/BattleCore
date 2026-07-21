using BattleCore.Events;
using BattleCore.Simulation;
using System;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 兵力補充システム。
    /// 毎Step実行され、勢力に所属する Army の兵力を回復させる。
    ///
    /// 補充ルール：
    ///   - ClanId=0（無所属）の Army は補充しない
    ///   - 兵力が MaxSoldiers に達したら補充しない
    ///   - 全滅した Army も毎Step少しずつ回復する（再建）
    ///   - 春は補充量が増える（農繁期後の兵集め）
    /// </summary>
    public class SupplySystem : ISimulationSystem
    {
        /// <summary>毎Stepの基本補充量。</summary>
        public int BaseReplenishment { get; }

        /// <summary>春の追加補充量。</summary>
        public int SpringBonus { get; }

        /// <summary>兵力の上限。</summary>
        public int MaxSoldiers { get; }

        /// <summary>SupplyEventを発火する補充量の閾値。微量補充はログに出さない。</summary>
        public int EventThreshold { get; }

        public SupplySystem(
            int baseReplenishment = 50,
            int springBonus       = 30,
            int maxSoldiers       = 1000,
            int eventThreshold    = 200)
        {
            BaseReplenishment = baseReplenishment;
            SpringBonus       = springBonus;
            MaxSoldiers       = maxSoldiers;
            EventThreshold    = eventThreshold;
        }

        public void Update(SimulationContext context)
        {
            var world = context.World;

            foreach (var army in world.Armies)
            {
                // 無所属・全滅は補充しない
                if (army.ClanId == 0 || army.Soldiers == 0) continue;

                // 上限に達していたらスキップ
                if (army.Soldiers >= army.MaxSoldiers) continue;

                var amount = BaseReplenishment;

                // 春ボーナス
                if (context.Time.Season == Season.Spring)
                    amount += SpringBonus;

                var newSoldiers = Math.Min(army.MaxSoldiers, army.Soldiers + amount);
                var gain        = newSoldiers - army.Soldiers;

                if (gain > 0)
                {
                    army.Reinforce(gain);

                    // 閾値以上の補充はイベントとして発火
                    if (gain >= EventThreshold)
                    {
                        var officer = army.OfficerId.HasValue
                            ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value)
                            : null;
                        var name = officer?.Name ?? $"軍{army.Id}";
                        context.EventQueue.Enqueue(
                            new SupplyEvent(army.Id, name, gain, army.Soldiers));
                    }
                }
            }
        }
    }
}
