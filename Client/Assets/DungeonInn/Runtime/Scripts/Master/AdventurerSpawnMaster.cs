using System;

namespace DungeonInn.Master
{
    public sealed class AdventurerSpawnMaster
    {
        public int Id { get; }
        public string DisplayName { get; }
        public int ActorArchetypeId { get; }
        public bool SpawnOnce { get; }

        public AdventurerSpawnMaster(
            int id,
            string displayName,
            int actorArchetypeId,
            bool spawnOnce)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            if (actorArchetypeId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(actorArchetypeId));
            }

            Id = id;
            DisplayName = displayName;
            ActorArchetypeId = actorArchetypeId;
            SpawnOnce = spawnOnce;
        }
    }
}
