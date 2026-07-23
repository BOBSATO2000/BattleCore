using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Systems.Battle
{
    /// <summary>
    /// 1件の補正エントリ。ラベルと係数を持つ。
    /// 例: Label="森林防御", Factor=0.80
    /// </summary>
    public sealed class ModifierEntry
    {
        public string Label  { get; }
        public double Factor { get; }

        public ModifierEntry(string label, double factor)
        {
            Label  = label;
            Factor = factor;
        }

        /// <summary>表示用文字列。例: "森林防御 -20%" / "背後攻撃 +50%"</summary>
        public override string ToString()
        {
            int pct = (int)System.Math.Round((Factor - 1.0) * 100);
            string sign = pct >= 0 ? "+" : "";
            return $"{Label} {sign}{pct}%";
        }
    }

    /// <summary>
    /// 戦闘補正の内訳。各 IBattleModifier が追記し、BattleResult に格納される。
    /// BattleLogEvent がこれを使ってプレイヤーに「なぜそうなったか」を表示する。
    /// </summary>
    public sealed class BattleBreakdown
    {
        private readonly List<ModifierEntry> entries = new();

        public IReadOnlyList<ModifierEntry> Entries => entries;

        public void Add(string label, double factor)
        {
            if (System.Math.Abs(factor - 1.0) > 0.001)
                entries.Add(new ModifierEntry(label, factor));
        }

        /// <summary>全補正を乗算した総係数。</summary>
        public double TotalFactor => entries.Aggregate(1.0, (acc, e) => acc * e.Factor);

        /// <summary>
        /// ログ表示用の複数行文字列。
        /// 例:
        ///   森林防御 -20%
        ///   雨天 -10%
        ///   背後攻撃 +50%
        ///   総補正 ×1.08
        /// </summary>
        public string ToLogString()
        {
            if (entries.Count == 0) return "補正なし";
            var lines = entries.Select(e => e.ToString()).ToList();
            lines.Add($"総補正 ×{TotalFactor:F2}");
            return string.Join(" / ", lines);
        }
    }
}
