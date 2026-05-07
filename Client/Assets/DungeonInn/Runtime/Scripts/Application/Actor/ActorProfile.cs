using System;

namespace DungeonInn.Application.Profiles
{
    public sealed class ActorProfile
    {
        public Guid ActorId { get; }
        public string DisplayName { get; }

        public ActorProfile(Guid actorId, string displayName)
        {
            ActorId = actorId;
            DisplayName = displayName ?? string.Empty;
        }
    }
}
