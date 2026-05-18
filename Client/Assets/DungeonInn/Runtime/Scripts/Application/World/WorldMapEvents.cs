using DungeonInn.Application.Event;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.World
{
    public sealed class MapLayerAddedEvent : IGameEvent
    {
        public MapLayerAddedEvent(MapLayerId layerId)
        {
            LayerId = layerId;
        }

        public MapLayerId LayerId { get; }
    }

    public sealed class DungeonFloorRegeneratedEvent : IGameEvent
    {
        public DungeonFloorRegeneratedEvent(MapLayerId layerId)
        {
            LayerId = layerId;
        }

        public MapLayerId LayerId { get; }
    }
}
