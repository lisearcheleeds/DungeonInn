using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class InnFeeCharged : IGameEvent
    {
        public Guid ActorId { get; }
        public int FeeAmount { get; }
        public int ActorRemainingGold { get; }
        public int GuildGold { get; }

        public InnFeeCharged(Guid actorId, int feeAmount, int actorRemainingGold, int guildGold)
        {
            ActorId = actorId;
            FeeAmount = feeAmount;
            ActorRemainingGold = actorRemainingGold;
            GuildGold = guildGold;
        }
    }
}
