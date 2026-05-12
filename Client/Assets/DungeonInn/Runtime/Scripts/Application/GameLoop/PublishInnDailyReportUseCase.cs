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
        readonly InnEconomyStatisticsService statisticsService;
        readonly InnDailyReportStore reportStore;
        readonly IEventPublisher eventPublisher;
        readonly InnEconomyStatusCalculator calculator;

        [Inject]
        public PublishInnDailyReportUseCase(
            IGameWorldStateReader worldState,
            InnEconomyStatisticsService statisticsService,
            InnDailyReportStore reportStore,
            IEventPublisher eventPublisher,
            InnEconomyStatusCalculator calculator)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            this.reportStore = reportStore ?? throw new ArgumentNullException(nameof(reportStore));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        public UniTask<InnDailyReport> ExecuteAsync(int day)
        {
            var report = calculator.CalculateReport(
                worldState,
                day,
                day,
                statisticsService.GetByDay(day));
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

            reportStore.Save(dailyReport);
            eventPublisher.Publish(new DailyInnReportGenerated(dailyReport));
            return UniTask.FromResult(dailyReport);
        }
    }
}
