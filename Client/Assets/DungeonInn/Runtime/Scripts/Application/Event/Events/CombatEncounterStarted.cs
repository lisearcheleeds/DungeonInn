using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class CombatEncounterStarted : IGameEvent
    {
        public Guid ActorId { get; }
        public Guid TargetActorId { get; }

        public CombatEncounterStarted(Guid actorId, Guid targetActorId)
        {
            ActorId = actorId;
            TargetActorId = targetActorId;
        }
    }
}
