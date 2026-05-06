using System;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameRandom : IGameRandom
    {
        readonly Random random;

        public GameRandom(int seed)
        {
            random = new Random(seed);
        }

        public int Next() => random.Next();
        public int Next(int maxExclusive) => random.Next(maxExclusive);
        public int Next(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);
    }
}
