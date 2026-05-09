using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Item
{
    public sealed class ItemInstance
    {
        public Guid InstanceId { get; }
        public ItemStack Stack { get; }
        public LayerPosition Position { get; }

        public ItemInstance(Guid instanceId, ItemStack stack, LayerPosition position)
        {
            InstanceId = instanceId;
            Stack = stack;
            Position = position;
        }
    }
}
