using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;

namespace BattleCore.Systems.Battle
{
    public sealed class BattleContext
    {
        public Army         Attacker        { get; }
        public Army         Defender        { get; }
        public Officer?     AttackerOfficer { get; }
        public Officer?     DefenderOfficer { get; }
        public TerrainType  DefenderTerrain { get; }
        public Weather      Weather         { get; }
        public bool         HasCastle       { get; }
        public ArmyStance   AttackerStance  => Attacker.Stance;
        public ArmyStance   DefenderStance  => Defender.Stance;

        /// <summary>攻撃側のHex。HeightModifier が参照する。</summary>
        public Hex?         AttackerHex     { get; }
        /// <summary>防御側のHex。HeightModifier が参照する。</summary>
        public Hex?         DefenderHex     { get; }

        public BattleContext(
            Army        attacker,
            Army        defender,
            Officer?    attackerOfficer,
            Officer?    defenderOfficer,
            TerrainType defenderTerrain,
            Weather     weather,
            bool        hasCastle,
            Hex?        attackerHex = null,
            Hex?        defenderHex = null)
        {
            Attacker        = attacker;
            Defender        = defender;
            AttackerOfficer = attackerOfficer;
            DefenderOfficer = defenderOfficer;
            DefenderTerrain = defenderTerrain;
            Weather         = weather;
            HasCastle       = hasCastle;
            AttackerHex     = attackerHex;
            DefenderHex     = defenderHex;
        }
    }
}
