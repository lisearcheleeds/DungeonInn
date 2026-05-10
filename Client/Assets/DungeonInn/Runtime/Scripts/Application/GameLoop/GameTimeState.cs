namespace DungeonInn.Application.GameLoop
{
    public readonly struct GameTimeState
    {
        public int CurrentScheduleTick { get; }
        public int CurrentDay { get; }
        public float ElapsedRealTimeSeconds { get; }
        public float ElapsedGameTimeSeconds { get; }
        public float TimeScale { get; }
        public bool IsPaused { get; }

        public GameTimeState(
            int currentScheduleTick,
            int currentDay,
            float elapsedRealTimeSeconds,
            float elapsedGameTimeSeconds,
            float timeScale,
            bool isPaused)
        {
            CurrentScheduleTick = currentScheduleTick;
            CurrentDay = currentDay;
            ElapsedRealTimeSeconds = elapsedRealTimeSeconds;
            ElapsedGameTimeSeconds = elapsedGameTimeSeconds;
            TimeScale = timeScale;
            IsPaused = isPaused;
        }
    }
}
