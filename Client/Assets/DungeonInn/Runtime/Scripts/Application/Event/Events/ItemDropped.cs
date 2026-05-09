using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ItemDropped : IGameEvent
    {
        public Guid ActorId { get; }
        public Guid ItemInstanceId { get; }
        public int ItemId { get; }
        public string ItemName { get; }
        public LayerPosition Position { get; }

        public ItemDropped(Guid actorId, Guid itemInstanceId, int itemId, string itemName, LayerPosition position)
        {
            ActorId = actorId;
            ItemInstanceId = itemInstanceId;
            ItemId = itemId;
            ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
            Position = position;
        }
    }
}
