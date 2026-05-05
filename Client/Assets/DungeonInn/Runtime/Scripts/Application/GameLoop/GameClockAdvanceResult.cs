namespace DungeonInn.Application.GameLoop
{
    public sealed class GameClockAdvanceResult
    {
        public int AdvancedScheduleTicks { get; }
        public bool GameDateChanged { get; }

        public GameClockAdvanceResult(int advancedScheduleTicks, bool gameDateChanged)
        {
            AdvancedScheduleTicks = advancedScheduleTicks;
            GameDateChanged = gameDateChanged;
        }
    }
}
