using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Vision;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 索敵システム。視界範囲を以下の要素で動的に計算する。
    ///
    /// 基本視界: 2
    /// +Height  : 自軍Hexの高度分だけ加算（最大+3）
    /// +Tower   : 同Hex同勢力のTower +2
    /// +Scouting: 偵察態勢 +1
    /// -夜      : -2（最低1）
    /// -雨      : -1
    /// -霧      : -2
    /// </summary>
    public class VisionSystem : ISimulationSystem
    {
        private const int BaseVisionRange = 2;
        private const int TowerBonus      = 2;

        public void Update(SimulationContext context)
        {
            bool isNight = context.Time.IsNight;
            var  weather = context.World.Weather;

            foreach (var army in context.World.Armies)
            {
                var currentHex = context.World.Map.GetHexById(army.CurrentHexId);
                int range = BaseVisionRange;

                // 高度ボーナス
                range += currentHex?.Height ?? 0;

                // Tower ボーナス
                bool hasTower = context.World.Structures.Any(s =>
                    s.Type == StructureType.Tower &&
                    s.HexId == army.CurrentHexId &&
                    s.OwnerClanId == army.ClanId);
                if (hasTower) range += TowerBonus;

                // 偵察態勢
                if (army.Stance == ArmyStance.Scouting) range += army.ScoutingBonus ? 3 : 1;

                // 強行軍は視界-1
                if (army.Marching) range -= 1;

                // 夜・天候ペナルティ
                if (isNight)                    range -= 2;
                if (weather == Weather.Rain)    range -= 1;
                if (weather == Weather.Fog)     range -= 2;

                range = System.Math.Max(1, range);

                var vision = new VisionData(army.Id);
                foreach (var hexId in FindVisibleHexes(context, army.CurrentHexId, range))
                    vision.VisibleHexes.Add(hexId);

                context.World.Visions[army.Id] = vision;
            }
        }

        private IEnumerable<int> FindVisibleHexes(SimulationContext context, int startHexId, int range)
        {
            var visited = new HashSet<int> { startHexId };
            var queue   = new Queue<(int hexId, int distance)>();
            queue.Enqueue((startHexId, 0));

            while (queue.Count > 0)
            {
                var (current, distance) = queue.Dequeue();
                yield return current;

                if (distance >= range) continue;

                foreach (var neighbor in context.World.Map.GetNeighbors(current))
                {
                    if (visited.Add(neighbor.Id))
                        queue.Enqueue((neighbor.Id, distance + 1));
                }
            }
        }
    }
}
