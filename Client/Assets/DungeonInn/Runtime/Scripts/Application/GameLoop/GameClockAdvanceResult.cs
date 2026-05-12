using System.Collections.Generic;

namespace DungeonInn.Application.GameLoop
{
    public readonly struct GameClockAdvanceResult
    {
        public int AdvancedScheduleTicks { get; }
        public IReadOnlyList<int> CompletedDays { get; }
        public bool DayBoundaryCrossed => CompletedDays != null && 0 < CompletedDays.Count;

        public GameClockAdvanceResult(
            int advancedScheduleTicks,
            IReadOnlyList<int> completedDays)
        {
            AdvancedScheduleTicks = advancedScheduleTicks;
            CompletedDays = completedDays;
        }
    }
}
