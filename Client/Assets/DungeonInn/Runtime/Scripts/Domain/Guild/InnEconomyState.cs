using System;
using DungeonInn.Domain.Common;

namespace DungeonInn.Domain.Guild
{
    public sealed class InnEconomyState
    {
        public int Reputation { get; private set; } = GameConstants.InitialInnReputation;
        public int TodayGuests { get; private set; }
        public int TodayRejectedGuests { get; private set; }
        public int TodaySales { get; private set; }
        public int TodaySatisfactionDelta { get; private set; }
        public InnDailyReport LastDailyReport { get; private set; }

        public void RecordStayedGuest(int sales, int satisfactionDelta)
        {
            TodayGuests++;
            TodaySales += Math.Max(0, sales);
            TodaySatisfactionDelta += satisfactionDelta;
        }

        public void RecordRejectedGuest(int satisfactionDelta)
        {
            TodayRejectedGuests++;
            TodaySatisfactionDelta += satisfactionDelta;
        }

        public InnDailyReport CloseDay(
            int day,
            int occupiedRooms,
            int roomCapacity,
            int guildGold,
            int rookieSwordStock,
            int rookieArmorStock)
        {
            var reputationDelta = TodaySatisfactionDelta / GameConstants.InnDailyReputationSatisfactionUnit;
            Reputation = Math.Max(0, Reputation + reputationDelta);
            var occupancyPercent = roomCapacity <= 0 ? 0 : occupiedRooms * 100 / roomCapacity;
            var report = new InnDailyReport(
                day,
                TodayGuests,
                TodayRejectedGuests,
                TodayGuests + TodayRejectedGuests,
                TodaySales,
                TodaySatisfactionDelta,
                Reputation,
                occupiedRooms,
                roomCapacity,
                occupancyPercent,
                guildGold,
                rookieSwordStock,
                rookieArmorStock);

            LastDailyReport = report;
            TodayGuests = 0;
            TodayRejectedGuests = 0;
            TodaySales = 0;
            TodaySatisfactionDelta = 0;
            return report;
        }
    }
}
