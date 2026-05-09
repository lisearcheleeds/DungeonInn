using System;

namespace DungeonInn.Domain.Item
{
    public sealed class ActorDropEntry
    {
        public int ItemId { get; }
        public float Probability { get; }
        public int MinCount { get; }
        public int MaxCount { get; }

        public ActorDropEntry(int itemId, float probability, int minCount, int maxCount)
        {
            if (itemId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            if (probability < 0f || 1f < probability)
            {
                throw new ArgumentOutOfRangeException(nameof(probability));
            }

            if (minCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minCount));
            }

            if (maxCount < minCount)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            ItemId = itemId;
            Probability = probability;
            MinCount = minCount;
            MaxCount = maxCount;
        }
    }
}
