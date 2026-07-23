namespace BattleCore.Systems.Battle
{
    /// <summary>
    /// 戦闘ダメージ補正のインターフェース。
    /// Apply は損害を補正しつつ、BattleBreakdown に補正内容を記録する。
    /// </summary>
    public interface IBattleModifier
    {
        (int winnerLosses, int loserLosses) Apply(
            BattleContext   context,
            int             winnerLosses,
            int             loserLosses,
            BattleBreakdown breakdown);
    }
}
