using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Item
{
    public sealed class ItemInstance
    {
        public Guid InstanceId { get; }
        public int ItemId { get; }
        public LayerPosition Position { get; }

        public ItemInstance(Guid instanceId, int itemId, LayerPosition position)
        {
            InstanceId = instanceId;
            ItemId = itemId;
            Position = position;
        }
    }
}
