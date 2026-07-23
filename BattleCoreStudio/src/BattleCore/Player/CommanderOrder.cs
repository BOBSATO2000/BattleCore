using BattleCore.Commands;

namespace BattleCore.Player
{
    /// <summary>
    /// 命令の寿命。
    /// OneShot    : 1Tick実行したら消える（偵察・待機など）
    /// Persistent : 目標到達 or 明示的キャンセルまで毎Tick継続（移動・包囲など）
    /// </summary>
    public enum OrderLifetime { OneShot, Persistent }

    /// <summary>
    /// ICommander が生成する命令ラッパー。
    /// Priority が高いほど優先される（AIの補給割り込みなど）。
    /// </summary>
    public sealed class CommanderOrder
    {
        public ICommand       Command  { get; }
        public int            Priority { get; }   // 高いほど優先
        public OrderLifetime  Lifetime { get; }

        public CommanderOrder(ICommand command, int priority = 10, OrderLifetime lifetime = OrderLifetime.OneShot)
        {
            Command  = command;
            Priority = priority;
            Lifetime = lifetime;
        }
    }
}
