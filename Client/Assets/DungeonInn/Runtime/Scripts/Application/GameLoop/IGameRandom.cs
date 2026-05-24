namespace DungeonInn.Application.GameLoop
{
    public interface IGameRandom
    {
        void Initialize(int seed);
        int Next();
        int Next(int maxExclusive);
        int Next(int minInclusive, int maxExclusive);
    }
}
