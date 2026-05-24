namespace DungeonInn.View.Scene.Bridge
{
    public interface IActiveLayerProvider
    {
        int? ActiveLayerId { get; }
    }

    public interface IActiveLayerProviderRegistry
    {
        void Register(IActiveLayerProvider provider);
        void Unregister(IActiveLayerProvider provider);
    }
}
