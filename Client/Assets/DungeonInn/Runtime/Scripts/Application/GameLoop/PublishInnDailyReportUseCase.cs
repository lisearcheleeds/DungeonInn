using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Guild;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class PublishInnDailyReportUseCase
    {
        readonly IGameWorldStateReader worldState;
        readonly IGameEventHistoryReader historyReader;
        readonly IEventPublisher eventPublisher;
        readonly InnEconomyStatusCalculator calculator;

        [Inject]
        public PublishInnDailyReportUseCase(
            IGameWorldStateReader worldState,
            IGameEventHistoryReader historyReader,
            IEventPublisher eventPublisher,
            InnEconomyStatusCalculator calculator)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.historyReader = historyReader ?? throw new ArgumentNullException(nameof(historyReader));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        public UniTask<InnDailyReport> ExecuteAsync(int day)
        {
            var report = calculator.CalculateReport(
                worldState,
                day,
                day,
                historyReader.GetByDay(day));
            var dailyReport = new InnDailyReport(
                report.StartDay,
                report.Guests,
                report.RejectedGuests,
                report.Demand,
                report.Sales,
                report.SatisfactionDelta,
                report.Reputation,
                report.OccupiedRooms,
                report.RoomCapacity,
                report.OccupancyPercent,
                report.GuildGold,
                report.RookieSwordStock,
                report.RookieArmorStock);

            eventPublisher.Publish(new DailyInnReportGenerated(dailyReport));
            return UniTask.FromResult(dailyReport);
        }
    }
}
