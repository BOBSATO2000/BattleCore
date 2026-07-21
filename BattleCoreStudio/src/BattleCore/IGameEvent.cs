namespace BattleCore;
/// <summary>
/// ゲーム内イベントのマーカーインターフェース。
/// BattleLogEvent / BetrayalEvent / GameOverEvent など全てのイベントが実装する。
/// SimulationContext.EventQueue に積まれ、UI・ログシステムが参照する。
/// </summary>
public interface IGameEvent{}
