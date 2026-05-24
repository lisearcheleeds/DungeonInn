using System;

namespace DungeonInn.Master
{
    public sealed class LevelTable
    {
        readonly int[] cumulativeXp;

        public int Id { get; }
        public int MaxLevel => cumulativeXp.Length - 1;

        public LevelTable(int id, int[] cumulativeXp)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            Id = id;
            this.cumulativeXp = cumulativeXp ?? throw new ArgumentNullException(nameof(cumulativeXp));
        }

        public int GetExperienceForLevel(int level)
        {
            if (level < 0 || MaxLevel < level)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            return cumulativeXp[level];
        }

        public int GetLevel(int xp)
        {
            var lo = 0;
            var hi = MaxLevel;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) / 2;
                if (cumulativeXp[mid] <= xp)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return lo;
        }
    }
}
