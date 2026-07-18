using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Relations;
using BattleCore.Simulation;
using System;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 無所属武将の再仕官システム。
    /// 毎Step実行され、ClanId=0（無所属）の Army を指揮する Officer を
    /// 最も近い勢力へ仕官させる。
    ///
    /// 仕官ルール：
    ///   1. 無所属（ClanId=0）の Army を指揮する Officer を探す
    ///   2. 生存している Army を持つ勢力の中で最も近い勢力を探す
    ///   3. Officer.Ambition が高いほど仕官しやすい（低Ambitionは慎重）
    ///   4. 仕官すると Membership 追加・Army の ClanId 変更・RecruitEvent 発生
    /// </summary>
    public class RecruitmentSystem : ISimulationSystem
    {
        /// <summary>仕官判定の基準Ambition。これ以上なら必ず仕官する。</summary>
        public int RecruitAmbitionThreshold { get; }

        private static int _nextMembershipId = 100;

        public RecruitmentSystem(int recruitAmbitionThreshold = 30)
        {
            RecruitAmbitionThreshold = recruitAmbitionThreshold;
        }

        public void Update(SimulationContext context)
        {
            var world = context.World;

            // 無所属Armyを指揮するOfficerを取得
            var ronins = world.Armies
                .Where(a => a.ClanId == 0 && a.Soldiers > 0 && a.OfficerId.HasValue)
                .Select(a => new
                {
                    Army    = a,
                    Officer = world.Officers.FirstOrDefault(o => o.Id == a.OfficerId)
                })
                .Where(x => x.Officer != null)
                .ToList();

            foreach (var ronin in ronins)
            {
                // Ambitionが低い武将は仕官を急がない
                if (ronin.Officer!.Ambition < RecruitAmbitionThreshold)
                    continue;

                // 生存Armyを持つ勢力を探す
                var activeClanIds = world.Armies
                    .Where(a => a.Soldiers > 0 && a.ClanId != 0)
                    .Select(a => a.ClanId)
                    .Distinct()
                    .ToList();

                if (!activeClanIds.Any()) continue;

                // 最も近い勢力のArmyを探す
                var nearest = world.Armies
                    .Where(a => activeClanIds.Contains(a.ClanId) && a.Soldiers > 0)
                    .Select(a => new
                    {
                        Army     = a,
                        Distance = Math.Abs(a.CurrentHexId - ronin.Army.CurrentHexId)
                    })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                if (nearest == null) continue;

                var targetClanId = nearest.Army.ClanId;

                // 仕官処理
                var membership = new Membership(
                    _nextMembershipId++,
                    ronin.Officer.Id,
                    targetClanId)
                {
                    Loyalty = 50
                };

                world.Memberships.Add(membership);
                ronin.Army.Defect(targetClanId);

                context.EventQueue.Enqueue(
                    new RecruitEvent(ronin.Officer.Id, targetClanId));
            }
        }
    }
}
