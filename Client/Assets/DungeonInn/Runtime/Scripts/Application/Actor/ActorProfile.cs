using System;

namespace DungeonInn.Application.Profiles
{
    public sealed class ActorProfile
    {
        public Guid ActorId { get; }
        public string DisplayName { get; }
        public int MonsterSpeciesId { get; }

        public ActorProfile(Guid actorId, string displayName)
            : this(actorId, displayName, 0)
        {
        }

        public ActorProfile(Guid actorId, string displayName, int monsterSpeciesId)
        {
            ActorId = actorId;
            DisplayName = displayName ?? string.Empty;
            MonsterSpeciesId = Math.Max(0, monsterSpeciesId);
        }
    }
}
