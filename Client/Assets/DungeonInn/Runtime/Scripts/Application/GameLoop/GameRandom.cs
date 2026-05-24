using System;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameRandom : IGameRandom
    {
        Random random = new(12345);

        public void Initialize(int seed)
        {
            random = new Random(seed);
        }

        public int Next()
        {
            return random.Next();
        }

        public int Next(int maxExclusive)
        {
            return random.Next(maxExclusive);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }
    }
}
