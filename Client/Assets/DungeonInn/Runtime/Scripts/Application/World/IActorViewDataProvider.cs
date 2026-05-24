namespace DungeonInn.Application.World
{
    public interface IActorViewDataProvider
    {
        ActorViewDataChangeBuffer ConsumeChanges();
    }
}
