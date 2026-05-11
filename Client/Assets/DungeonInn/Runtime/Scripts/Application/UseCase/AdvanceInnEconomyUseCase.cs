using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceInnEconomyUseCase
    {
        readonly IItemMasterRepository masterRepository;
        readonly IEventPublisher eventBus;
        readonly InnEconomyStatusCalculator statusCalculator;

        [Inject]
        public AdvanceInnEconomyUseCase(
            IItemMasterRepository masterRepository,
            IEventPublisher eventBus,
            InnEconomyStatusCalculator statusCalculator)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.statusCalculator = statusCalculator ?? throw new ArgumentNullException(nameof(statusCalculator));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, int currentDay)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            ReplenishRookieEquipment(worldState, GameConstants.InitialRookieSwordItemId);
            ReplenishRookieEquipment(worldState, GameConstants.InitialRookieArmorItemId);

            var status = statusCalculator.Calculate(worldState, Math.Max(0, currentDay - 1));
            var report = worldState.InnEconomy.CloseDay(
                status.CurrentDay,
                status.OccupiedRooms,
                status.RoomCapacity,
                status.GuildGold,
                status.RookieSwordStock,
                status.RookieArmorStock);
            eventBus.Publish(new DailyInnReportGenerated(report));
            return UniTask.CompletedTask;
        }

        void ReplenishRookieEquipment(IGameWorldState worldState, int itemId)
        {
            var currentCount = CountItem(worldState, itemId);
            if (GameConstants.GuildRookieEquipmentMinimumStock <= currentCount)
            {
                return;
            }

            var addCount = GameConstants.GuildRookieEquipmentRestockTarget - currentCount;
            if (addCount <= 0)
            {
                return;
            }

            var itemMaster = masterRepository.GetItemMaster(itemId);
            var cost = itemMaster.BasePrice * addCount;
            if (!worldState.Guild.Inventory.TrySpendGold(cost))
            {
                return;
            }

            worldState.Guild.Inventory.Add(new ItemStack(itemId, addCount));
            eventBus.Publish(new GuildSupplyReplenished(itemId, addCount, cost, worldState.Guild.Inventory.Gold));
        }

        static int CountItem(IGameWorldState worldState, int itemId)
        {
            return worldState.Guild.Inventory.ItemCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

    }
}
