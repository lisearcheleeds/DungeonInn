using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Commerce
{
    public sealed class ExchangeOffer
    {
        public Guid Id { get; }
        public Guid FacilityId { get; }
        public IReadOnlyList<ItemStack> RequestedItems { get; }
        public IReadOnlyList<ItemStack> RewardItems { get; }
        public bool IsActive { get; private set; }

        public ExchangeOffer(
            Guid id,
            Guid facilityId,
            IReadOnlyList<ItemStack> requestedItems,
            IReadOnlyList<ItemStack> rewardItems)
        {
            if (requestedItems == null)
            {
                throw new ArgumentNullException(nameof(requestedItems));
            }

            if (requestedItems.Count == 0)
            {
                throw new ArgumentException("Exchange offer requires requested items.", nameof(requestedItems));
            }

            Id = id;
            FacilityId = facilityId;
            RequestedItems = requestedItems;
            RewardItems = rewardItems ?? throw new ArgumentNullException(nameof(rewardItems));
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
