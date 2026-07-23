namespace BattleCore.Entities
{
    /// <summary>
    /// 城・拠点エンティティ。
    /// 特定のHexに配置され、占領した勢力に毎Tick兵力補充ボーナスを与える。
    /// 防御ボーナス: 城のあるHexで戦闘する場合、敗者損害を20%軽減。
    /// Capacity: 城Hexに駐留できる最大部隊数（デフォルト4）。
    /// </summary>
    public class Castle : Entity
    {
        public int    HexId               { get; }
        public string Name                { get; }
        public int    OwnerClanId         { get; set; }
        public int    ReinforcementPerTick { get; }
        public int    SiegeTick           { get; set; } = 0;

        /// <summary>
        /// 城Hexに駐留できる最大部隊数。
        /// 小城=2、中城=4（デフォルト）、大城=6〜8。
        /// OccupancyRules が参照する。
        /// </summary>
        public int Capacity { get; set; } = 4;

        public Castle(int id, string name, int hexId, int ownerClanId, int reinforcementPerTick = 50)
            : base(id)
        {
            Name                 = name;
            HexId                = hexId;
            OwnerClanId          = ownerClanId;
            ReinforcementPerTick = reinforcementPerTick;
        }
    }
}
