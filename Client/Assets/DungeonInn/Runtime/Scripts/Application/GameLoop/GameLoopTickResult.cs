using System.Collections.Generic;

namespace DungeonInn.Application.GameLoop
{
    public readonly struct GameLoopTickResult
    {
        public int TotalScheduleTick { get; }
        public int CurrentScheduleTick { get; }
        public int CurrentDay { get; }
        public int CurrentTickOfDay { get; }
        public int AdvancedScheduleTicks { get; }
        public IReadOnlyList<int> CompletedDays { get; }
        public bool DayBoundaryCrossed => CompletedDays != null && 0 < CompletedDays.Count;
        public float ElapsedRealTimeSeconds { get; }
        public float ElapsedGameTimeSeconds { get; }
        public float TimeScale { get; }
        public bool IsPaused { get; }

        public GameLoopTickResult(
            int totalScheduleTick,
            int advancedScheduleTicks,
            IReadOnlyList<int> completedDays,
            float elapsedRealTimeSeconds,
            float elapsedGameTimeSeconds,
            float timeScale,
            bool isPaused)
        {
            TotalScheduleTick = totalScheduleTick;
            CurrentScheduleTick = totalScheduleTick;
            CurrentDay = GameTimeUtility.GetDay(totalScheduleTick);
            CurrentTickOfDay = GameTimeUtility.GetTickOfDay(totalScheduleTick);
            AdvancedScheduleTicks = advancedScheduleTicks;
            CompletedDays = completedDays;
            ElapsedRealTimeSeconds = elapsedRealTimeSeconds;
            ElapsedGameTimeSeconds = elapsedGameTimeSeconds;
            TimeScale = timeScale;
            IsPaused = isPaused;
        }
    }
}
