using BattleCore.Entities;
using BattleCore.World;
using System.Collections.Generic;

namespace BattleCore.Player
{
    /// <summary>
    /// プレイヤー（大名）の意図生成者。
    /// UI から EnqueueIntent() で意図を受け取り、GenerateIntents() で返す。
    ///
    /// ルール：
    /// - 入力がなければ空リストを返す（その部隊はAI命令なし）
    /// - Persistent 意図は目標到達 or キャンセルまで毎Tick再発行する
    /// - AI緊急割り込みは CommanderSystem 側で Priority 比較して処理する
    /// </summary>
    public sealed class PlayerCommander : ICommander
    {
        public int ClanId { get; }

        private readonly Queue<Intent>            _incoming   = new();
        private readonly Dictionary<int, Intent>  _persistent = new();  // ArmyId → Intent

        public PlayerCommander(int clanId) => ClanId = clanId;

        /// <summary>UIから呼ぶ。意図をキューに積む。</summary>
        public void EnqueueIntent(Intent intent) => _incoming.Enqueue(intent);

        /// <summary>指定部隊の継続意図をキャンセルする。</summary>
        public void CancelIntent(int armyId) => _persistent.Remove(armyId);

        /// <summary>未処理の新規意図数（UI表示用）。</summary>
        public int PendingCount => _incoming.Count;

        /// <summary>指定部隊に継続意図があるか（UI表示用）。</summary>
        public bool HasPersistentIntent(int armyId) => _persistent.ContainsKey(armyId);

        public IEnumerable<Intent> GenerateIntents(Clan clan, WorldState world)
        {
            // 新規意図を取り込む（同部隊の既存 Persistent を上書き）
            while (_incoming.Count > 0)
            {
                var intent = _incoming.Dequeue();
                if (intent.Lifetime == OrderLifetime.Persistent)
                    _persistent[intent.ArmyId] = intent;
                yield return intent;
            }

            // Persistent 意図を継続発行（目標到達済み or 全滅なら削除）
            var toRemove = new List<int>();
            foreach (var (armyId, intent) in _persistent)
            {
                var army = world.GetArmyById(armyId);
                if (army == null || army.Soldiers == 0)
                {
                    toRemove.Add(armyId);
                    continue;
                }
                // MoveTo: 目標Hexに到達したら終了
                if (intent.Type == IntentType.MoveTo
                    && intent.TargetHexId.HasValue
                    && army.CurrentHexId == intent.TargetHexId.Value)
                {
                    toRemove.Add(armyId);
                    continue;
                }
                yield return intent;
            }
            foreach (var id in toRemove) _persistent.Remove(id);
        }
    }
}
