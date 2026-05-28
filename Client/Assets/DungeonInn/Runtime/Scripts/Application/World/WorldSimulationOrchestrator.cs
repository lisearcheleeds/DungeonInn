using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Items;
using DungeonInn.Application.SaveLoad;
using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using DungeonInn.GameSession;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class WorldSimulationOrchestrator : IWorldSimulationOrchestrator
    {
        readonly IGameLoopUseCase gameLoopUseCase;
        readonly IGameRandom gameRandom;
        readonly IGameWorldState gameWorldState;
        readonly InitializeGameWorldOrchestrator initializeGameWorldUseCase;
        readonly SpawnScheduledAdventurerOrchestrator spawnScheduledAdventurerUseCase;
        readonly SpawnScheduledMonsterOrchestrator spawnScheduledMonsterUseCase;
        readonly AdvanceActorAiOrchestrator advanceActorAiOrchestrator;
        readonly AdvanceActorLifecycleOrchestrator advanceActorSimpleLifecycleUseCase;
        readonly DetectCombatEncounterUseCase detectCombatEncounterUseCase;
        readonly AdvanceCombatUseCase advanceCombatUseCase;
        readonly AdvanceProjectileUseCase advanceProjectileUseCase;
        readonly AdvanceAreaEffectUseCase advanceAreaEffectUseCase;
        readonly PickUpItemUseCase pickUpItemUseCase;
        readonly UpdateEquipmentUseCase updateEquipmentUseCase;
        readonly SellItemsUseCase sellItemsUseCase;
        readonly UseRecoveryItemOrchestrator useRecoveryItemUseCase;
        readonly AdvanceActorEffectsUseCase advanceActorEffectsUseCase;
        readonly DecideAdventurerReturnUseCase decideAdventurerReturnUseCase;
        readonly AdvanceInnRecoveryOrchestrator advanceInnRecoveryOrchestrator;
        readonly PublishInnDailyReportUseCase publishInnDailyReportUseCase;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly GameSessionStartRequestStore startRequestStore;
        readonly RestoreGameSaveSnapshotUseCase restoreGameSaveSnapshotUseCase;
        readonly ActiveSaveSlotService activeSaveSlotService;
        readonly Dictionary<int, float> realtimeMovedSecondsByLayer = new();
        readonly HashSet<int> scheduledActorLayerIds = new();

        int aiEvaluationFrameId;

        [Inject]
        public WorldSimulationOrchestrator(
            IGameLoopUseCase gameLoopUseCase,
            IGameRandom gameRandom,
            IGameWorldState gameWorldState,
            InitializeGameWorldOrchestrator initializeGameWorldUseCase,
            SpawnScheduledAdventurerOrchestrator spawnScheduledAdventurerUseCase,
            SpawnScheduledMonsterOrchestrator spawnScheduledMonsterUseCase,
            AdvanceActorAiOrchestrator advanceActorAiOrchestrator,
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
            AdvanceInnRecoveryOrchestrator advanceInnRecoveryOrchestrator,
            PublishInnDailyReportUseCase publishInnDailyReportUseCase,
            IWorldGameSettingsRepository worldGameSettingsRepository,
            GameSessionStartRequestStore startRequestStore,
            RestoreGameSaveSnapshotUseCase restoreGameSaveSnapshotUseCase,
            ActiveSaveSlotService activeSaveSlotService)
        {
            this.gameLoopUseCase = gameLoopUseCase ?? throw new ArgumentNullException(nameof(gameLoopUseCase));
            this.gameRandom = gameRandom ?? throw new ArgumentNullException(nameof(gameRandom));
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.initializeGameWorldUseCase = initializeGameWorldUseCase ?? throw new ArgumentNullException(nameof(initializeGameWorldUseCase));
            this.spawnScheduledAdventurerUseCase = spawnScheduledAdventurerUseCase ?? throw new ArgumentNullException(nameof(spawnScheduledAdventurerUseCase));
            this.spawnScheduledMonsterUseCase = spawnScheduledMonsterUseCase ?? throw new ArgumentNullException(nameof(spawnScheduledMonsterUseCase));
            this.advanceActorAiOrchestrator = advanceActorAiOrchestrator ?? throw new ArgumentNullException(nameof(advanceActorAiOrchestrator));
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
            this.advanceInnRecoveryOrchestrator = advanceInnRecoveryOrchestrator ?? throw new ArgumentNullException(nameof(advanceInnRecoveryOrchestrator));
            this.publishInnDailyReportUseCase = publishInnDailyReportUseCase ?? throw new ArgumentNullException(nameof(publishInnDailyReportUseCase));
            this.worldGameSettingsRepository = worldGameSettingsRepository
                ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.startRequestStore = startRequestStore
                ?? throw new ArgumentNullException(nameof(startRequestStore));
            this.restoreGameSaveSnapshotUseCase = restoreGameSaveSnapshotUseCase
                ?? throw new ArgumentNullException(nameof(restoreGameSaveSnapshotUseCase));
            this.activeSaveSlotService = activeSaveSlotService
                ?? throw new ArgumentNullException(nameof(activeSaveSlotService));
        }

        public async UniTask<WorldSimulationInitializeResult> InitializeAsync(CancellationToken cancellationToken)
        {
            await worldGameSettingsRepository.LoadAsync(cancellationToken);
            var startRequest = startRequestStore.Current;
            gameRandom.Initialize(startRequest.GameRandomSeed);
            await initializeGameWorldUseCase.ExecuteAsync(
                new InitializeGameWorldRequest(startRequest.DungeonSeed));

            if (startRequest.Mode == GameSessionStartMode.LoadGame)
            {
                restoreGameSaveSnapshotUseCase.Execute(startRequest.SaveData);
                if (startRequest.LoadSlotId.HasValue)
                {
                    activeSaveSlotService.SetActiveSlot(startRequest.LoadSlotId.Value);
                }
            }
            else
            {
                activeSaveSlotService.Clear();
            }

            cancellationToken.ThrowIfCancellationRequested();

            return new WorldSimulationInitializeResult(
                gameWorldState.Guild.Facilities.Count,
                gameWorldState.Dungeon.Floors.Count,
                gameWorldState.Actors.Count);
        }

        public async UniTask AdvanceFrameAsync(WorldFrameAdvanceRequest request)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            var result = await gameLoopUseCase.ExecuteAsync(new GameLoopTickRequest(request.UnscaledDeltaTimeSeconds));
            request.CancellationToken.ThrowIfCancellationRequested();
            var frameDeltaGameSeconds = result.IsPaused ? 0f : request.UnscaledDeltaTimeSeconds * result.TimeScale;
            var shouldAdvanceTimeDependentSystems = !result.IsPaused && 0f < frameDeltaGameSeconds;

            foreach (var completedDay in result.CompletedDays)
            {
                await publishInnDailyReportUseCase.ExecuteAsync(completedDay);
            }

            if (shouldAdvanceTimeDependentSystems &&
                request.RealtimeLayerId.HasValue &&
                HasActorOnLayer(request.RealtimeLayerId.Value))
            {
                await advanceActorSimpleLifecycleUseCase.ExecuteAsync(
                    gameWorldState,
                    frameDeltaGameSeconds,
                    ActorLifecycleAdvanceScope.Only(request.RealtimeLayerId.Value));
                AddRealtimeMovedSeconds(request.RealtimeLayerId.Value, frameDeltaGameSeconds);
                request.CancellationToken.ThrowIfCancellationRequested();
            }

            if (0 < result.AdvancedScheduleTicks)
            {
                await AdvanceScheduleSystemsAsync(
                    result.AdvancedScheduleTicks,
                    result.CurrentScheduleTick,
                    request.CancellationToken);
            }

            if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.Actors.Count)
            {
                aiEvaluationFrameId++;
                await advanceActorAiOrchestrator.ExecuteAsync(
                    gameWorldState.Actors,
                    result.ElapsedGameTimeSeconds,
                    aiEvaluationFrameId,
                    0f);
                request.CancellationToken.ThrowIfCancellationRequested();
            }

            if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.Actors.Count)
            {
                await detectCombatEncounterUseCase.ExecuteAsync(gameWorldState);
                request.CancellationToken.ThrowIfCancellationRequested();
                await advanceCombatUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                request.CancellationToken.ThrowIfCancellationRequested();
            }

            if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.Projectiles.Count)
            {
                await advanceProjectileUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                request.CancellationToken.ThrowIfCancellationRequested();
            }

            if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.AreaEffects.Count)
            {
                await advanceAreaEffectUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                request.CancellationToken.ThrowIfCancellationRequested();
            }

            if (0 < gameWorldState.Items.Count)
            {
                pickUpItemUseCase.Execute(gameWorldState);
            }
            if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.Actors.Count)
            {
                await advanceActorEffectsUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                request.CancellationToken.ThrowIfCancellationRequested();
            }

            if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.Actors.Count)
            {
                await advanceInnRecoveryOrchestrator.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                request.CancellationToken.ThrowIfCancellationRequested();
            }
        }

        async UniTask AdvanceScheduleSystemsAsync(
            int scheduleTicks,
            int currentScheduleTick,
            CancellationToken cancellationToken)
        {
            await spawnScheduledAdventurerUseCase.ExecuteAsync(gameWorldState, currentScheduleTick);
            cancellationToken.ThrowIfCancellationRequested();
            await spawnScheduledMonsterUseCase.ExecuteAsync(gameWorldState, currentScheduleTick);
            cancellationToken.ThrowIfCancellationRequested();

            await AdvanceScheduledActorLifecycleAsync(scheduleTicks);
            cancellationToken.ThrowIfCancellationRequested();
            await advanceInnRecoveryOrchestrator.EnsureReservationsAsync(gameWorldState, currentScheduleTick);
            cancellationToken.ThrowIfCancellationRequested();

            updateEquipmentUseCase.Execute(gameWorldState);
            sellItemsUseCase.Execute(gameWorldState);
            await useRecoveryItemUseCase.ExecuteAsync(gameWorldState);
            cancellationToken.ThrowIfCancellationRequested();
            await decideAdventurerReturnUseCase.ExecuteAsync(gameWorldState);
            cancellationToken.ThrowIfCancellationRequested();
        }

        async UniTask AdvanceScheduledActorLifecycleAsync(int scheduleTicks)
        {
            if (gameWorldState.Actors.Count == 0)
            {
                ConsumeRealtimeMovedSeconds(scheduleTicks);
                return;
            }

            scheduledActorLayerIds.Clear();
            foreach (var actor in gameWorldState.Actors)
            {
                scheduledActorLayerIds.Add(actor.Position.LayerId.Value);
            }

            foreach (var layerIdValue in scheduledActorLayerIds)
            {
                var layerId = new MapLayerId(layerIdValue);
                var consumedSeconds = GetRealtimeMovedSeconds(layerId);
                var deltaGameSeconds = Math.Max(0f, scheduleTicks - consumedSeconds);
                if (deltaGameSeconds <= 0f)
                {
                    continue;
                }

                await advanceActorSimpleLifecycleUseCase.ExecuteAsync(
                    gameWorldState,
                    deltaGameSeconds,
                    ActorLifecycleAdvanceScope.Only(layerId));
            }

            scheduledActorLayerIds.Clear();
            ConsumeRealtimeMovedSeconds(scheduleTicks);
        }

        bool HasActorOnLayer(MapLayerId layerId)
        {
            foreach (var actor in gameWorldState.Actors)
            {
                if (actor.Position.LayerId.Equals(layerId))
                {
                    return true;
                }
            }

            return false;
        }

        void AddRealtimeMovedSeconds(MapLayerId layerId, float deltaGameSeconds)
        {
            var layerIdValue = layerId.Value;
            realtimeMovedSecondsByLayer.TryGetValue(layerIdValue, out var current);
            realtimeMovedSecondsByLayer[layerIdValue] = current + deltaGameSeconds;
        }

        float GetRealtimeMovedSeconds(MapLayerId layerId)
        {
            realtimeMovedSecondsByLayer.TryGetValue(layerId.Value, out var value);
            return value;
        }

        void ConsumeRealtimeMovedSeconds(float consumedSeconds)
        {
            if (realtimeMovedSecondsByLayer.Count == 0)
            {
                return;
            }

            var layerIds = new List<int>(realtimeMovedSecondsByLayer.Keys);
            foreach (var layerId in layerIds)
            {
                var remainingSeconds = realtimeMovedSecondsByLayer[layerId] - consumedSeconds;
                if (remainingSeconds <= 0f)
                {
                    realtimeMovedSecondsByLayer.Remove(layerId);
                }
                else
                {
                    realtimeMovedSecondsByLayer[layerId] = remainingSeconds;
                }
            }
        }

    }
}
