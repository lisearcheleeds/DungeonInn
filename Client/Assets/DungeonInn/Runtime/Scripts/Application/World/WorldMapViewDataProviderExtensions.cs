using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.World
{
    public static class WorldMapViewDataProviderExtensions
    {
        public static WorldMapLayerViewData GetLayerById(this IWorldMapViewDataProvider provider, int layerId)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            return provider.GetLayer(new MapLayerId(layerId));
        }
    }
}
