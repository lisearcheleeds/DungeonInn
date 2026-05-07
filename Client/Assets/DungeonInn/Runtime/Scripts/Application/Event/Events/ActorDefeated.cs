using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorDefeated : IGameEvent
    {
        public Guid ActorId { get; }
        public Guid? KillerActorId { get; }
        public DeathCause Cause { get; }

        public ActorDefeated(Guid actorId, Guid? killerActorId, DeathCause cause)
        {
            ActorId = actorId;
            KillerActorId = killerActorId;
            Cause = cause;
        }
    }
}
