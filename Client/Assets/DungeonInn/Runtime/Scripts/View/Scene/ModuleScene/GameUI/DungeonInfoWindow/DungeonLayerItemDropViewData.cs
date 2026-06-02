using System;

namespace DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack
{
    public sealed class DungeonLayerItemDropViewData
    {
        public DungeonLayerItemDropViewData(
            string itemName,
            string sourceMonsterName,
            string chance,
            string countRange)
        {
            ItemName = string.IsNullOrWhiteSpace(itemName)
                ? throw new ArgumentException("Item name is required.", nameof(itemName))
                : itemName;
            SourceMonsterName = sourceMonsterName ?? string.Empty;
            Chance = chance ?? string.Empty;
            CountRange = countRange ?? string.Empty;
        }

        public string ItemName { get; }
        public string SourceMonsterName { get; }
        public string Chance { get; }
        public string CountRange { get; }
    }
}

