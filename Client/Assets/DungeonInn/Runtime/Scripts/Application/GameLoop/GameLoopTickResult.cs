namespace DungeonInn.Application.GameLoop
{
    public sealed class GameLoopTickResult
    {
        public int CurrentScheduleTick { get; }
        public int CurrentDay { get; }
        public int AdvancedScheduleTicks { get; }
        public bool GameDateChanged { get; }
        public float ElapsedRealTimeSeconds { get; }
        public float ElapsedGameTimeSeconds { get; }
        public float TimeScale { get; }

        public GameLoopTickResult(
            int currentScheduleTick,
            int currentDay,
            int advancedScheduleTicks,
            bool gameDateChanged,
            float elapsedRealTimeSeconds,
            float elapsedGameTimeSeconds,
            float timeScale)
        {
            CurrentScheduleTick = currentScheduleTick;
            CurrentDay = currentDay;
            AdvancedScheduleTicks = advancedScheduleTicks;
            GameDateChanged = gameDateChanged;
            ElapsedRealTimeSeconds = elapsedRealTimeSeconds;
            ElapsedGameTimeSeconds = elapsedGameTimeSeconds;
            TimeScale = timeScale;
        }
    }
}
