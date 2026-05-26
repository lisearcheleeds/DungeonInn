using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.World;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class GuildCombinedInventoryViewService
    {
        readonly IGameWorldStateReader worldState;
        readonly IMasterRepository masterRepository;

        [Inject]
        public GuildCombinedInventoryViewService(
            IGameWorldStateReader worldState,
            IMasterRepository masterRepository)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public bool CanExecute => worldState.IsInitialized;

        public IReadOnlyList<GuildCombinedInventoryItemSummary> GetSnapshot()
        {
            var buffer = new List<GuildCombinedInventoryItemSummary>();
            Fill(buffer);
            return buffer;
        }

        public void Fill(List<GuildCombinedInventoryItemSummary> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.Clear();
            if (!CanExecute)
            {
                return;
            }

            // This is a UI/validation snapshot only; facility inventories remain separate domain state.
            var sourceByItemId = new Dictionary<int, List<GuildInventorySourceSummary>>();
            AddSourceItems(
                sourceByItemId,
                worldState.Guild.Id,
                "Guild",
                GuildInventorySourceType.Guild,
                null,
                worldState.Guild.Inventory);

            foreach (var facility in worldState.Guild.Facilities)
            {
                AddSourceItems(
                    sourceByItemId,
                    facility.Id,
                    facility.Name,
                    GuildInventorySourceType.Facility,
                    facility.Type,
                    facility.Inventory);
            }

            foreach (var kvp in sourceByItemId.OrderBy(x => x.Key))
            {
                var itemMaster = masterRepository.GetItemMaster(kvp.Key);
                buffer.Add(new GuildCombinedInventoryItemSummary(
                    itemMaster.Id,
                    itemMaster.Name,
                    itemMaster.Tags,
                    kvp.Value.Sum(x => x.Count),
                    kvp.Value));
            }
        }

        public int CountItem(int itemId)
        {
            if (!CanExecute)
            {
                return 0;
            }

            var total = Count(worldState.Guild.Inventory, itemId);
            foreach (var facility in worldState.Guild.Facilities)
            {
                total += Count(facility.Inventory, itemId);
            }

            return total;
        }

        public bool HasAll(IEnumerable<ItemStack> itemStacks)
        {
            var requiredCountByItemId = new Dictionary<int, int>();
            foreach (var itemStack in itemStacks ?? throw new ArgumentNullException(nameof(itemStacks)))
            {
                if (requiredCountByItemId.TryGetValue(itemStack.ItemId, out var count))
                {
                    requiredCountByItemId[itemStack.ItemId] = count + itemStack.Count;
                    continue;
                }

                requiredCountByItemId.Add(itemStack.ItemId, itemStack.Count);
            }

            foreach (var kvp in requiredCountByItemId)
            {
                if (CountItem(kvp.Key) < kvp.Value)
                {
                    return false;
                }
            }

            return true;
        }

        static void AddSourceItems(
            Dictionary<int, List<GuildInventorySourceSummary>> sourceByItemId,
            Guid sourceId,
            string sourceName,
            GuildInventorySourceType sourceType,
            FacilityType? facilityType,
            IReadOnlyInventory inventory)
        {
            foreach (var kvp in inventory.ItemCounts)
            {
                if (!sourceByItemId.TryGetValue(kvp.Key, out var sources))
                {
                    sources = new List<GuildInventorySourceSummary>();
                    sourceByItemId.Add(kvp.Key, sources);
                }

                sources.Add(new GuildInventorySourceSummary(
                    sourceType,
                    sourceId,
                    sourceName,
                    facilityType,
                    kvp.Value));
            }
        }

        static int Count(IReadOnlyInventory inventory, int itemId)
        {
            return inventory.ItemCounts.TryGetValue(itemId, out var count) ? count : 0;
        }
    }
}
