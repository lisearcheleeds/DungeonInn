using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.Event.Events
{
    public sealed class DailyInnReportGenerated : IGameEvent
    {
        public InnDailyReport Report { get; }

        public DailyInnReportGenerated(InnDailyReport report)
        {
            Report = report;
        }
    }
}
