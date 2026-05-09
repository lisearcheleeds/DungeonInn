using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.Event.Events
{
    public sealed class EquipmentChanged : IGameEvent
    {
        public Guid ActorId { get; }
        public EquipmentSlot Slot { get; }
        public int? PreviousItemId { get; }
        public int NewItemId { get; }

        public EquipmentChanged(Guid actorId, EquipmentSlot slot, int? previousItemId, int newItemId)
        {
            ActorId = actorId;
            Slot = slot;
            PreviousItemId = previousItemId;
            NewItemId = newItemId;
        }
    }
}
