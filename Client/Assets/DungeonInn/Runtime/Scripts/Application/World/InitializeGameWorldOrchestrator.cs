using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Event;
using DungeonInn.Application.Facilities;
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
        readonly IFacilityBuildingDefinitionRepository facilityBuildingDefinitionRepository;
        readonly FacilityBuildingRegistry facilityBuildingRegistry;

        [Inject]
        public InitializeGameWorldOrchestrator(
            IGameWorldState gameWorldState,
            InitializeWorldMapUseCase initializeWorldMapUseCase,
            InitializeDungeonOrchestrator initializeDungeonUseCase,
            IItemStackLimitResolver stackLimitResolver,
            IEventPublisher eventPublisher,
            IWorldGameSettingsRepository worldGameSettingsRepository,
            IFacilityBuildingDefinitionRepository facilityBuildingDefinitionRepository,
            FacilityBuildingRegistry facilityBuildingRegistry)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.initializeWorldMapUseCase = initializeWorldMapUseCase ?? throw new ArgumentNullException(nameof(initializeWorldMapUseCase));
            this.initializeDungeonUseCase = initializeDungeonUseCase ?? throw new ArgumentNullException(nameof(initializeDungeonUseCase));
            this.stackLimitResolver = stackLimitResolver ?? throw new ArgumentNullException(nameof(stackLimitResolver));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.worldGameSettingsRepository = worldGameSettingsRepository
                ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.facilityBuildingDefinitionRepository = facilityBuildingDefinitionRepository
                ?? throw new ArgumentNullException(nameof(facilityBuildingDefinitionRepository));
            this.facilityBuildingRegistry = facilityBuildingRegistry
                ?? throw new ArgumentNullException(nameof(facilityBuildingRegistry));
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
            RegisterFacilityBuildings(guild, groundMap.Layer);

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
            var facilities = new List<Facility>
            {
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.Inn,
                    "First Inn",
                    initialWorldSettings.InnBasePrice,
                    initialWorldSettings.InnCapacity,
                    CreateInventory())
            };

            facilities.Add(new Facility(
                Guid.NewGuid(),
                FacilityType.Tavern,
                "First Tavern",
                initialWorldSettings.GeneralStoreBasePrice,
                initialWorldSettings.ShopCapacity,
                CreateInventory()));

            facilities.AddRange(new[]
            {
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.GeneralStore,
                    "First General Store",
                    initialWorldSettings.GeneralStoreBasePrice,
                    initialWorldSettings.ShopCapacity,
                    CreateInventory(
                        new ItemStack(SpecialItemIds.Money, initialWorldSettings.GeneralStoreGold),
                        new ItemStack(SpecialItemIds.Potion, 20))),
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.EquipmentShop,
                    "First Equipment Shop",
                    initialWorldSettings.EquipmentShopBasePrice,
                    initialWorldSettings.ShopCapacity,
                    CreateInventory(
                        new ItemStack(SpecialItemIds.Money, initialWorldSettings.EquipmentShopGold),
                        new ItemStack(3001, 5),
                        new ItemStack(3002, 5),
                        new ItemStack(3004, 5),
                        new ItemStack(3012, 5)))
            });

            return new AdventurerGuild(Guid.NewGuid(), inventory, facilities);
        }

        void RegisterFacilityBuildings(AdventurerGuild guild, MapLayer groundLayer)
        {
            facilityBuildingRegistry.Clear();
            var definitions = facilityBuildingDefinitionRepository.GetDefinitions();
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                foreach (var facility in guild.Facilities)
                {
                    if (facility.Type != definition.FacilityType)
                    {
                        continue;
                    }

                    facilityBuildingRegistry.Register(new FacilityBuilding(
                        facility.Id,
                        facility.Type,
                        definition,
                        FacilityBuildingLayoutCalculator.CalculateInteractionPoint(definition, groundLayer)));
                    break;
                }
            }
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
