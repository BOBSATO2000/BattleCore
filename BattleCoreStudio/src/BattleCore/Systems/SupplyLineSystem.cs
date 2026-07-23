using BattleCore.Simulation;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 補給線システム。毎Tick実行。
    ///
    /// 補給線の定義：
    ///   自勢力の城から、敵に塞がれていないHexを経由して到達できる経路。
    ///   BFSで「補給圏内のHex集合」を計算し、そこに属さない軍は補給切れとする。
    ///
    /// 補給切れの効果：
    ///   Army.IsSupplied = false → FoodSystem が消費量を増加させる
    ///
    /// 将来の拡張：
    ///   - 補給路の距離制限（城から5Hex以内など）
    ///   - 街道・海路ボーナス
    ///   - 補給部隊エンティティ
    /// </summary>
    public class SupplyLineSystem : ISimulationSystem
    {
        /// <summary>補給圏の最大半径（Hex数）。0=無制限。</summary>
        public int MaxRange { get; }

        public SupplyLineSystem(int maxRange = 0)
        {
            MaxRange = maxRange;
        }

        public void Update(SimulationContext context)
        {
            var world = context.World;

            foreach (var army in world.Armies)
            {
                if (army.Soldiers == 0) continue;

                var suppliedHexes = CalcSuppliedHexes(army.ClanId, context);
                army.IsSupplied = suppliedHexes.Contains(army.CurrentHexId);
            }
        }

        /// <summary>
        /// 指定勢力の補給圏（到達可能なHex集合）をBFSで計算する。
        /// 敵軍が占拠しているHexは通過不可（補給路遮断）。
        /// </summary>
        private HashSet<int> CalcSuppliedHexes(int clanId, SimulationContext context)
        {
            var world = context.World;

            // 補給源：自勢力の城
            var sources = world.Castles
                .Where(c => c.OwnerClanId == clanId)
                .Select(c => c.HexId)
                .ToList();

            if (!sources.Any()) return new HashSet<int>();

            // 敵が占拠しているHex（補給路遮断）
            var blockedHexes = world.Armies
                .Where(a => a.Soldiers > 0 &&
                            a.ClanId != clanId &&
                            !world.AreAllied(clanId, a.ClanId))
                .Select(a => a.CurrentHexId)
                .ToHashSet();

            // BFS
            var visited = new HashSet<int>(sources);
            var queue   = new Queue<(int hexId, int dist)>(sources.Select(s => (s, 0)));

            while (queue.Count > 0)
            {
                var (hexId, dist) = queue.Dequeue();

                if (MaxRange > 0 && dist >= MaxRange) continue;

                foreach (var neighbor in world.Map.GetNeighbors(hexId))
                {
                    if (visited.Contains(neighbor.Id)) continue;
                    if (blockedHexes.Contains(neighbor.Id)) continue;

                    visited.Add(neighbor.Id);
                    queue.Enqueue((neighbor.Id, dist + 1));
                }
            }

            return visited;
        }
    }
}
