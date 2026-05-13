using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using DungeonInn.Application.Orchestration;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;

namespace DungeonInn.Application.GameLoop
{
    public sealed class WorldSimulationOrchestrator : IWorldSimulationOrchestrator
    {
        readonly IGameLoopUseCase gameLoopUseCase;
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
        readonly RecoverAdventurerAtInnUseCase recoverAdventurerAtInnUseCase;
        readonly PublishInnDailyReportUseCase publishInnDailyReportUseCase;

        int aiEvaluationFrameId;

        [Inject]
        public WorldSimulationOrchestrator(
            IGameLoopUseCase gameLoopUseCase,
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
            RecoverAdventurerAtInnUseCase recoverAdventurerAtInnUseCase,
            PublishInnDailyReportUseCase publishInnDailyReportUseCase)
        {
            this.gameLoopUseCase = gameLoopUseCase ?? throw new ArgumentNullException(nameof(gameLoopUseCase));
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
            this.recoverAdventurerAtInnUseCase = recoverAdventurerAtInnUseCase ?? throw new ArgumentNullException(nameof(recoverAdventurerAtInnUseCase));
            this.publishInnDailyReportUseCase = publishInnDailyReportUseCase ?? throw new ArgumentNullException(nameof(publishInnDailyReportUseCase));
        }

        public async UniTask<WorldSimulationInitializeResult> InitializeAsync(CancellationToken cancellationToken)
        {
            await initializeGameWorldUseCase.ExecuteAsync(
                new InitializeGameWorldRequest(
                    GameConstants.InitialDungeonSeed,
                    Array.Empty<DungeonDepthBandConfig>()));

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

            if (0 < result.AdvancedScheduleTicks)
            {
                await AdvanceScheduleSystemsAsync(result, request.CancellationToken);
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

            await detectCombatEncounterUseCase.ExecuteAsync(gameWorldState);
            request.CancellationToken.ThrowIfCancellationRequested();
            if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.Actors.Count)
            {
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

            updateEquipmentUseCase.Execute(gameWorldState);
            sellItemsUseCase.Execute(gameWorldState);
            await useRecoveryItemUseCase.ExecuteAsync(gameWorldState);
            request.CancellationToken.ThrowIfCancellationRequested();
            if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.Actors.Count)
            {
                await advanceActorEffectsUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                request.CancellationToken.ThrowIfCancellationRequested();
            }

            await decideAdventurerReturnUseCase.ExecuteAsync(gameWorldState);
            request.CancellationToken.ThrowIfCancellationRequested();
            if (shouldAdvanceTimeDependentSystems && 0 < gameWorldState.Actors.Count)
            {
                await recoverAdventurerAtInnUseCase.ExecuteAsync(gameWorldState, frameDeltaGameSeconds);
                request.CancellationToken.ThrowIfCancellationRequested();
            }
        }

        async UniTask AdvanceScheduleSystemsAsync(
            GameLoopTickResult result,
            CancellationToken cancellationToken)
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
    }
}
