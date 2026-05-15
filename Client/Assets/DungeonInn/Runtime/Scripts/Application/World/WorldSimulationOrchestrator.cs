using System;
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
using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using VContainer;

namespace DungeonInn.Application.World
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
        readonly AdvanceInnRecoveryOrchestrator advanceInnRecoveryOrchestrator;
        readonly PublishInnDailyReportUseCase publishInnDailyReportUseCase;

        const int ScheduleWorkBudgetPerFrame = 3;
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
            AdvanceInnRecoveryOrchestrator advanceInnRecoveryOrchestrator,
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
            this.advanceInnRecoveryOrchestrator = advanceInnRecoveryOrchestrator ?? throw new ArgumentNullException(nameof(advanceInnRecoveryOrchestrator));
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
            GameLoopTickResult result,
            CancellationToken cancellationToken)
        {
            var workBudget = new ScheduleWorkBudget(ScheduleWorkBudgetPerFrame);
            await spawnScheduledAdventurerUseCase.ExecuteAsync(gameWorldState, result.CurrentScheduleTick);
            cancellationToken.ThrowIfCancellationRequested();
            await YieldIfBudgetExhaustedAsync(workBudget, cancellationToken);
            await spawnScheduledMonsterUseCase.ExecuteAsync(gameWorldState, result.CurrentScheduleTick);
            cancellationToken.ThrowIfCancellationRequested();
            await YieldIfBudgetExhaustedAsync(workBudget, cancellationToken);

            var scheduleDeltaGameSeconds = result.AdvancedScheduleTicks;
            await advanceActorSimpleLifecycleUseCase.ExecuteAsync(gameWorldState, scheduleDeltaGameSeconds);
            cancellationToken.ThrowIfCancellationRequested();
            await YieldIfBudgetExhaustedAsync(workBudget, cancellationToken);
            await advanceInnRecoveryOrchestrator.EnsureReservationsAsync(gameWorldState, result.CurrentScheduleTick);
            cancellationToken.ThrowIfCancellationRequested();
            await YieldIfBudgetExhaustedAsync(workBudget, cancellationToken);

            updateEquipmentUseCase.Execute(gameWorldState);
            await YieldIfBudgetExhaustedAsync(workBudget, cancellationToken);
            sellItemsUseCase.Execute(gameWorldState);
            await YieldIfBudgetExhaustedAsync(workBudget, cancellationToken);
            await useRecoveryItemUseCase.ExecuteAsync(gameWorldState);
            cancellationToken.ThrowIfCancellationRequested();
            await YieldIfBudgetExhaustedAsync(workBudget, cancellationToken);
            await decideAdventurerReturnUseCase.ExecuteAsync(gameWorldState);
            cancellationToken.ThrowIfCancellationRequested();
        }

        static async UniTask YieldIfBudgetExhaustedAsync(
            ScheduleWorkBudget workBudget,
            CancellationToken cancellationToken)
        {
            workBudget.Consume();
            if (!workBudget.IsExhausted)
            {
                return;
            }

            await UniTask.Yield(cancellationToken);
            workBudget.Reset();
        }

        sealed class ScheduleWorkBudget
        {
            readonly int maxWork;
            int remainingWork;

            public bool IsExhausted => remainingWork <= 0;

            public ScheduleWorkBudget(int maxWork)
            {
                this.maxWork = Math.Max(1, maxWork);
                remainingWork = this.maxWork;
            }

            public void Consume()
            {
                remainingWork--;
            }

            public void Reset()
            {
                remainingWork = maxWork;
            }
        }
    }
}
