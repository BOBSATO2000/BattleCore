using System;

namespace BattleCore.Entities
{
    public class Army : Entity
    {
        public int? OfficerId { get; private set; }
        public int ClanId { get; set; }
        public int CurrentHexId { get; private set; }
        public int Soldiers { get; private set; } = 1000;
        public int? DestinationHexId { get; private set; }

        public Army() : base() { }
        public Army(int id) : base(id) { }
        public Army(int id, int commanderId, int clanId, int currentHexId) : base(id)
        {
            ClanId = clanId;
            CurrentHexId = currentHexId;
            Soldiers = 1000;
        }

        public void MoveTo(int hexId) => CurrentHexId = hexId;
        public void Arrive() => DestinationHexId = null;
        public void OrderMove(int destinationHexId) => DestinationHexId = destinationHexId;

        public void LoseSoldiers(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Soldiers = Math.Max(0, Soldiers - count);
        }

        public void ClearDestination() => DestinationHexId = null;

        public int MoveCooldown { get; set; } = 0;
        public int ActionPoints { get; set; } = MaxActionPoints;
        public const int MaxActionPoints = 2;
        public void ResetActionPoints() => ActionPoints = MaxActionPoints;
        public void AssignOfficer(int officerId) => OfficerId = officerId;
        public int MaxSoldiers { get; private set; } = 1000;

        public void Reinforce(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Soldiers = Math.Min(MaxSoldiers, Soldiers + amount);
        }

        public void SetInitialSoldiers(int soldiers)
        {
            Soldiers    = soldiers;
            MaxSoldiers = soldiers;
        }

        public void SetSoldiers(int soldiers) => Soldiers = Math.Clamp(soldiers, 0, MaxSoldiers);
        public void Defect(int newClanId) => ClanId = newClanId;

        public int Morale { get; set; } = 100;
        public int Food { get; set; } = 100;
        public const int MaxFood = 100;
        public const int FoodConsumptionPerTick = 10;
        public bool IsSupplied { get; set; } = true;
        public int Fatigue { get; set; } = 0;
        public bool MovedThisTick { get; set; } = false;
        public UnitType UnitType { get; set; } = UnitType.Ashigaru;
        public FacingDirection Facing { get; set; } = FacingDirection.East;
        public ArmyStance Stance { get; set; } = ArmyStance.Normal;
        public double TurnCostAccumulator { get; set; } = 0.0;
        public bool ScoutingBonus { get; set; } = false;
        public bool Marching { get; set; } = false;
        public int EntrenchTick { get; set; } = 0;
        public bool IsDecoy { get; set; } = false;
        public ArmyFormation Formation { get; set; } = ArmyFormation.Normal;
        public bool InZoc { get; set; } = false;

        /// <summary>
        /// 戦闘トリガー待機HexId。
        /// MovementSystem が敵Hexへの侵入時に設定する。
        /// BattleFinder がこの値を参照して隣接戦闘を解決する。
        /// 毎Tick開始時に SimulationEngine がリセットする。
        /// </summary>
        public int? PendingAttackHexId { get; set; } = null;
    }
}
