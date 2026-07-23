using BattleCore.Entities;
using BattleCore.Map;

namespace BattleCore.Events
{
    /// <summary>
    /// 軍が目的地に到着したイベント。
    /// MovementSystem が DestinationHexId に到達した際に発生する。
    /// UnitType と TerrainType を持つことで MoraleSystem 等が参照できる。
    /// </summary>
    public class MovementEvent : IGameEvent
    {
        public int           ArmyId      { get; }
        public string        OfficerName { get; }
        public int           HexId       { get; }
        public UnitType      UnitType    { get; }
        public TerrainType   Terrain     { get; }

        public MovementEvent(int armyId, string officerName, int hexId,
            UnitType unitType = UnitType.Ashigaru, TerrainType terrain = TerrainType.Plain)
        {
            ArmyId      = armyId;
            OfficerName = officerName;
            HexId       = hexId;
            UnitType    = unitType;
            Terrain     = terrain;
        }
    }
}
