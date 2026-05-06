using System;

namespace DungeonInn.Application.Combat
{
    public sealed class CombatDeathEvent
    {
        public Guid ActorId { get; }
        public string ActorName { get; }

        public CombatDeathEvent(Guid actorId, string actorName)
        {
            ActorId = actorId;
            ActorName = actorName ?? string.Empty;
        }
    }
}
