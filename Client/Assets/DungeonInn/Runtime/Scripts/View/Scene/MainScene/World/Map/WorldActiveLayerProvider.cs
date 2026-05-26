using System;
using DungeonInn.View.Scene;
using DungeonInn.View.Scene.Bridge;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActiveLayerProvider : IActiveLayerProvider
    {
        readonly MapLayerViewRegistry layerViewRegistry;

        [Inject]
        public WorldActiveLayerProvider(MapLayerViewRegistry layerViewRegistry)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public int? ActiveLayerId => layerViewRegistry.ActiveLayerId?.Value;
    }
}
