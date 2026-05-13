using System.Collections.Generic;
using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.Economy
{
    public sealed class InnDailyReportStore
    {
        readonly Dictionary<int, InnDailyReport> reportsByDay = new();

        public void Save(InnDailyReport report)
        {
            reportsByDay[report.Day] = report;
        }

        public bool TryGet(int day, out InnDailyReport report)
        {
            return reportsByDay.TryGetValue(day, out report);
        }

        public IReadOnlyList<InnDailyReport> GetRange(int startDay, int endDay)
        {
            var reports = new List<InnDailyReport>();
            for (var day = startDay; day <= endDay; day++)
            {
                if (reportsByDay.TryGetValue(day, out var report))
                {
                    reports.Add(report);
                }
            }

            return reports;
        }
    }
}
