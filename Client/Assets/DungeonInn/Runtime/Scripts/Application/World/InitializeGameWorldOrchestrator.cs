using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class InitializeGameWorldOrchestrator
    {
        readonly IGameWorldState gameWorldState;
        readonly InitializeWorldMapUseCase initializeWorldMapUseCase;
        readonly InitializeDungeonOrchestrator initializeDungeonUseCase;
        readonly IItemStackLimitResolver stackLimitResolver;
        readonly IEventPublisher eventPublisher;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;

        [Inject]
        public InitializeGameWorldOrchestrator(
            IGameWorldState gameWorldState,
            InitializeWorldMapUseCase initializeWorldMapUseCase,
            InitializeDungeonOrchestrator initializeDungeonUseCase,
            IItemStackLimitResolver stackLimitResolver,
            IEventPublisher eventPublisher,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.initializeWorldMapUseCase = initializeWorldMapUseCase ?? throw new ArgumentNullException(nameof(initializeWorldMapUseCase));
            this.initializeDungeonUseCase = initializeDungeonUseCase ?? throw new ArgumentNullException(nameof(initializeDungeonUseCase));
            this.stackLimitResolver = stackLimitResolver ?? throw new ArgumentNullException(nameof(stackLimitResolver));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.worldGameSettingsRepository = worldGameSettingsRepository
                ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
        }

        public async UniTask<IGameWorldState> ExecuteAsync(InitializeGameWorldRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (gameWorldState.IsInitialized)
            {
                return gameWorldState;
            }

            var groundMap = await initializeWorldMapUseCase.ExecuteAsync();
            var dungeon = await initializeDungeonUseCase.ExecuteAsync(request.DungeonSeed);
            var guild = CreateInitialGuild();

            gameWorldState.Initialize(guild, groundMap, dungeon);
            eventPublisher.Publish(new MapLayerAddedEvent(MapLayerId.Ground));
            foreach (var floorIndex in dungeon.Floors.Keys)
            {
                eventPublisher.Publish(new MapLayerAddedEvent(MapLayerId.DungeonFloor(floorIndex)));
            }

            return gameWorldState;
        }

        AdventurerGuild CreateInitialGuild()
        {
            var initialWorldSettings = worldGameSettingsRepository.GetInitialWorldSettings();
            var inventory = new Inventory(
                initialWorldSettings.GuildInventorySlotCapacity,
                stackLimitResolver);
            inventory.AddRange(CreateInitialInventory());
            var facilities = new[]
            {
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.Inn,
                    "First Inn",
                    initialWorldSettings.InnBasePrice,
                    initialWorldSettings.InnCapacity,
                    CreateInventory()),
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.GeneralStore,
                    "First General Store",
                    initialWorldSettings.GeneralStoreBasePrice,
                    initialWorldSettings.ShopCapacity,
                    CreateInventory(new ItemStack(SpecialItemIds.Money, initialWorldSettings.GeneralStoreGold))),
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.EquipmentShop,
                    "First Equipment Shop",
                    initialWorldSettings.EquipmentShopBasePrice,
                    initialWorldSettings.ShopCapacity,
                    CreateInventory(new ItemStack(SpecialItemIds.Money, initialWorldSettings.EquipmentShopGold)))
            };

            return new AdventurerGuild(Guid.NewGuid(), inventory, facilities);
        }

        Inventory CreateInventory(params ItemStack[] items)
        {
            var initialWorldSettings = worldGameSettingsRepository.GetInitialWorldSettings();
            var inventory = new Inventory(
                initialWorldSettings.GuildInventorySlotCapacity,
                stackLimitResolver);
            inventory.AddRange(items);
            return inventory;
        }

        IReadOnlyList<ItemStack> CreateInitialInventory()
        {
            return worldGameSettingsRepository.GetInitialWorldSettings().InitialGuildInventory;
        }
    }
}
