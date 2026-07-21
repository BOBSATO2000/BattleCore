using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Navigation;
using BattleCore.Simulation;
using BattleCore.World;

namespace BattleCore.Debug
{
    /// <summary>
    /// 選択部隊のデバッグ情報を構築する。UI非依存。
    /// WinForms / Unity / ログ出力で共通利用できる。
    /// </summary>
    public static class DebugPanelBuilder
    {
        public record DebugLine(string Text, DebugColor Color = DebugColor.Normal);

        public enum DebugColor { Normal, Header, Info, Good, Warn, Dim, Path, AI }

        public static IReadOnlyList<DebugLine> Build(
            int armyId,
            WorldState world,
            SimulationContext context,
            IReadOnlyDictionary<int, (string Summary, IReadOnlyList<string> Factors)>? lastDecisions = null)
        {
            var lines = new List<DebugLine>();
            var army  = world.GetArmyById(armyId);
            if (army == null)
            {
                lines.Add(new("(部隊なし)", DebugColor.Dim));
                return lines;
            }

            var officer = army.OfficerId.HasValue
                ? world.Officers.FirstOrDefault(o => o.Id == army.OfficerId.Value)
                : null;
            var clan = world.Clans.FirstOrDefault(c => c.Id == army.ClanId);
            var membership = officer != null
                ? world.Memberships.FirstOrDefault(m => m.OfficerId == officer.Id && m.ClanId == army.ClanId)
                : null;
            int loyalty = membership?.Loyalty ?? officer?.Loyalty ?? 0;

            // ヘッダー
            lines.Add(new("=== BattleCore Debug ===", DebugColor.Header));
            lines.Add(new($"Turn:{context.Time.Tick}  Phase:{context.CurrentPhase}", DebugColor.Dim));
            lines.Add(new(""));

            // 部隊
            lines.Add(new("--- Selected Army ---", DebugColor.Info));
            lines.Add(new($"勢力  : {clan?.Name ?? "無所属"}"));
            if (officer != null)
            {
                lines.Add(new($"武将  : {officer.Name}"));
                lines.Add(new($"性格  : {PersonalityLabel(officer.Personality)}"));
                lines.Add(new($"忠誠  : {loyalty}", loyalty < 30 ? DebugColor.Warn : DebugColor.Normal));
                lines.Add(new($"統率  : {officer.Leadership}  武勇:{officer.Courage}"));
            }
            lines.Add(new($"兵力  : {army.Soldiers}", army.Soldiers < 300 ? DebugColor.Warn : DebugColor.Good));
            lines.Add(new($"Hex   : {army.CurrentHexId}"));
            lines.Add(new($"AP    : {army.ActionPoints}/{Army.MaxActionPoints}",
                army.ActionPoints == 0 ? DebugColor.Warn : DebugColor.Normal));
            if (army.DestinationHexId.HasValue)
                lines.Add(new($"目標  : Hex{army.DestinationHexId.Value}", DebugColor.Path));
            lines.Add(new(""));

            // AI判断
            lines.Add(new("--- Last Decision ---", DebugColor.Info));
            if (lastDecisions != null && lastDecisions.TryGetValue(armyId, out var dec))
            {
                lines.Add(new($"判断  : {dec.Summary}", DebugColor.AI));
                foreach (var f in dec.Factors.Where(f => !string.IsNullOrEmpty(f)))
                    lines.Add(new($"  ・{f}", DebugColor.Dim));
            }
            else
            {
                lines.Add(new("  (まだ判断なし)", DebugColor.Dim));
            }
            lines.Add(new(""));

            // 経路（A*）
            lines.Add(new("--- Path (A*) ---", DebugColor.Info));
            if (army.DestinationHexId.HasValue && army.DestinationHexId.Value != army.CurrentHexId)
            {
                var result = new HexPathFinder().FindPathWithCost(
                    world.Map, army.CurrentHexId, army.DestinationHexId.Value);
                if (result.HexIds.Count > 0)
                {
                    lines.Add(new("  " + string.Join("→", result.HexIds), DebugColor.Path));
                    var costParts = result.HexIds
                        .Zip(result.StepCosts, (h, c) => c > 0 ? $"{h}({c})" : $"{h}")
                        .ToList();
                    lines.Add(new("  " + string.Join("→", costParts), DebugColor.Dim));
                    lines.Add(new($"  Total Cost: {result.TotalCost}", DebugColor.Dim));
                }
                else
                    lines.Add(new("  (経路なし)", DebugColor.Dim));
            }
            else
            {
                lines.Add(new("  (移動なし)", DebugColor.Dim));
            }
            lines.Add(new(""));

            // Vision
            lines.Add(new("--- Vision ---", DebugColor.Info));
            var visibleHexes = world.Visions.Values
                .Where(v => v.ArmyId == armyId)
                .SelectMany(v => v.VisibleHexes)
                .Distinct()
                .OrderBy(h => h)
                .ToList();
            lines.Add(new($"  可視Hex数: {visibleHexes.Count}", DebugColor.Dim));
            if (visibleHexes.Count > 0)
                lines.Add(new("  " + string.Join(",", visibleHexes.Take(14))
                    + (visibleHexes.Count > 14 ? "..." : ""), DebugColor.Dim));
            lines.Add(new(""));

            // CommandQueue（この部隊分）
            lines.Add(new("--- CommandQueue ---", DebugColor.Info));
            var cmds = context.CommandQueue
                .OfType<MoveArmyCommand>()
                .Where(c => c.ArmyId == armyId)
                .ToList();
            if (cmds.Any())
                foreach (var c in cmds)
                    lines.Add(new($"  Move→Hex{c.DestinationHexId} [{c.Reason}]", DebugColor.Path));
            else
                lines.Add(new("  (なし)", DebugColor.Dim));

            return lines;
        }

        private static string PersonalityLabel(OfficerPersonality p) => p switch
        {
            OfficerPersonality.Brave       => "勇猛",
            OfficerPersonality.Cautious    => "慎重",
            OfficerPersonality.Ambitious   => "野心的",
            OfficerPersonality.Loyal       => "忠義",
            OfficerPersonality.Opportunist => "日和見",
            _ => ""
        };
    }
}
