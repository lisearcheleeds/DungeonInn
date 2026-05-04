using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Commerce
{
    public sealed class ExchangeTransaction
    {
        public Guid Id { get; }
        public Guid OurId { get; }
        public Guid TheirId { get; }
        public IReadOnlyList<ItemStack> OurGives { get; }
        public IReadOnlyList<ItemStack> TheirGives { get; }
        public int OccurredAtTick { get; }

        public ExchangeTransaction(
            Guid id,
            Guid ourId,
            Guid theirId,
            IReadOnlyList<ItemStack> ourGives,
            IReadOnlyList<ItemStack> theirGives,
            int occurredAtTick)
        {
            Id = id;
            OurId = ourId;
            TheirId = theirId;
            OurGives = ourGives ?? throw new ArgumentNullException(nameof(ourGives));
            TheirGives = theirGives ?? throw new ArgumentNullException(nameof(theirGives));
            OccurredAtTick = occurredAtTick;
        }
    }
}
