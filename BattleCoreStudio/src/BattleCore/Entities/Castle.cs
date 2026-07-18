namespace BattleCore.Entities
{
    /// <summary>
    /// 城・拠点エンティティ。
    /// 特定のHexに配置され、占領した勢力に毎Tick兵力補充ボーナスを与える。
    /// 防御ボーナス: 城のあるHexで戦闘する場合、敗者損害を20%軽減。
    /// </summary>
    public class Castle : Entity
    {
        /// <summary>城が配置されているHexのID。</summary>
        public int HexId { get; }

        /// <summary>城の名前。</summary>
        public string Name { get; }

        /// <summary>現在の占領勢力ID。0=中立。</summary>
        public int OwnerClanId { get; set; }

        /// <summary>毎Tick補充する兵力。占領勢力の軍が同Hexにいる場合に適用。</summary>
        public int ReinforcementPerTick { get; }

        public Castle(int id, string name, int hexId, int ownerClanId, int reinforcementPerTick = 50)
            : base(id)
        {
            Name                  = name;
            HexId                 = hexId;
            OwnerClanId           = ownerClanId;
            ReinforcementPerTick  = reinforcementPerTick;
        }
    }
}
