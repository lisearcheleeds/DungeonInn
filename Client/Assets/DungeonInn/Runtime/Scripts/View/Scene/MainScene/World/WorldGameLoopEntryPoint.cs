using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Orchestration;
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
        InitializeGameWorldOrchestrator initializeGameWorldUseCase;
        SpawnScheduledAdventurerOrchestrator spawnScheduledAdventurerUseCase;
        SpawnScheduledMonsterOrchestrator spawnScheduledMonsterUseCase;
        AdvanceActorLifecycleOrchestrator advanceActorSimpleLifecycleUseCase;
        DetectCombatEncounterUseCase detectCombatEncounterUseCase;
        AdvanceCombatUseCase advanceCombatUseCase;
        AdvanceProjectileUseCase advanceProjectileUseCase;
        AdvanceAreaEffectUseCase advanceAreaEffectUseCase;
        PickUpItemUseCase pickUpItemUseCase;
        UpdateEquipmentUseCase updateEquipmentUseCase;
        SellItemsUseCase sellItemsUseCase;
        UseRecoveryItemOrchestrator useRecoveryItemUseCase;
        AdvanceActorEffectsUseCase advanceActorEffectsUseCase;
        DecideAdventurerReturnUseCase decideAdventurerReturnUseCase;
        RecoverAdventurerAtInnUseCase recoverAdventurerAtInnUseCase;
        PublishInnDailyReportUseCase publishInnDailyReportUseCase;
        WorldActorDebugVisualizer worldActorDebugVisualizer;

        readonly CancellationTokenSource destroyCancellationTokenSource = new();

        bool isExecuting;
        bool isInitialized;

        [Inject]
        public void Construct(
            IGameLoopUseCase gameLoopUseCase,
            IGameWorldState gameWorldState,
            InitializeGameWorldOrchestrator initializeGameWorldUseCase,
            SpawnScheduledAdventurerOrchestrator spawnScheduledAdventurerUseCase,
            SpawnScheduledMonsterOrchestrator spawnScheduledMonsterUseCase,
            AdvanceActorLifecycleOrchestrator advanceActorSimpleLifecycleUseCase,
            DetectCombatEncounterUseCase detectCombatEncounterUseCase,
            AdvanceCombatUseCase advanceCombatUseCase,
            AdvanceProjectileUseCase advanceProjectileUseCase,
            AdvanceAreaEffectUseCase advanceAreaEffectUseCase,
            PickUpItemUseCase pickUpItemUseCase,
            UpdateEquipmentUseCase updateEquipmentUseCase,
            SellItemsUseCase sellItemsUseCase,
            UseRecoveryItemOrchestrator useRecoveryItemUseCase,
            AdvanceActorEffectsUseCase advanceActorEffectsUseCase,
            DecideAdventurerReturnUseCase decideAdventurerReturnUseCase,
            RecoverAdventurerAtInnUseCase recoverAdventurerAtInnUseCase,
            PublishInnDailyReportUseCase publishInnDailyReportUseCase,
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
            this.advanceProjectileUseCase = advanceProjectileUseCase ?? throw new ArgumentNullException(nameof(advanceProjectileUseCase));
            this.advanceAreaEffectUseCase = advanceAreaEffectUseCase ?? throw new ArgumentNullException(nameof(advanceAreaEffectUseCase));
            this.pickUpItemUseCase = pickUpItemUseCase ?? throw new ArgumentNullException(nameof(pickUpItemUseCase));
            this.updateEquipmentUseCase = updateEquipmentUseCase ?? throw new ArgumentNullException(nameof(updateEquipmentUseCase));
            this.sellItemsUseCase = sellItemsUseCase ?? throw new ArgumentNullException(nameof(sellItemsUseCase));
            this.useRecoveryItemUseCase = useRecoveryItemUseCase ?? throw new ArgumentNullException(nameof(useRecoveryItemUseCase));
            this.advanceActorEffectsUseCase = advanceActorEffectsUseCase ?? throw new ArgumentNullException(nameof(advanceActorEffectsUseCase));
            this.decideAdventurerReturnUseCase = decideAdventurerReturnUseCase ?? throw new ArgumentNullException(nameof(decideAdventurerReturnUseCase));
            this.recoverAdventurerAtInnUseCase = recoverAdventurerAtInnUseCase ?? throw new ArgumentNullException(nameof(recoverAdventurerAtInnUseCase));
            this.publishInnDailyReportUseCase = publishInnDailyReportUseCase ?? throw new ArgumentNullException(nameof(publishInnDailyReportUseCase));
            this.worldActorDebugVisualizer = worldActorDebugVisualizer ?? throw new ArgumentNullException(nameof(worldActorDebugVisualizer));
        }

        void Start()
        {
            Debug.Log("[WorldGameLoop] EntryPoint started.");
            InitializeAsync(destroyCancellationTokenSource.Token).Forget();
        }

        void OnDestroy()
        {
            destroyCancellationTokenSource.Cancel();
            destroyCancellationTokenSource.Dispose();
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

            TickAsync(destroyCancellationTokenSource.Token).Forget();
        }

        async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await initializeGameWorldUseCase.ExecuteAsync(
                new InitializeGameWorldRequest(
                    GameConstants.InitialDungeonSeed,
                    Array.Empty<DungeonDepthBandConfig>()));
                cancellationToken.ThrowIfCancellationRequested();
                isInitialized = true;
                Debug.Log(
                    $"[World] GameWorldState initialized. " +
                    $"Facilities={gameWorldState.Guild.Facilities.Count} " +
                    $"DungeonFloors={gameWorldState.Dungeon.Floors.Count} " +
                    $"Actors={gameWorldState.Actors.Count}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        async UniTask TickAsync(CancellationToken cancellationToken)
        {
            isExecuting = true;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var unscaledDeltaTime = Time.unscaledDeltaTime;
                var result = await gameLoopUseCase.ExecuteAsync(new GameLoopTickRequest(unscaledDeltaTime));
                cancellationToken.ThrowIfCancellationRequested();
                var frameDeltaGameSeconds = result.IsPaused ? 0f : unscaledDeltaTime * result.TimeScale;

                if (0 < result.AdvancedScheduleTicks)
                {
                    await spawnScheduledAdventurerUseCase.ExecuteAsync(gameWorldState, result.CurrentScheduleTick);
                    cancellationToken.ThrowIfCancellationRequested();
                    await spawnScheduledMonsterUseCase.ExecuteAsync(gameWorldState, result.CurrentScheduleTick);
                    cancellationToken.ThrowIfCancellationRequested();

                    var scheduleDeltaGameSeconds = result.AdvancedScheduleTicks;
                    await advanceActorSimpleLifecycleUseCase.ExecuteAsync(gameWorldState, scheduleDeltaGameSeconds);
                    cancellationToken.ThrowIfCancellationRequested();
                    await recoverAdventurerAtInnUseCase.EnsureReservationsAsync(gameWorldState, result.CurrentScheduleTick);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await detectCombatEncounterUseCase.ExecuteAsync(gameWorldState);
                cancellationToken.ThrowIfCancellationRequested();
                await advanceCombatUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                cancellationToken.ThrowIfCancellationRequested();
                await advanceProjectileUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                cancellationToken.ThrowIfCancellationRequested();
                await advanceAreaEffectUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                cancellationToken.ThrowIfCancellationRequested();
                pickUpItemUseCase.Execute(gameWorldState);
                updateEquipmentUseCase.Execute(gameWorldState);
                sellItemsUseCase.Execute(gameWorldState);
                await useRecoveryItemUseCase.ExecuteAsync(gameWorldState);
                cancellationToken.ThrowIfCancellationRequested();
                await advanceActorEffectsUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                cancellationToken.ThrowIfCancellationRequested();
                await decideAdventurerReturnUseCase.ExecuteAsync(gameWorldState);
                cancellationToken.ThrowIfCancellationRequested();
                await recoverAdventurerAtInnUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                cancellationToken.ThrowIfCancellationRequested();
                if (result.GameDateChanged)
                {
                    await publishInnDailyReportUseCase.ExecuteAsync(Math.Max(0, result.CurrentDay - 1));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                isExecuting = false;
            }
        }
    }
}
