using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class CombatEncounterEnded : IGameEvent
    {
        public Guid ActorId { get; }

        public CombatEncounterEnded(Guid actorId)
        {
            ActorId = actorId;
        }
    }
}
