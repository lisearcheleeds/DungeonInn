using System;

namespace DungeonInn.View.Scene.Bridge
{
    public sealed class ActiveLayerProviderProxy :
        IActiveLayerProvider,
        IActiveLayerProviderRegistry
    {
        IActiveLayerProvider current;

        public int? ActiveLayerId => current?.ActiveLayerId;

        public void Register(IActiveLayerProvider provider)
        {
            if (current != null)
            {
                throw new InvalidOperationException("Duplicate active layer provider registration.");
            }

            current = provider;
        }

        public void Unregister(IActiveLayerProvider provider)
        {
            if (ReferenceEquals(current, provider))
            {
                current = null;
            }
        }
    }
}
