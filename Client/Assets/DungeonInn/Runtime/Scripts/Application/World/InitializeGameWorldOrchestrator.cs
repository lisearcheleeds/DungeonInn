using System;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Combat;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;

using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.World
{
    /// <summary>
    /// World シーン開始時に地上�EチE�E、ダンジョン、�E険老E��ルドを生�Eして GameWorldState へ格納するユースケース、E    /// </summary>
    public sealed class InitializeGameWorldOrchestrator
    {
        readonly IGameWorldState gameWorldState;
        readonly InitializeWorldMapUseCase initializeWorldMapUseCase;
        readonly InitializeDungeonOrchestrator initializeDungeonUseCase;
        readonly IItemStackLimitResolver stackLimitResolver;

        /// <summary>
        /// GameWorldState 初期化ユースケースを作�Eする、E        /// </summary>
        [Inject]
        public InitializeGameWorldOrchestrator(
            IGameWorldState gameWorldState,
            InitializeWorldMapUseCase initializeWorldMapUseCase,
            InitializeDungeonOrchestrator initializeDungeonUseCase,
            IItemStackLimitResolver stackLimitResolver)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.initializeWorldMapUseCase = initializeWorldMapUseCase ?? throw new ArgumentNullException(nameof(initializeWorldMapUseCase));
            this.initializeDungeonUseCase = initializeDungeonUseCase ?? throw new ArgumentNullException(nameof(initializeDungeonUseCase));
            this.stackLimitResolver = stackLimitResolver ?? throw new ArgumentNullException(nameof(stackLimitResolver));
        }

        /// <summary>
        /// GameWorldState が未初期化であれば初期地上�EチE�E、�E期ダンジョン、�E期ギルドを生�Eして格納する、E        /// </summary>
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
            var dungeon = await initializeDungeonUseCase.ExecuteAsync(request.DungeonSeed, request.DepthBandConfigs);
            var guild = CreateInitialGuild();

            gameWorldState.Initialize(guild, groundMap, dungeon);
            return gameWorldState;
        }

        AdventurerGuild CreateInitialGuild()
        {
            var inventory = new Inventory(
                GameConstants.InitialGuildInventorySlotCapacity,
                stackLimitResolver);
            inventory.AddRange(CreateInitialInventory());
            var facilities = new[]
            {
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.Inn,
                    "First Inn",
                    GameConstants.InitialInnBasePrice,
                    GameConstants.InitialInnCapacity,
                    CreateInventory()),
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.GeneralStore,
                    "First General Store",
                    GameConstants.InitialGeneralStoreBasePrice,
                    GameConstants.InitialShopCapacity,
                    CreateInventory(new ItemStack(SpecialItemIds.Money, GameConstants.InitialGeneralStoreGold))),
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.EquipmentShop,
                    "First Equipment Shop",
                    GameConstants.InitialEquipmentShopBasePrice,
                    GameConstants.InitialShopCapacity,
                    CreateInventory(new ItemStack(SpecialItemIds.Money, GameConstants.InitialEquipmentShopGold)))
            };

            return new AdventurerGuild(Guid.NewGuid(), inventory, facilities);
        }

        Inventory CreateInventory(params ItemStack[] items)
        {
            var inventory = new Inventory(
                GameConstants.InitialGuildInventorySlotCapacity,
                stackLimitResolver);
            inventory.AddRange(items);
            return inventory;
        }

        static IReadOnlyList<ItemStack> CreateInitialInventory()
        {
            return new[]
            {
                new ItemStack(SpecialItemIds.Money, GameConstants.InitialGuildReserveGold),
                new ItemStack(GameConstants.InitialRookieSwordItemId, GameConstants.InitialRookieSwordCount),
                new ItemStack(GameConstants.InitialRookieArmorItemId, GameConstants.InitialRookieArmorCount)
            };
        }
    }
}
