namespace DungeonInn.Application.Event
{
    public interface IGameEventHistoryRecorder
    {
        void Record(IGameEvent gameEvent);
    }
}
