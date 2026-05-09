using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ExperienceGranted : IGameEvent
    {
        public Guid ActorId { get; }
        public int GainedXp { get; }
        public int TotalXp { get; }

        public ExperienceGranted(Guid actorId, int gainedXp, int totalXp)
        {
            ActorId = actorId;
            GainedXp = gainedXp;
            TotalXp = totalXp;
        }
    }
}
