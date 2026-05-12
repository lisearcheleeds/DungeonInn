using System;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Commerce
{
    public interface IExchangeParticipant
    {
        Guid Id { get; }
        Inventory Inventory { get; }
    }
}
