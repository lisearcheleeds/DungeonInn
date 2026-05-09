using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ItemPickedUp : IGameEvent
    {
        public Guid ActorId { get; }
        public ItemInstance ItemInstance { get; }

        public ItemPickedUp(Guid actorId, ItemInstance itemInstance)
        {
            ActorId = actorId;
            ItemInstance = itemInstance ?? throw new ArgumentNullException(nameof(itemInstance));
        }
    }
}
