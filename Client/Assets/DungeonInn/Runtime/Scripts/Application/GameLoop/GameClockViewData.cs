namespace DungeonInn.Application.GameLoop
{
    public readonly struct GameClockViewData
    {
        public string DayTimeText { get; }
        public string TimePeriodText { get; }
        public bool IsPaused { get; }

        public GameClockViewData(string dayTimeText, string timePeriodText, bool isPaused)
        {
            DayTimeText = dayTimeText;
            TimePeriodText = timePeriodText;
            IsPaused = isPaused;
        }
    }
}
