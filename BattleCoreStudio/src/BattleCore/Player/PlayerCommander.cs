using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Navigation;
using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Player
{
    /// <summary>
    /// プレイヤー（大名）の命令生成者。
    /// UI から EnqueueOrder() で命令を受け取り、GenerateOrders() で返す。
    ///
    /// ルール：
    /// - 入力がなければ空リストを返す（AIは動かない）
    /// - Persistent 命令は目標到達 or キャンセルまで毎Tick再発行する
    /// - AI割り込み（Food=0 など）は CommanderSystem 側で Priority 比較して処理する
    /// </summary>
    public sealed class PlayerCommander : ICommander
    {
        public int ClanId { get; }

        // UI から積まれた新規命令
        private readonly Queue<CommanderOrder> _incoming = new();

        // 継続中の Persistent 命令（ArmyId → CommanderOrder）
        private readonly Dictionary<int, CommanderOrder> _persistent = new();

        private readonly IPathFinder _pathFinder = new HexPathFinder();

        public PlayerCommander(int clanId) => ClanId = clanId;

        /// <summary>UIから呼ぶ。命令をキューに積む。</summary>
        public void EnqueueOrder(CommanderOrder order) => _incoming.Enqueue(order);

        /// <summary>指定部隊の継続命令をキャンセルする。</summary>
        public void CancelOrder(int armyId) => _persistent.Remove(armyId);

        /// <summary>未処理の新規命令数（UI表示用）。</summary>
        public int PendingCount => _incoming.Count;

        public IEnumerable<CommanderOrder> GenerateOrders(Clan clan, WorldState world)
        {
            // 新規命令を取り込む（同部隊の既存 Persistent を上書き）
            while (_incoming.Count > 0)
            {
                var order = _incoming.Dequeue();
                var armyId = GetArmyId(order.Command);
                if (armyId.HasValue)
                {
                    if (order.Lifetime == OrderLifetime.Persistent)
                        _persistent[armyId.Value] = order;
                    yield return order;
                }
                else
                {
                    yield return order;
                }
            }

            // Persistent 命令を継続発行（目標到達済みなら削除）
            var toRemove = new List<int>();
            foreach (var (armyId, order) in _persistent)
            {
                var army = world.GetArmyById(armyId);
                if (army == null || army.Soldiers == 0)
                {
                    toRemove.Add(armyId);
                    continue;
                }

                // 目標到達判定（MoveArmyCommand の場合）
                if (order.Command is MoveArmyCommand move && army.CurrentHexId == move.DestinationHexId)
                {
                    toRemove.Add(armyId);
                    continue;
                }

                yield return order;
            }
            foreach (var id in toRemove) _persistent.Remove(id);
        }

        private static int? GetArmyId(ICommand cmd) => cmd switch
        {
            MoveArmyCommand m  => m.ArmyId,
            MoveOrder       m  => m.ArmyId,
            DefendOrder     d  => d.ArmyId,
            RetreatOrder    r  => r.ArmyId,
            ScoutOrder      s  => s.ArmyId,
            SupplyOrder     s  => s.ArmyId,
            FortifyOrder    f  => f.ArmyId,
            SiegeOrder      s  => s.ArmyId,
            WaitOrder       w  => w.ArmyId,
            _                  => null,
        };
    }
}
