using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Facility
{
    public sealed class FacilityUsageRequest
    {
        public FacilityUsageType UsageType { get; }
        public IReadOnlyList<ItemStack> PurchasedItems { get; }

        public FacilityUsageRequest(FacilityUsageType usageType, IReadOnlyList<ItemStack> purchasedItems)
        {
            UsageType = usageType;
            PurchasedItems = purchasedItems ?? throw new ArgumentNullException(nameof(purchasedItems));

            if ((usageType == FacilityUsageType.Rest || usageType == FacilityUsageType.Meal) && 0 < PurchasedItems.Count)
            {
                throw new ArgumentException("Rest and meal usage cannot include purchased items.", nameof(purchasedItems));
            }

            if ((usageType == FacilityUsageType.BuyItem || usageType == FacilityUsageType.BuyEquipment) && PurchasedItems.Count == 0)
            {
                throw new ArgumentException("Purchase usage requires purchased items.", nameof(purchasedItems));
            }
        }
    }
}
