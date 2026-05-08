using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldGameLoopEntryPoint : MonoBehaviour
    {
        IGameLoopUseCase gameLoopUseCase;
        IGameWorldState gameWorldState;
        InitializeGameWorldUseCase initializeGameWorldUseCase;
        SpawnScheduledAdventurerUseCase spawnScheduledAdventurerUseCase;
        SpawnScheduledMonsterUseCase spawnScheduledMonsterUseCase;
        AdvanceActorSimpleLifecycleUseCase advanceActorSimpleLifecycleUseCase;
        DetectCombatEncounterUseCase detectCombatEncounterUseCase;
        AdvanceCombatUseCase advanceCombatUseCase;
        DecideAdventurerReturnUseCase decideAdventurerReturnUseCase;
        WorldActorDebugVisualizer worldActorDebugVisualizer;

        bool isExecuting;
        bool isInitialized;

        [Inject]
        public void Construct(
            IGameLoopUseCase gameLoopUseCase,
            IGameWorldState gameWorldState,
            InitializeGameWorldUseCase initializeGameWorldUseCase,
            SpawnScheduledAdventurerUseCase spawnScheduledAdventurerUseCase,
            SpawnScheduledMonsterUseCase spawnScheduledMonsterUseCase,
            AdvanceActorSimpleLifecycleUseCase advanceActorSimpleLifecycleUseCase,
            DetectCombatEncounterUseCase detectCombatEncounterUseCase,
            AdvanceCombatUseCase advanceCombatUseCase,
            DecideAdventurerReturnUseCase decideAdventurerReturnUseCase,
            WorldActorDebugVisualizer worldActorDebugVisualizer)
        {
            this.gameLoopUseCase = gameLoopUseCase ?? throw new ArgumentNullException(nameof(gameLoopUseCase));
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.initializeGameWorldUseCase = initializeGameWorldUseCase ?? throw new ArgumentNullException(nameof(initializeGameWorldUseCase));
            this.spawnScheduledAdventurerUseCase = spawnScheduledAdventurerUseCase ?? throw new ArgumentNullException(nameof(spawnScheduledAdventurerUseCase));
            this.spawnScheduledMonsterUseCase = spawnScheduledMonsterUseCase ?? throw new ArgumentNullException(nameof(spawnScheduledMonsterUseCase));
            this.advanceActorSimpleLifecycleUseCase = advanceActorSimpleLifecycleUseCase ?? throw new ArgumentNullException(nameof(advanceActorSimpleLifecycleUseCase));
            this.detectCombatEncounterUseCase = detectCombatEncounterUseCase ?? throw new ArgumentNullException(nameof(detectCombatEncounterUseCase));
            this.advanceCombatUseCase = advanceCombatUseCase ?? throw new ArgumentNullException(nameof(advanceCombatUseCase));
            this.decideAdventurerReturnUseCase = decideAdventurerReturnUseCase ?? throw new ArgumentNullException(nameof(decideAdventurerReturnUseCase));
            this.worldActorDebugVisualizer = worldActorDebugVisualizer ?? throw new ArgumentNullException(nameof(worldActorDebugVisualizer));
        }

        void Start()
        {
            Debug.Log("[WorldGameLoop] EntryPoint started.");
            InitializeAsync().Forget();
        }

        void Update()
        {
            if (!isInitialized || gameLoopUseCase == null)
            {
                return;
            }

            worldActorDebugVisualizer.UpdateVisuals();

            if (isExecuting)
            {
                return;
            }

            TickAsync().Forget();
        }

        async UniTask InitializeAsync()
        {
            await initializeGameWorldUseCase.ExecuteAsync(
                new InitializeGameWorldRequest(
                    GameConstants.InitialDungeonSeed,
                    Array.Empty<DungeonDepthBandConfig>()));
            isInitialized = true;
            Debug.Log(
                $"[World] GameWorldState initialized. " +
                $"Facilities={gameWorldState.Guild.Facilities.Count} " +
                $"DungeonFloors={gameWorldState.Dungeon.Floors.Count} " +
                $"Actors={gameWorldState.Actors.Count}");
        }

        async UniTask TickAsync()
        {
            isExecuting = true;
            try
            {
                var unscaledDeltaTime = Time.unscaledDeltaTime;
                var result = await gameLoopUseCase.ExecuteAsync(new GameLoopTickRequest(unscaledDeltaTime));
                var frameDeltaGameSeconds = unscaledDeltaTime * result.TimeScale;

                if (0 < result.AdvancedScheduleTicks)
                {
                    await spawnScheduledAdventurerUseCase.ExecuteAsync(gameWorldState, result.CurrentScheduleTick);
                    await spawnScheduledMonsterUseCase.ExecuteAsync(gameWorldState, result.CurrentScheduleTick);

                    var scheduleDeltaGameSeconds = result.AdvancedScheduleTicks;
                    await advanceActorSimpleLifecycleUseCase.ExecuteAsync(gameWorldState, scheduleDeltaGameSeconds);
                }

                await detectCombatEncounterUseCase.ExecuteAsync(gameWorldState);
                await advanceCombatUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                await decideAdventurerReturnUseCase.ExecuteAsync(gameWorldState);
            }
            finally
            {
                isExecuting = false;
            }
        }
    }
}
