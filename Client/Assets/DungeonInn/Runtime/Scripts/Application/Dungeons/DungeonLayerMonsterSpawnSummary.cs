using System;

namespace DungeonInn.Application.Dungeons
{
    public sealed class DungeonLayerMonsterSpawnSummary
    {
        public DungeonLayerMonsterSpawnSummary(
            int actorArchetypeId,
            string monsterName,
            int weight,
            int minLevel,
            int maxLevel)
        {
            if (actorArchetypeId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(actorArchetypeId));
            }

            if (string.IsNullOrWhiteSpace(monsterName))
            {
                throw new ArgumentException("Monster name is required.", nameof(monsterName));
            }

            ActorArchetypeId = actorArchetypeId;
            MonsterName = monsterName;
            Weight = Math.Max(0, weight);
            MinLevel = Math.Max(1, minLevel);
            MaxLevel = Math.Max(MinLevel, maxLevel);
        }

        public int ActorArchetypeId { get; }
        public string MonsterName { get; }
        public int Weight { get; }
        public int MinLevel { get; }
        public int MaxLevel { get; }
    }
}
