using BattleCore.Entities;
using BattleCore.Simulation;

namespace BattleCore.Commands
{
    /// <summary>
    /// 陣形命令。指定した陣形を設定する。
    /// BattleModifiers の FormationModifier が参照する。
    /// </summary>
    public sealed class FormationOrder : ICommand
    {
        public int           ArmyId    { get; }
        public ArmyFormation Formation { get; }

        public FormationOrder(int armyId, ArmyFormation formation)
        {
            ArmyId    = armyId;
            Formation = formation;
        }

        public void Execute(SimulationContext context)
        {
            var army = context.World.GetArmyById(ArmyId);
            if (army == null) return;
            army.Formation = Formation;
        }
    }
}
