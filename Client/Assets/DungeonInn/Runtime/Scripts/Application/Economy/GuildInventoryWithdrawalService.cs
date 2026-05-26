using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class GuildInventoryWithdrawalService
    {
        readonly IGameWorldStateReader worldState;
        readonly GuildCombinedInventoryViewService combinedInventoryViewService;

        [Inject]
        public GuildInventoryWithdrawalService(
            IGameWorldStateReader worldState,
            GuildCombinedInventoryViewService combinedInventoryViewService)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.combinedInventoryViewService =
                combinedInventoryViewService ?? throw new ArgumentNullException(nameof(combinedInventoryViewService));
        }

        public GuildInventoryWithdrawalResult Withdraw(IReadOnlyList<ItemStack> costs)
        {
            if (!combinedInventoryViewService.HasAll(costs))
            {
                throw new InvalidOperationException("Guild combined inventory does not satisfy required items.");
            }

            var sources = new List<GuildInventoryWithdrawalSource>();
            foreach (var cost in costs)
            {
                var remaining = cost.Count;
                remaining = WithdrawFrom(
                    worldState.Guild.Id,
                    "Guild",
                    null,
                    (IExchangeParticipant)worldState.Guild,
                    worldState.Guild.Inventory,
                    cost.ItemId,
                    remaining,
                    sources);

                foreach (var facility in worldState.Guild.Facilities)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }

                    remaining = WithdrawFrom(
                        facility.Id,
                        facility.Name,
                        facility.Type,
                        (IExchangeParticipant)facility,
                        facility.Inventory,
                        cost.ItemId,
                        remaining,
                        sources);
                }
            }

            return new GuildInventoryWithdrawalResult(sources);
        }

        static int WithdrawFrom(
            Guid sourceId,
            string sourceName,
            FacilityType? facilityType,
            IExchangeParticipant participant,
            IReadOnlyInventory inventory,
            int itemId,
            int remaining,
            List<GuildInventoryWithdrawalSource> sources)
        {
            if (remaining <= 0 ||
                !inventory.ItemCounts.TryGetValue(itemId, out var available) ||
                available <= 0)
            {
                return remaining;
            }

            var count = Math.Min(available, remaining);
            var stack = new ItemStack(itemId, count);
            participant.Remove(stack);
            sources.Add(new GuildInventoryWithdrawalSource(sourceId, sourceName, facilityType, stack));
            return remaining - count;
        }
    }
}
