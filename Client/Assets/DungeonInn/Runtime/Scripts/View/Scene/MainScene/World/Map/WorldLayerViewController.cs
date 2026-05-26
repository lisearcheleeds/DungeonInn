using System;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldLayerViewController
    {
        readonly MapLayerViewRegistry layerViewRegistry;

        [Inject]
        public WorldLayerViewController(MapLayerViewRegistry layerViewRegistry)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public void SelectPreviousLayer()
        {
            layerViewRegistry.SelectPreviousLayer();
        }

        public void SelectNextLayer()
        {
            layerViewRegistry.SelectNextLayer();
        }
    }
}
