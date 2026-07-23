using BattleCore.AI;
using BattleCore.Entities;
using BattleCore.World;
using System.Collections.Generic;
using System.Linq;

namespace BattleCore.Player
{
    /// <summary>
    /// AI勢力の命令生成者。
    /// IClanStrategy（AggressiveClanStrategy など）を内部で呼び出し、
    /// 生成された ICommand を CommanderOrder にラップして返す。
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

        public IEnumerable<CommanderOrder> GenerateOrders(Clan clan, WorldState world)
        {
            foreach (var cmd in _strategy.Decide(clan, world))
                yield return new CommanderOrder(cmd, priority: 10, OrderLifetime.Persistent);
        }
    }
}
