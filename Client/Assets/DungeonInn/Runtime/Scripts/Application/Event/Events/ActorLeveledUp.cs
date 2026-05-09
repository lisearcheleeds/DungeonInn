using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorLeveledUp : IGameEvent
    {
        public Guid ActorId { get; }
        public int PreviousLevel { get; }
        public int NewLevel { get; }

        public ActorLeveledUp(Guid actorId, int previousLevel, int newLevel)
        {
            ActorId = actorId;
            PreviousLevel = previousLevel;
            NewLevel = newLevel;
        }
    }
}
