using System.Collections.Generic;

namespace DungeonInn.Application.Dungeons
{
    public interface IDungeonInfoScreenService
    {
        IReadOnlyList<DungeonLayerInfoSummary> GetLayers();
    }
}
