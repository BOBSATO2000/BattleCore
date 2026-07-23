using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 外交システム。
    /// 1. 同盟・停戦の期限管理
    /// 2. AI自動同盟（共通の敵・兵力比）
    /// 3. AI停戦申し入れ（兵力が劣勢の勢力が申し入れ）
    /// 4. 援軍要請（同盟国が包囲されたら援軍派遣命令）
    /// 5. 同盟裏切り（野心的な勢力が優勢になると同盟を破棄）
    /// </summary>
    public class DiplomacySystem : ISimulationSystem
    {
        public int AutoAllianceInterval { get; }
        public int AutoAllianceDuration { get; }
        public int CeasefireDuration    { get; }

        public DiplomacySystem(
            int autoAllianceInterval = 10,
            int autoAllianceDuration = 15,
            int ceasefireDuration    = 8)
        {
            AutoAllianceInterval = autoAllianceInterval;
            AutoAllianceDuration = autoAllianceDuration;
            CeasefireDuration    = ceasefireDuration;
        }

        public void Update(SimulationContext context)
        {
            TickAlliances(context);
            TickCeasefires(context);

            if (AutoAllianceInterval <= 0) return;
            if (context.Time.Tick % AutoAllianceInterval != 0) return;

            TryAutoAlliance(context);
            TryCeasefire(context);
            TryReinforcement(context);
            TryBetrayal(context);
        }

        // ── 同盟期限 ──────────────────────────────────────────────
        private static void TickAlliances(SimulationContext context)
        {
            var world = context.World;
            foreach (var a in world.Alliances.Where(a => a.RemainingTicks > 0).ToList())
            {
                a.RemainingTicks--;
                if (a.RemainingTicks > 0) continue;
                var c1 = world.Clans.FirstOrDefault(c => c.Id == a.ClanId1)?.Name ?? "?";
                var c2 = world.Clans.FirstOrDefault(c => c.Id == a.ClanId2)?.Name ?? "?";
                context.EventQueue.Enqueue(new ScenarioEvent("alliance_expired",
                    $"【同盟解消】{c1}と{c2}の同盟が期限切れとなった。"));
            }
            world.Alliances.RemoveAll(a => a.RemainingTicks <= 0);
        }

        // ── 停戦期限 ──────────────────────────────────────────────
        private static void TickCeasefires(SimulationContext context)
        {
            var world = context.World;
            foreach (var c in world.Ceasefires.Where(c => c.RemainingTicks > 0).ToList())
            {
                c.RemainingTicks--;
                if (c.RemainingTicks > 0) continue;
                var n1 = world.Clans.FirstOrDefault(x => x.Id == c.ClanId1)?.Name ?? "?";
                var n2 = world.Clans.FirstOrDefault(x => x.Id == c.ClanId2)?.Name ?? "?";
                context.EventQueue.Enqueue(new DiplomacyEvent(
                    DiplomacyEventType.CeasefireExpired, n1, n2));
            }
            world.Ceasefires.RemoveAll(c => c.RemainingTicks <= 0);
        }

        // ── AI自動同盟 ────────────────────────────────────────────
        private void TryAutoAlliance(SimulationContext context)
        {
            var world       = context.World;
            var activeClans = ActiveClanIds(world);
            if (activeClans.Count < 3) return;

            var nextId = NextId(world.Alliances);

            for (int i = 0; i < activeClans.Count; i++)
            for (int j = i + 1; j < activeClans.Count; j++)
            {
                var a = activeClans[i];
                var b = activeClans[j];
                if (world.AreAllied(a, b)) continue;

                bool commonEnemy = activeClans.Any(c =>
                    c != a && c != b &&
                    !world.AreAllied(a, c) && !world.AreAllied(b, c));
                if (!commonEnemy) continue;

                var ratio = SoldierRatio(a, b, world);
                if (ratio < 0.4 || ratio > 2.5) continue;

                world.Alliances.Add(new Alliance(nextId++, a, b, AutoAllianceDuration));
                var na = world.Clans.FirstOrDefault(c => c.Id == a)?.Name ?? "?";
                var nb = world.Clans.FirstOrDefault(c => c.Id == b)?.Name ?? "?";
                context.EventQueue.Enqueue(new ScenarioEvent("alliance_formed",
                    $"【同盟締結】{na}と{nb}が同盟を結んだ！（{AutoAllianceDuration}ターン）"));
            }
        }

        // ── AI停戦申し入れ ────────────────────────────────────────
        // 兵力が敵の40%未満の劣勢勢力が停戦を申し入れる
        private void TryCeasefire(SimulationContext context)
        {
            var world       = context.World;
            var activeClans = ActiveClanIds(world);
            var nextId      = NextId(world.Ceasefires);

            foreach (var weakId in activeClans)
            {
                var weakSoldiers = TotalSoldiers(weakId, world);

                foreach (var strongId in activeClans.Where(c => c != weakId))
                {
                    if (world.AreAllied(weakId, strongId)) continue;
                    if (world.IsInCeasefire(weakId, strongId)) continue;

                    var strongSoldiers = TotalSoldiers(strongId, world);
                    if (strongSoldiers == 0) continue;
                    if ((double)weakSoldiers / strongSoldiers > 0.4) continue;

                    world.Ceasefires.Add(new Ceasefire(nextId++, weakId, strongId, CeasefireDuration));
                    var nw = world.Clans.FirstOrDefault(c => c.Id == weakId)?.Name  ?? "?";
                    var ns = world.Clans.FirstOrDefault(c => c.Id == strongId)?.Name ?? "?";
                    context.EventQueue.Enqueue(new DiplomacyEvent(
                        DiplomacyEventType.CeasefireAccepted, nw, ns,
                        $"{CeasefireDuration}ターン"));
                    break; // 1勢力につき1件
                }
            }
        }

        // ── 援軍要請 ──────────────────────────────────────────────
        // 同盟国が包囲されていたら、最も近い同盟軍に援軍命令（MoraleBoost）
        private static void TryReinforcement(SimulationContext context)
        {
            var world = context.World;

            foreach (var castle in world.Castles.Where(c => c.SiegeTick > 0))
            {
                var ownerClanId = castle.OwnerClanId;

                // 同盟国を探す
                var allies = world.Alliances
                    .Where(a => a.Involves(ownerClanId))
                    .Select(a => a.ClanId1 == ownerClanId ? a.ClanId2 : a.ClanId1)
                    .ToList();

                if (!allies.Any()) continue;

                // 同盟国の最も近い軍に士気ボーナス（援軍意識）
                var castleHex = world.Map.GetHexById(castle.HexId);
                if (castleHex == null) continue;

                foreach (var allyClanId in allies)
                {
                    var allyArmy = world.Armies
                        .Where(a => a.ClanId == allyClanId && a.Soldiers > 0)
                        .OrderBy(a =>
                        {
                            var h = world.Map.GetHexById(a.CurrentHexId);
                            return h == null ? int.MaxValue : Map.HexDistance.Calculate(castleHex, h);
                        })
                        .FirstOrDefault();

                    if (allyArmy == null) continue;

                    // 援軍意識で士気+5
                    allyArmy.Morale = System.Math.Min(100, allyArmy.Morale + 5);

                    var allyName  = world.Clans.FirstOrDefault(c => c.Id == allyClanId)?.Name ?? "?";
                    var ownerName = world.Clans.FirstOrDefault(c => c.Id == ownerClanId)?.Name ?? "?";
                    context.EventQueue.Enqueue(new DiplomacyEvent(
                        DiplomacyEventType.ReinforcementSent, allyName, ownerName,
                        $"「{castle.Name}」救援"));
                }
            }
        }

        // ── 同盟裏切り ────────────────────────────────────────────
        // 野心的な勢力が同盟国より兵力2倍以上になると同盟を破棄
        private static void TryBetrayal(SimulationContext context)
        {
            var world = context.World;

            foreach (var alliance in world.Alliances.ToList())
            {
                foreach (var (betrayerId, victimId) in new[]
                {
                    (alliance.ClanId1, alliance.ClanId2),
                    (alliance.ClanId2, alliance.ClanId1),
                })
                {
                    var betrayerClan = world.Clans.FirstOrDefault(c => c.Id == betrayerId);
                    if (betrayerClan == null) continue;

                    // 野心的な大名がいる勢力のみ裏切る
                    var daimyo = betrayerClan.DaimyoOfficerId.HasValue
                        ? world.Officers.FirstOrDefault(o => o.Id == betrayerClan.DaimyoOfficerId.Value)
                        : null;
                    if (daimyo?.Personality != Entities.OfficerPersonality.Ambitious) continue;

                    var betrayerSoldiers = TotalSoldiers(betrayerId, world);
                    var victimSoldiers   = TotalSoldiers(victimId,   world);
                    if (victimSoldiers == 0) continue;
                    if ((double)betrayerSoldiers / victimSoldiers < 2.0) continue;

                    world.Alliances.Remove(alliance);
                    var bn = world.Clans.FirstOrDefault(c => c.Id == betrayerId)?.Name ?? "?";
                    var vn = world.Clans.FirstOrDefault(c => c.Id == victimId)?.Name   ?? "?";
                    context.EventQueue.Enqueue(new DiplomacyEvent(
                        DiplomacyEventType.AllianceBetrayed, bn, vn));
                    break;
                }
            }
        }

        // ── ヘルパー ──────────────────────────────────────────────
        private static System.Collections.Generic.List<int> ActiveClanIds(World.WorldState world)
            => world.Armies
                .Where(a => a.Soldiers > 0 && a.ClanId != 0)
                .Select(a => a.ClanId)
                .Distinct()
                .ToList();

        private static int TotalSoldiers(int clanId, World.WorldState world)
            => world.Armies.Where(a => a.ClanId == clanId).Sum(a => a.Soldiers);

        private static double SoldierRatio(int a, int b, World.WorldState world)
        {
            var sa = TotalSoldiers(a, world);
            return sa == 0 ? 0 : (double)TotalSoldiers(b, world) / sa;
        }

        private static int NextId<T>(System.Collections.Generic.List<T> list)
            where T : Entities.Entity
            => list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
    }
}
