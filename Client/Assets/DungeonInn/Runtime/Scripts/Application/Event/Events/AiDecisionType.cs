namespace DungeonInn.Application.Event.Events
{
    public enum AiDecisionType
    {
        None = 0,
        ReturnToInn = 1,
        WaitForInn = 2,
        UseRecoveryItem = 3,
        StartCombat = 4,
        SelectDungeonFloor = 5,
        UseFacility = 6
    }
}
