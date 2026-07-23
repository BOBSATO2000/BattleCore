using BattleCore.Events;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 包囲・籠城システム。毎Tick実行。
    ///
    /// 包囲の定義：
    ///   城の隣接Hex（補給路）が全て敵軍に塞がれている状態。
    ///   城Hexに敵がいるだけでは包囲にならない。
    ///   隣接Hexが1つでも味方（または中立）なら補給路あり＝包囲解除。
    ///
    /// 将来の拡張余地：
    ///   - 補給路の距離を伸ばす（城から2Hex以内など）
    ///   - 海路・山越えルートの考慮
    ///
    /// 包囲成立時：
    ///   - SiegeTick++ / 守備側士気 -2/Tick / 攻囲側士気 +1/Tick
    ///   - SiegeTick >= SurrenderThreshold で降伏
    /// </summary>
    public class SiegeSystem : ISimulationSystem
    {
        private const int SurrenderThreshold = 10;
        private const int PalisadeBonus      = 5;

        public void Update(SimulationContext context)
        {
            var world = context.World;

            foreach (var castle in world.Castles)
            {
                if (castle.OwnerClanId == 0) continue;

                bool isUnderSiege = IsSurrounded(castle.HexId, castle.OwnerClanId, context);

                // Palisade があれば降伏閘値を増加
                bool hasPalisade = world.Structures.Any(s =>
                    s.Type == BattleCore.Entities.StructureType.Palisade &&
                    s.HexId == castle.HexId &&
                    s.OwnerClanId == castle.OwnerClanId);

                // Well があれば降伏閾値をさらに増加（水があれば長期耐久できる）
                bool hasWell = world.Structures.Any(s =>
                    s.Type == BattleCore.Entities.StructureType.Well &&
                    s.HexId == castle.HexId &&
                    s.OwnerClanId == castle.OwnerClanId);

                int threshold = SurrenderThreshold
                    + (hasPalisade ? PalisadeBonus : 0)
                    + (hasWell    ? 8             : 0);

                var defenders = world.Armies.Where(a =>
                    a.Soldiers > 0 &&
                    a.CurrentHexId == castle.HexId &&
                    a.ClanId == castle.OwnerClanId).ToList();

                var besiegers = world.Armies.Where(a =>
                    a.Soldiers > 0 &&
                    a.ClanId != castle.OwnerClanId &&
                    !world.AreAllied(castle.OwnerClanId, a.ClanId) &&
                    world.Map.GetNeighbors(castle.HexId)
                        .Select(h => h.Id)
                        .Contains(a.CurrentHexId)).ToList();

                if (isUnderSiege)
                {
                    bool wasAlreadySieged = castle.SiegeTick > 0;
                    castle.SiegeTick++;

                    if (!wasAlreadySieged)
                        context.EventQueue.Enqueue(new SiegeEvent(
                            SiegeEventType.SiegeStarted, castle.Name, castle.OwnerClanId));

                    foreach (var def in defenders)
                        def.Morale = System.Math.Max(0, def.Morale - 2);
                    foreach (var att in besiegers)
                        att.Morale = System.Math.Min(100, att.Morale + 1);

                    if (castle.SiegeTick >= threshold)
                    {
                        var newOwner = besiegers.Any()
                            ? besiegers.First().ClanId
                            : world.Armies
                                .Where(a => a.Soldiers > 0 &&
                                            a.ClanId != castle.OwnerClanId &&
                                            !world.AreAllied(castle.OwnerClanId, a.ClanId))
                                .OrderBy(a => world.Map.GetNeighbors(castle.HexId)
                                    .Any(h => h.Id == a.CurrentHexId) ? 0 : 1)
                                .First().ClanId;

                        context.EventQueue.Enqueue(new SiegeEvent(
                            SiegeEventType.Surrendered, castle.Name, castle.OwnerClanId));
                        castle.OwnerClanId = newOwner;
                        castle.SiegeTick   = 0;
                        context.EventQueue.Enqueue(new CastleCapturedEvent(
                            castle.Id, castle.Name, newOwner));
                    }
                }
                else if (castle.SiegeTick > 0)
                {
                    castle.SiegeTick = 0;
                    context.EventQueue.Enqueue(new SiegeEvent(
                        SiegeEventType.SiegeLifted, castle.Name, castle.OwnerClanId));
                }
            }
        }

        /// <summary>
        /// 城の補給路が断たれているか判定する。
        /// 城の隣接Hex全てに敵軍がいる（＝味方・中立のHexが1つもない）場合に true。
        /// 隣接Hexが存在しない（孤島）場合は包囲なしとする。
        /// </summary>
        private static bool IsSurrounded(int castleHexId, int ownerClanId, SimulationContext context)
        {
            var world     = context.World;
            var neighbors = world.Map.GetNeighbors(castleHexId);

            if (!neighbors.Any()) return false;

            // 隣接Hexのうち、敵軍が占拠していないHexが1つでもあれば補給路あり
            foreach (var neighbor in neighbors)
            {
                bool blockedByEnemy = world.Armies.Any(a =>
                    a.Soldiers > 0 &&
                    a.CurrentHexId == neighbor.Id &&
                    a.ClanId != ownerClanId &&
                    !world.AreAllied(ownerClanId, a.ClanId));

                if (!blockedByEnemy) return false;  // このHexは通れる
            }

            return true;  // 全隣接Hexが敵に塞がれている
        }
    }
}
