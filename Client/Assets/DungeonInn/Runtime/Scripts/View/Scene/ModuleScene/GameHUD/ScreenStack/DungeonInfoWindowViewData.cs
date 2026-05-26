using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class DungeonInfoWindowViewData
    {
        public DungeonInfoWindowViewData(IReadOnlyList<DungeonLayerListItemViewData> layers)
        {
            Layers = (layers ?? Array.Empty<DungeonLayerListItemViewData>()).ToArray();
        }

        public IReadOnlyList<DungeonLayerListItemViewData> Layers { get; }
    }
}
