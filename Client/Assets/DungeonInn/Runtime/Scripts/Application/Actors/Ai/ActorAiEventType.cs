namespace DungeonInn.Application.Actors.Ai
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
