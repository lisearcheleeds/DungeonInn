using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    /// <summary>
    /// World シーン開始時に地上マップ、ダンジョン、冒険者ギルドを生成して GameWorldState へ格納するユースケース。
    /// </summary>
    public sealed class InitializeGameWorldUseCase
    {
        readonly IGameWorldState gameWorldState;
        readonly InitializeWorldMapUseCase initializeWorldMapUseCase;
        readonly InitializeDungeonUseCase initializeDungeonUseCase;

        /// <summary>
        /// GameWorldState 初期化ユースケースを作成する。
        /// </summary>
        [Inject]
        public InitializeGameWorldUseCase(
            IGameWorldState gameWorldState,
            InitializeWorldMapUseCase initializeWorldMapUseCase,
            InitializeDungeonUseCase initializeDungeonUseCase)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.initializeWorldMapUseCase = initializeWorldMapUseCase ?? throw new ArgumentNullException(nameof(initializeWorldMapUseCase));
            this.initializeDungeonUseCase = initializeDungeonUseCase ?? throw new ArgumentNullException(nameof(initializeDungeonUseCase));
        }

        /// <summary>
        /// GameWorldState が未初期化であれば初期地上マップ、初期ダンジョン、初期ギルドを生成して格納する。
        /// </summary>
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

        static AdventurerGuild CreateInitialGuild()
        {
            var inventory = new Inventory();
            inventory.AddRange(CreateInitialInventory());
            var facilities = new[]
            {
                new Facility(
                    Guid.NewGuid(),
                    FacilityType.Inn,
                    "First Inn",
                    GameConstants.InitialInnBasePrice,
                    GameConstants.InitialInnCapacity)
            };

            return new AdventurerGuild(Guid.NewGuid(), inventory, facilities);
        }

        static IReadOnlyList<ItemStack> CreateInitialInventory()
        {
            return new[]
            {
                new ItemStack(SpecialItemIds.Money, GameConstants.InitialGuildGold),
                new ItemStack(3001, GameConstants.InitialRookieSwordCount),
                new ItemStack(3003, GameConstants.InitialRookieArmorCount)
            };
        }
    }
}
