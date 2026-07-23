using BattleCore.Entities;
using BattleCore.Events;
using BattleCore.Map;
using BattleCore.Simulation;
using System.Linq;

namespace BattleCore.Systems
{
    /// <summary>
    /// 移動システム。Army の DestinationHexId を見て1Hexずつ移動させる。
    ///
    /// 占有ルール（1Hex1部隊、城Hexは Capacity まで）：
    ///   移動先に敵がいる  → OccupancyEvent(Combat) を発火し移動しない
    ///                        BattleFinder が隣接戦闘として処理する
    ///   移動先が満員      → OccupancyEvent(Blocked) を発火し移動しない
    ///   移動先が空き      → 通常移動
    /// </summary>
    public class MovementSystem : ISimulationSystem
    {
        public void Update(SimulationContext context)
        {
            foreach (var army in context.World.Armies.ToList())
            {
                army.MovedThisTick = false;

                if (army.ActionPoints <= 0) continue;
                if (army.MoveCooldown > 0) { army.MoveCooldown--; continue; }
                if (army.DestinationHexId == null) continue;

                if (army.Stance == ArmyStance.Defend      ||
                    army.Stance == ArmyStance.Intercept   ||
                    army.Stance == ArmyStance.Ambush      ||
                    army.Stance == ArmyStance.Scouting    ||
                    army.Stance == ArmyStance.Garrisoning)
                    continue;

                if (army.Stance == ArmyStance.Entrenched)
                {
                    if (army.EntrenchTick > 0) army.EntrenchTick--;
                    if (army.EntrenchTick == 0) army.Stance = ArmyStance.Normal;
                    continue;
                }

                var next = GetNextStep(context, army);
                if (next == null || next.Terrain == TerrainType.Mountain) continue;

                var officerName = GetOfficerName(context, army);

                // ── 占有ルール判定 ──────────────────────────────
                bool hasEnemy = OccupancyRules.HasEnemy(next.Id, army.ClanId, context.World);
                if (hasEnemy)
                {
                    // 敵Hexへの侵入 → 戦闘トリガー（移動はしない）
                    // BattleFinder が army.PendingAttackHexId を参照して戦闘を解決する
                    army.PendingAttackHexId = next.Id;
                    context.EventQueue.Enqueue(new OccupancyEvent(
                        army.Id, officerName, next.Id, OccupancyEventType.Combat));
                    army.ActionPoints--;
                    continue;
                }

                bool isFull = !OccupancyRules.CanEnter(next.Id, army.ClanId, context.World);
                if (isFull)
                {
                    // 満員 → 移動ブロック（目的地をクリアして待機）
                    context.EventQueue.Enqueue(new OccupancyEvent(
                        army.Id, officerName, next.Id, OccupancyEventType.Blocked));
                    army.ClearDestination();
                    continue;
                }
                // ────────────────────────────────────────────────

                // River は Bridge がなければ通行コスト+2
                bool hasBridge = context.World.Structures.Any(s =>
                    s.Type == StructureType.Bridge && s.HexId == next.Id);
                if (next.Terrain == TerrainType.River && !hasBridge)
                {
                    army.MoveTo(next.Id);
                    army.MovedThisTick = true;
                    army.MoveCooldown  = context.World.Weather == Weather.Rain ? 4 : 2;
                    if (army.CurrentHexId == army.DestinationHexId)
                    {
                        army.ClearDestination();
                        context.EventQueue.Enqueue(new MovementEvent(
                            army.Id, officerName, army.CurrentHexId, army.UnitType, next.Terrain));
                    }
                    army.ActionPoints--;
                    continue;
                }

                // Facing 更新と方向転換コスト計算
                double turnCost = 0.0;
                var fromHex = context.World.Map.GetHexById(army.CurrentHexId);
                if (fromHex != null)
                {
                    var dir = context.World.Map.GetDirection(fromHex, next);
                    if (dir.HasValue)
                    {
                        int steps = FacingHelper.StepDiff(army.Facing, dir.Value);
                        turnCost  = FacingHelper.TurnCost(steps);
                        army.Facing = (FacingDirection)(int)dir.Value;
                    }
                }

                army.MoveTo(next.Id);
                army.MovedThisTick = true;

                double costFactor = next.Terrain == TerrainType.Plain
                    ? UnitTypeData.PlainMoveCostFactor(army.UnitType)
                    : 1.0;

                bool hasRoad = context.World.Structures.Any(s =>
                    s.Type == StructureType.Road && s.HexId == next.Id);
                if (hasRoad) costFactor = 0.0;

                int heightDiff = next.Height - (fromHex?.Height ?? 0);
                if (!hasRoad && heightDiff > 0) costFactor += 1.0;
                else if (!hasRoad && heightDiff < 0) costFactor = 0.0;

                if (!hasRoad)
                {
                    army.TurnCostAccumulator += turnCost;
                    if (army.TurnCostAccumulator >= 1.0)
                    {
                        army.TurnCostAccumulator -= 1.0;
                        costFactor += 1.0;
                    }
                }

                if (costFactor > 0.0)
                    army.ActionPoints--;

                if (army.InZoc && army.ActionPoints > 0)
                    army.ActionPoints--;

                if (next.Terrain == TerrainType.Forest)
                    army.MoveCooldown = context.World.Weather == Weather.Rain ? 2 : 1;

                if (army.CurrentHexId == army.DestinationHexId)
                {
                    army.ClearDestination();
                    context.EventQueue.Enqueue(new MovementEvent(
                        army.Id, officerName, army.CurrentHexId, army.UnitType, next.Terrain));
                }
            }
        }

        private static string GetOfficerName(SimulationContext context, Army army)
        {
            var officer = army.OfficerId.HasValue
                ? context.World.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value) : null;
            return officer?.Name ?? $"軍{army.Id}";
        }

        private Hex? GetNextStep(SimulationContext context, Army army)
        {
            var neighbors = context.World.Map.GetNeighbors(army.CurrentHexId);
            var direct = neighbors.FirstOrDefault(x => x.Id == army.DestinationHexId);
            if (direct != null) return direct;

            var destination = context.World.Map.GetHexById(army.DestinationHexId!.Value);
            if (destination == null) return null;

            return neighbors
                .Where(x => x.Terrain != TerrainType.Mountain)
                .OrderBy(x => HexDistance.Calculate(x, destination))
                .FirstOrDefault();
        }
    }
}
