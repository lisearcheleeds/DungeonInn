namespace DungeonInn.Application.GameLoop
{
    public interface IGameClock
    {
        int TotalScheduleTick { get; }
        int CurrentScheduleTick { get; }
        int CurrentDay { get; }
        int CurrentTickOfDay { get; }
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
