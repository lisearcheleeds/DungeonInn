namespace DungeonInn.Application.GameLoop
{
    public readonly struct GameTimeState
    {
        public int TotalScheduleTick { get; }
        public int CurrentScheduleTick { get; }
        public int CurrentDay { get; }
        public int CurrentTickOfDay { get; }
        public float ElapsedRealTimeSeconds { get; }
        public float ElapsedGameTimeSeconds { get; }
        public float TimeScale { get; }
        public bool IsPaused { get; }

        public GameTimeState(
            int totalScheduleTick,
            float elapsedRealTimeSeconds,
            float elapsedGameTimeSeconds,
            float timeScale,
            bool isPaused)
        {
            TotalScheduleTick = totalScheduleTick;
            CurrentScheduleTick = totalScheduleTick;
            CurrentDay = GameTimeUtility.GetDay(totalScheduleTick);
            CurrentTickOfDay = GameTimeUtility.GetTickOfDay(totalScheduleTick);
            ElapsedRealTimeSeconds = elapsedRealTimeSeconds;
            ElapsedGameTimeSeconds = elapsedGameTimeSeconds;
            TimeScale = timeScale;
            IsPaused = isPaused;
        }
    }
}
