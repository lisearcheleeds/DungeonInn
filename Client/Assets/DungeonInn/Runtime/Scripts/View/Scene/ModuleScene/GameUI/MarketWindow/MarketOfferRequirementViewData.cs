using System;

namespace DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack
{
    public sealed class MarketOfferRequirementViewData
    {
        public MarketOfferRequirementViewData(string itemName, string count, string missing)
        {
            ItemName = string.IsNullOrWhiteSpace(itemName)
                ? throw new ArgumentException("Item name is required.", nameof(itemName))
                : itemName;
            Count = count ?? string.Empty;
            Missing = missing ?? string.Empty;
        }

        public string ItemName { get; }
        public string Count { get; }
        public string Missing { get; }
    }
}

