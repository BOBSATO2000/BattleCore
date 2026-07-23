using BattleCore.AI;
using BattleCore.Commands;
using BattleCore.Entities;
using BattleCore.World;
using System.Collections.Generic;

namespace BattleCore.Player
{
    /// <summary>
    /// AI勢力の意図生成者。
    /// IClanStrategy が生成した ICommand を Intent に変換して返す。
    /// 「どう実行するか」の詳細は OfficerDecision に委ねる。
    /// </summary>
    public sealed class AICommander : ICommander
    {
        private readonly IClanStrategy _strategy;

        public int ClanId { get; }

        public AICommander(int clanId, IClanStrategy strategy)
        {
            ClanId    = clanId;
            _strategy = strategy;
        }

        public IEnumerable<Intent> GenerateIntents(Clan clan, WorldState world)
        {
            foreach (var cmd in _strategy.Decide(clan, world))
            {
                var intent = ToIntent(cmd);
                if (intent != null) yield return intent;
            }
        }

        private static Intent? ToIntent(ICommand cmd) => cmd switch
        {
            MoveArmyCommand m => new Intent(m.ArmyId, IntentType.MoveTo,
                priority: 50, OrderLifetime.Persistent, targetHexId: m.DestinationHexId),
            DefendOrder     d => new Intent(d.ArmyId, IntentType.Defend,   priority: 50),
            RetreatOrder    r => new Intent(r.ArmyId, IntentType.Retreat,  priority: 50),
            ScoutOrder      s => new Intent(s.ArmyId, IntentType.Scout,    priority: 50),
            SupplyOrder     s => new Intent(s.ArmyId, IntentType.Supply,   priority: 50),
            FortifyOrder    f => new Intent(f.ArmyId, IntentType.Fortify,  priority: 50),
            SiegeOrder      s => new Intent(s.ArmyId, IntentType.Siege,    priority: 50, targetCastleId: s.CastleId),
            WaitOrder       w => new Intent(w.ArmyId, IntentType.Wait,     priority:  0),
            _                 => null,
        };
    }
}
