namespace DungeonInn.Core
{
    public interface IRebootCleanupRegistry
    {
        void Register(IRebootCleanupTarget target);
        void Unregister(IRebootCleanupTarget target);
    }
}
