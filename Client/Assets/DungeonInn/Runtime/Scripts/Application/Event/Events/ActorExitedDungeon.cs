using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorExitedDungeon : IGameEvent
    {
        public Guid ActorId { get; }

        public ActorExitedDungeon(Guid actorId)
        {
            ActorId = actorId;
        }
    }
}
