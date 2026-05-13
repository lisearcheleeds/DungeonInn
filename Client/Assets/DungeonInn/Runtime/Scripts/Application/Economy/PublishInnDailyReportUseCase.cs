using DungeonInn.Application.World;
using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Guild;
using VContainer;

namespace DungeonInn.Application.Economy
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
            var dailyReport = calculator.CalculateDailyReport(
                worldState,
                day,
                statisticsService.GetByDay(day));

            reportStore.Save(dailyReport);
            eventPublisher.Publish(new DailyInnReportGenerated(dailyReport));
            return UniTask.FromResult(dailyReport);
        }
    }
}
