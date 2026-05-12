using System;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameClock : IGameClock
    {
        const float ScheduleTickSeconds = 1f;

        float scheduleAccumulatorSeconds;

        public int TotalScheduleTick { get; private set; }
        public int CurrentScheduleTick => TotalScheduleTick;
        public int CurrentDay => GameTimeUtility.GetDay(TotalScheduleTick);
        public int CurrentTickOfDay => GameTimeUtility.GetTickOfDay(TotalScheduleTick);
        public float ElapsedRealTimeSeconds { get; private set; }
        public float ElapsedGameTimeSeconds { get; private set; }
        public float TimeScale { get; private set; } = 1f;
        public bool IsPaused { get; private set; }

        public void SetTimeScale(float timeScale)
        {
            if (timeScale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeScale));
            }

            TimeScale = timeScale;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds)
        {
            if (unscaledDeltaTimeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTimeSeconds));
            }

            ElapsedRealTimeSeconds += unscaledDeltaTimeSeconds;
            if (IsPaused)
            {
                return new GameClockAdvanceResult(0, Array.Empty<int>());
            }

            var scaledDeltaSeconds = unscaledDeltaTimeSeconds * TimeScale;
            ElapsedGameTimeSeconds += scaledDeltaSeconds;
            scheduleAccumulatorSeconds += scaledDeltaSeconds;

            var previousTotalScheduleTick = TotalScheduleTick;
            var advancedScheduleTicks = 0;
            while (ScheduleTickSeconds <= scheduleAccumulatorSeconds)
            {
                scheduleAccumulatorSeconds -= ScheduleTickSeconds;
                TotalScheduleTick++;
                advancedScheduleTicks++;
            }

            return new GameClockAdvanceResult(
                advancedScheduleTicks,
                GameTimeUtility.GetCompletedDays(
                    previousTotalScheduleTick,
                    TotalScheduleTick));
        }
    }
}
