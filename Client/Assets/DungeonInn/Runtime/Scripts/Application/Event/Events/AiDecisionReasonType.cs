namespace DungeonInn.Application.Event.Events
{
    public enum AiDecisionReasonType
    {
        None = 0,
        GoalCompleted = 1,
        CriticalHp = 2,
        LowHpWithoutRecoveryItem = 3,
        NoVacantInnRoom = 4,
        LowHpWithRecoveryItem = 5,
        NearestHostileInRange = 6,
        CombatPowerMatchesFloor = 7,
        FallbackToLowestFloor = 8,
        FacilityUsageRequest = 9
    }
}
