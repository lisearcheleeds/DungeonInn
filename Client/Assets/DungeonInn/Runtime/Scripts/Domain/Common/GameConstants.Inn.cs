namespace DungeonInn.Domain.Common
{
    public static partial class GameConstants
    {
        public const float InnHpRecoveryPercentPerMinute = 1.0f;
        public const int InnFeePerStay = 10;
        public const int AdventurerInnWaitDepartureDays = 3;
        public const int InitialInnReputation = 10;
        public const int InnStayedSatisfactionDelta = 2;
        public const int InnWaitingSatisfactionDelta = -2;
        public const int InnCannotPaySatisfactionDelta = -1;
        public const int InnDailyReputationSatisfactionUnit = 5;
        public const int GuildRookieEquipmentMinimumStock = 8;
        public const int GuildRookieEquipmentRestockTarget = 20;
    }
}
