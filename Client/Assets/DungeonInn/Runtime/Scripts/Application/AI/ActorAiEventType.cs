namespace DungeonInn.Application.AI
{
    public enum ActorAiEventType
    {
        HealthBandChanged,
        EnemyEnteredRange,
        EnteredDungeon,
        ObjectiveItemCountChanged,
        ObjectiveMonsterDefeated,
        DayBoundaryCrossed,
        CurrentActionFailed,
        CurrentActionCompleted
    }
}
