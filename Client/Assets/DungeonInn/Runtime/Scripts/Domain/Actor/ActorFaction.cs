using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorFaction
    {
        public int Id { get; }
        public string Name { get; }

        public ActorFaction(int id, string name)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Faction name is required.", nameof(name));
            }

            Id = id;
            Name = name;
        }
    }
}
