namespace DungeonInn.Application.GameLoop
{
    public interface IGameClock
    {
        int CurrentScheduleTick { get; }
        int CurrentDay { get; }
        float ElapsedRealTimeSeconds { get; }
        float ElapsedGameTimeSeconds { get; }
        float TimeScale { get; }
        bool IsPaused { get; }

        void SetTimeScale(float timeScale);
        void Pause();
        void Resume();
        GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds);
    }
}
