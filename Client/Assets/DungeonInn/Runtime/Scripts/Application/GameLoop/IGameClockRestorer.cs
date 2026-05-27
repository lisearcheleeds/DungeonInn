namespace DungeonInn.Application.GameLoop
{
    public interface IGameClockRestorer
    {
        void Restore(
            int totalScheduleTick,
            float elapsedRealTimeSeconds,
            float elapsedGameTimeSeconds,
            float timeScale,
            bool isPaused);
    }
}
