using DungeonInn.Domain.Map;

namespace DungeonInn.Application.World
{
    public interface IWorldMapViewDataProvider
    {
        WorldMapLayerViewData GetLayer(MapLayerId layerId);
        void InvalidateLayer(MapLayerId layerId);
    }
}
