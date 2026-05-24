using System;

namespace DungeonInn.Application.World
{
    public sealed class InnBalanceSettings
    {
        public float HpRecoveryPercentPerMinute { get; }
        public int FeePerStay { get; }
        public int AdventurerWaitDepartureDays { get; }
        public int StayedSatisfactionDelta { get; }
        public int WaitingSatisfactionDelta { get; }
        public int CannotPaySatisfactionDelta { get; }

        public InnBalanceSettings(
            float hpRecoveryPercentPerMinute,
            int feePerStay,
            int adventurerWaitDepartureDays,
            int stayedSatisfactionDelta,
            int waitingSatisfactionDelta,
            int cannotPaySatisfactionDelta)
        {
            HpRecoveryPercentPerMinute = Math.Max(0f, hpRecoveryPercentPerMinute);
            FeePerStay = Math.Max(0, feePerStay);
            AdventurerWaitDepartureDays = Math.Max(0, adventurerWaitDepartureDays);
            StayedSatisfactionDelta = stayedSatisfactionDelta;
            WaitingSatisfactionDelta = waitingSatisfactionDelta;
            CannotPaySatisfactionDelta = cannotPaySatisfactionDelta;
        }

        public static InnBalanceSettings CreateDefault()
        {
            return new InnBalanceSettings(1.0f, 10, 3, 2, -2, -1);
        }
    }
}
