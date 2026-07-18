using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 外交システム。
    /// 1. 毎Step同盟の残りTickを減らし、期限切れで解消する。
    /// 2. 共通の敵を持つ2勢力が一定間隔で自動的に同盟を結ぶ（AI外交）。
    /// </summary>
    public class DiplomacySystem : ISimulationSystem
    {
        /// <summary>AI自動同盟を試みる間隔（Tick）。</summary>
        public int AutoAllianceInterval { get; }

        /// <summary>AI自動同盟の期間（Tick）。</summary>
        public int AutoAllianceDuration { get; }

        public DiplomacySystem(int autoAllianceInterval = 10, int autoAllianceDuration = 15)
        {
            AutoAllianceInterval = autoAllianceInterval;
            AutoAllianceDuration = autoAllianceDuration;
        }

        public void Update(SimulationContext context)
        {
            // 1. 同盟期限チェック
            var expired = context.World.Alliances
                .Where(a => a.RemainingTicks > 0)
                .ToList();

            foreach (var alliance in expired)
            {
                alliance.RemainingTicks--;
                if (alliance.RemainingTicks <= 0)
                {
                    var c1 = context.World.Clans.FirstOrDefault(c => c.Id == alliance.ClanId1);
                    var c2 = context.World.Clans.FirstOrDefault(c => c.Id == alliance.ClanId2);
                    context.EventQueue.Enqueue(new ScenarioEvent(
                        "alliance_expired",
                        $"【同盟解消】{c1?.Name ?? "?"}と{c2?.Name ?? "?"}の同盟が期限切れとなった。"));
                }
            }
            context.World.Alliances.RemoveAll(a => a.RemainingTicks <= 0);

            // 2. AI自動同盟（指定間隔ごとに評価）
            if (AutoAllianceInterval <= 0) return;
            if (context.Time.Tick % AutoAllianceInterval != 0) return;

            var activeClans = context.World.Armies
                .Where(a => a.Soldiers > 0 && a.ClanId != 0)
                .Select(a => a.ClanId)
                .Distinct()
                .ToList();

            if (activeClans.Count < 3) return; // 3勢力以上いないと同盟の意味がない

            var nextId = context.World.Alliances.Count + 1;

            for (int i = 0; i < activeClans.Count; i++)
            for (int j = i + 1; j < activeClans.Count; j++)
            {
                var clanA = activeClans[i];
                var clanB = activeClans[j];

                // 既に同盟中はスキップ
                if (context.World.AreAllied(clanA, clanB)) continue;

                // 共通の敵（両方と戦っている勢力）が存在するか
                var commonEnemy = activeClans
                    .Where(c => c != clanA && c != clanB
                             && !context.World.AreAllied(clanA, c)
                             && !context.World.AreAllied(clanB, c))
                    .Any();

                if (!commonEnemy) continue;

                // 両勢力の総兵力差が大きい場合（弱い方が強い方に同盟を求める）
                var soldiersA = context.World.Armies
                    .Where(a => a.ClanId == clanA).Sum(a => a.Soldiers);
                var soldiersB = context.World.Armies
                    .Where(a => a.ClanId == clanB).Sum(a => a.Soldiers);
                var ratio = soldiersA == 0 ? 0 : (double)soldiersB / soldiersA;

                // 兵力比が0.4〜2.5の範囲（極端な差がない）なら同盟成立
                if (ratio < 0.4 || ratio > 2.5) continue;

                context.World.Alliances.Add(
                    new Alliance(nextId++, clanA, clanB, AutoAllianceDuration));

                var nameA = context.World.Clans.FirstOrDefault(c => c.Id == clanA)?.Name ?? "?";
                var nameB = context.World.Clans.FirstOrDefault(c => c.Id == clanB)?.Name ?? "?";
                context.EventQueue.Enqueue(new ScenarioEvent(
                    "alliance_formed",
                    $"【同盟締結】{nameA}と{nameB}が同盟を結んだ！（{AutoAllianceDuration}ターン）"));
            }
        }
    }
}
