namespace DungeonInn.Application.GameLoop
{
    public interface IGameRandom
    {
        int Next();
        int Next(int maxExclusive);
        int Next(int minInclusive, int maxExclusive);
    }
}
