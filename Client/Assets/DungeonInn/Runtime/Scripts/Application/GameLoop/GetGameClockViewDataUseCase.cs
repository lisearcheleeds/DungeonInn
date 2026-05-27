using System;
using DungeonInn.Domain.Common;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GetGameClockViewDataUseCase
    {
        const int MinutesPerDay = 1440;
        const int MorningStartHour = 5;
        const int NoonStartHour = 12;
        const int EveningStartHour = 17;
        const int NightStartHour = 21;

        readonly IGameClock gameClock;

        [Inject]
        public GetGameClockViewDataUseCase(IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public GameClockViewData Execute()
        {
            var gameTotalMinutes =
                gameClock.CurrentTickOfDay * MinutesPerDay / GameConstants.GameScheduleTicksPerDay;
            var hours = gameTotalMinutes / 60;
            var minutes = gameTotalMinutes % 60;
            var period = ResolvePeriod(hours);

            return new GameClockViewData(
                $"Day {gameClock.CurrentDay + 1}  {hours:D2}:{minutes:D2}  {period}",
                period,
                gameClock.IsPaused);
        }

        static string ResolvePeriod(int hour)
        {
            if (hour < MorningStartHour)
            {
                return "Night";
            }

            if (hour < NoonStartHour)
            {
                return "Morning";
            }

            if (hour < EveningStartHour)
            {
                return "Noon";
            }

            if (hour < NightStartHour)
            {
                return "Evening";
            }

            return "Night";
        }
    }
}
