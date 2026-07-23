namespace BattleCore.Entities
{
    /// <summary>
    /// 構造物エンティティ。Hexに配置され、各Systemが効果を参照する。
    /// Tower: 視界+2 / Palisade: 包囲時間+ / Camp: 食糧補給 / Bridge: 川通過
    /// </summary>
    public class Structure : Entity
    {
        public StructureType Type        { get; }
        public int           HexId       { get; }
        public int           OwnerClanId { get; set; }
        public int           Hp          { get; set; } = 100;

        public Structure(int id, StructureType type, int hexId, int ownerClanId)
            : base(id)
        {
            Type        = type;
            HexId       = hexId;
            OwnerClanId = ownerClanId;
        }
    }
}
