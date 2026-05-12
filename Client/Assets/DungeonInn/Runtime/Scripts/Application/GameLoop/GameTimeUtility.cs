using System;
using System.Collections.Generic;
using DungeonInn.Domain.Common;

namespace DungeonInn.Application.GameLoop
{
    public static class GameTimeUtility
    {
        public static int GetDay(int totalScheduleTick)
        {
            return Math.Max(0, totalScheduleTick) / GameConstants.GameScheduleTicksPerDay;
        }

        public static int GetTickOfDay(int totalScheduleTick)
        {
            return Math.Max(0, totalScheduleTick) % GameConstants.GameScheduleTicksPerDay;
        }

        public static int GetDayStartTick(int day)
        {
            return Math.Max(0, day) * GameConstants.GameScheduleTicksPerDay;
        }

        public static int GetDayEndTick(int day)
        {
            return GetDayStartTick(day + 1) - 1;
        }

        public static IReadOnlyList<int> GetCompletedDays(
            int previousTotalScheduleTick,
            int currentTotalScheduleTick)
        {
            if (currentTotalScheduleTick <= previousTotalScheduleTick)
            {
                return Array.Empty<int>();
            }

            var nextBoundaryTick = GetDayStartTick(GetDay(previousTotalScheduleTick) + 1);
            if (currentTotalScheduleTick < nextBoundaryTick)
            {
                return Array.Empty<int>();
            }

            var completedDays = new List<int>();
            while (nextBoundaryTick <= currentTotalScheduleTick)
            {
                completedDays.Add(GetDay(nextBoundaryTick) - 1);
                nextBoundaryTick += GameConstants.GameScheduleTicksPerDay;
            }

            return completedDays;
        }
    }
}
