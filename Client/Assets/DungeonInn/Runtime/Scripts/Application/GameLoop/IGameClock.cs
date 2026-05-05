namespace DungeonInn.Application.GameLoop
{
    public interface IGameClock
    {
        int CurrentScheduleTick { get; }
        int CurrentDay { get; }
        float ElapsedRealTimeSeconds { get; }
        float ElapsedGameTimeSeconds { get; }
        float TimeScale { get; }

        void SetTimeScale(float timeScale);
        GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds);
    }
}
