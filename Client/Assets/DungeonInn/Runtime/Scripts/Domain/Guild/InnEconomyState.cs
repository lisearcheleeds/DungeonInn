using DungeonInn.Domain.Common;

namespace DungeonInn.Domain.Guild
{
    public sealed class InnEconomyState
    {
        public int Reputation { get; private set; } = GameConstants.InitialInnReputation;
    }
}
