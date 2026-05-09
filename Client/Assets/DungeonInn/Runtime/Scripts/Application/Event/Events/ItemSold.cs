using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ItemSold : IGameEvent
    {
        public Guid ActorId { get; }
        public ItemStack Stack { get; }
        public int TotalPrice { get; }
        public int ActorGold { get; }

        public ItemSold(
            Guid actorId,
            ItemStack stack,
            int totalPrice,
            int actorGold)
        {
            ActorId = actorId;
            Stack = stack;
            TotalPrice = totalPrice;
            ActorGold = actorGold;
        }
    }
}
