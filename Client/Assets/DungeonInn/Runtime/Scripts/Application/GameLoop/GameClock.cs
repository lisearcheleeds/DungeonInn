using System;
using DungeonInn.Domain.Common;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameClock : IGameClock
    {
        const float ScheduleTickSeconds = 1f;

        float scheduleAccumulatorSeconds;

        public int CurrentScheduleTick { get; private set; }
        public int CurrentDay { get; private set; }
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
                return new GameClockAdvanceResult(0, false);
            }

            var scaledDeltaSeconds = unscaledDeltaTimeSeconds * TimeScale;
            ElapsedGameTimeSeconds += scaledDeltaSeconds;
            scheduleAccumulatorSeconds += scaledDeltaSeconds;

            var advancedScheduleTicks = 0;
            var gameDateChanged = false;
            while (ScheduleTickSeconds <= scheduleAccumulatorSeconds)
            {
                scheduleAccumulatorSeconds -= ScheduleTickSeconds;
                CurrentScheduleTick++;
                advancedScheduleTicks++;

                var nextDay = CurrentScheduleTick / GameConstants.GameScheduleTicksPerDay;
                if (nextDay == CurrentDay)
                {
                    continue;
                }

                CurrentDay = nextDay;
                gameDateChanged = true;
            }

            return new GameClockAdvanceResult(advancedScheduleTicks, gameDateChanged);
        }
    }
}
