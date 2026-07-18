using System;

namespace BattleCore.Entities
{
    /// <summary>
    /// 軍隊エンティティ。
    /// 勢力（Clan）に属し、Hexマップ上を移動して戦闘を行う。
    /// </summary>
    public class Army : Entity
    {
        /// <summary>指揮官（Officer）のID。未配属の場合は null。</summary>
        public int? OfficerId { get; private set; }

        /// <summary>
        /// この軍が所属する勢力のID。
        /// BattleSystem はこの値で味方・敵を判定する。
        /// </summary>
        public int ClanId { get; set; }

        /// <summary>現在いるHexのID。</summary>
        public int CurrentHexId { get; private set; }

        /// <summary>兵力。0になると全滅。</summary>
        public int Soldiers { get; private set; } = 1000;

        /// <summary>
        /// 移動目標HexのID。
        /// null の場合は待機中。MovementSystem はこの値を参照して移動する。
        /// </summary>
        public int? DestinationHexId { get; private set; }

        /// <summary>オブジェクト初期化子用コンストラクタ。</summary>
        public Army() : base() { }

        /// <summary>IDのみ指定するコンストラクタ（テスト用途）。</summary>
        public Army(int id) : base(id) { }

        /// <summary>フル指定コンストラクタ。</summary>
        public Army(int id, int commanderId, int clanId, int currentHexId)
            : base(id)
        {
            ClanId = clanId;
            CurrentHexId = currentHexId;
            Soldiers = 1000;
        }

        /// <summary>指定HexへArmyを瞬間移動させる（MovementSystem から呼ぶ）。</summary>
        public void MoveTo(int hexId) => CurrentHexId = hexId;

        /// <summary>目的地に到着した際に DestinationHexId をクリアする。</summary>
        public void Arrive() => DestinationHexId = null;

        /// <summary>
        /// 移動命令を設定する。
        /// DecisionSystem / CommandExecutionSystem から呼ばれ、
        /// MovementSystem が次のStep で1Hexずつ進める。
        /// </summary>
        public void OrderMove(int destinationHexId) => DestinationHexId = destinationHexId;

        /// <summary>
        /// 兵力を減らす。0未満にはならない。
        /// BattleSystem から呼ばれる。
        /// </summary>
        public void LoseSoldiers(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            Soldiers = Math.Max(0, Soldiers - count);
        }

        /// <summary>移動目標をクリアして待機状態にする。</summary>
        public void ClearDestination() => DestinationHexId = null;

        /// <summary>指揮官（Officer）を配属する。</summary>
        public void AssignOfficer(int officerId) => OfficerId = officerId;

        /// <summary>
        /// 兵力を補充する。MaxSoldiersを超えない範囲で増加する。
        /// SupplySystem から呼ばれる。
        /// </summary>
        public void Reinforce(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            Soldiers += amount;
        }

        /// <summary>
        /// 軍が離反し、新しい勢力へ移る。
        /// LoyaltySystem が武将の離反処理の一環として呼び出す。
        /// </summary>
        public void Defect(int newClanId) => ClanId = newClanId;
    }
}
