using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Combat;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Facilities
{
    public sealed class AdvanceGroundFacilityTaskOrchestrator
    {
        readonly MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase;
        readonly FacilityBuildingRegistry facilityBuildingRegistry;
        readonly FacilityInteractionOrchestrator facilityInteractionOrchestrator;
        readonly ActorFacilityPresenceService actorFacilityPresenceService;
        readonly ActorSpatialIndexService actorSpatialIndexService;
        readonly ActorViewDataStore actorViewDataStore;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly ActorDecisionScheduler actorDecisionScheduler;
        readonly IGameClock gameClock;
        readonly DespawnAdventurerUseCase despawnAdventurerUseCase;

        [Inject]
        public AdvanceGroundFacilityTaskOrchestrator(
            MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase,
            FacilityBuildingRegistry facilityBuildingRegistry,
            FacilityInteractionOrchestrator facilityInteractionOrchestrator,
            ActorFacilityPresenceService actorFacilityPresenceService,
            ActorSpatialIndexService actorSpatialIndexService,
            ActorViewDataStore actorViewDataStore,
            IWorldGameSettingsRepository worldGameSettingsRepository,
            ActorDecisionScheduler actorDecisionScheduler,
            IGameClock gameClock,
            DespawnAdventurerUseCase despawnAdventurerUseCase)
        {
            this.moveActorTowardDestinationUseCase = moveActorTowardDestinationUseCase
                ?? throw new ArgumentNullException(nameof(moveActorTowardDestinationUseCase));
            this.facilityBuildingRegistry = facilityBuildingRegistry
                ?? throw new ArgumentNullException(nameof(facilityBuildingRegistry));
            this.facilityInteractionOrchestrator = facilityInteractionOrchestrator
                ?? throw new ArgumentNullException(nameof(facilityInteractionOrchestrator));
            this.actorFacilityPresenceService = actorFacilityPresenceService
                ?? throw new ArgumentNullException(nameof(actorFacilityPresenceService));
            this.actorSpatialIndexService = actorSpatialIndexService
                ?? throw new ArgumentNullException(nameof(actorSpatialIndexService));
            this.actorViewDataStore = actorViewDataStore ?? throw new ArgumentNullException(nameof(actorViewDataStore));
            this.worldGameSettingsRepository = worldGameSettingsRepository
                ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.actorDecisionScheduler = actorDecisionScheduler
                ?? throw new ArgumentNullException(nameof(actorDecisionScheduler));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.despawnAdventurerUseCase =
                despawnAdventurerUseCase ?? throw new ArgumentNullException(nameof(despawnAdventurerUseCase));
        }

        public async UniTask ExecuteAsync(
            IGameWorldState worldState,
            Actor actor,
            AdventurerBehavior behavior,
            float deltaGameSeconds)
        {
            if (behavior.LifecycleState == AdventurerLifecycleState.WaitingForInn &&
                worldState.Guild.TryGetQueuedInnFacility(actor.Id, out var queuedInnFacilityId))
            {
                await AdvanceQueuedInnAsync(worldState, actor, behavior, queuedInnFacilityId, deltaGameSeconds);
                return;
            }

            if (actor.CurrentPlan.Type == ActorPlanType.Prepare)
            {
                actor.ChangePlan(ActorPlan.None());
                actor.ChangeAction(ActorAction.None());
                actorDecisionScheduler.MarkDirty(actor.Id, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm);
                if (actor.Hp >= actor.Params.MaxHp)
                {
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
                }

                actorFacilityPresenceService.Exit(actor.Id);
                actorViewDataStore.SyncActor(actor);
                return;
            }

            if (actor.CurrentPlan.Type != ActorPlanType.UseFacility || !actor.CurrentPlan.TargetGuid.HasValue)
            {
                return;
            }

            var facilityId = actor.CurrentPlan.TargetGuid.Value;
            if (!facilityBuildingRegistry.TryGetByFacilityId(facilityId, out var building))
            {
                actor.ChangePlan(ActorPlan.None());
                actor.ChangeAction(ActorAction.None());
                return;
            }

            var action = actor.CurrentAction;
            if (action.Type != ActorActionType.Move || action.State == ActorActionState.Completed)
            {
                var point = building.InteractionPoint;
                actor.ChangeAction(ActorAction.MoveTo(point.Position, point.ArrivalRadiusMeters));
                action = actor.CurrentAction;
            }

            var arrived = moveActorTowardDestinationUseCase.Execute(
                actor,
                action.TargetPosition,
                worldState.GroundMap.Layer,
                worldState.GroundMap,
                worldGameSettingsRepository.GetActorSimulationSettings().MoveSpeedMetersPerSecond,
                deltaGameSeconds,
                Math.Max(action.ArrivalRadiusMeters, worldState.GroundMap.Layer.CellSizeMeters * 0.5f));
            actorSpatialIndexService.SyncActor(actor);
            actorViewDataStore.SyncActor(actor);
            if (!arrived)
            {
                return;
            }

            actor.CompleteCurrentAction();
            var facility = worldState.Guild.GetFacility(facilityId);
            var shouldEnterFacility = ShouldEnterFacility(worldState, actor, facility);
            if (shouldEnterFacility)
            {
                actorFacilityPresenceService.Enter(actor.Id, facilityId);
                actorViewDataStore.SyncActor(actor);
            }

            var result = await facilityInteractionOrchestrator.ExecuteAsync(worldState, actor, facility);

            if (result == FacilityInteractionResult.WaitingOutside)
            {
                actorFacilityPresenceService.Exit(actor.Id);
                actor.ChangePlan(ActorPlan.None());
                actor.ChangeAction(ActorAction.Wait());
                actorDecisionScheduler.MarkDirty(actor.Id, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm);
                actorViewDataStore.SyncActor(actor);
                return;
            }

            if (result == FacilityInteractionResult.Completed || actor.Hp >= actor.Params.MaxHp)
            {
                actorFacilityPresenceService.Exit(actor.Id);
                actor.ChangePlan(ActorPlan.None());
                actor.ChangeAction(ActorAction.None());
                actorDecisionScheduler.MarkDirty(actor.Id, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm);
                actorViewDataStore.SyncActor(actor);
            }
        }

        async UniTask AdvanceQueuedInnAsync(
            IGameWorldState worldState,
            Actor actor,
            AdventurerBehavior behavior,
            Guid innFacilityId,
            float deltaGameSeconds)
        {
            RemoveInvalidQueuedInnReservations(worldState, innFacilityId);

            if (!facilityBuildingRegistry.TryGetByFacilityId(innFacilityId, out var building))
            {
                worldState.Guild.RemoveQueuedInnReservation(actor.Id);
                behavior.ClearWaitingForInn();
                actor.ChangePlan(ActorPlan.None());
                actor.ChangeAction(ActorAction.None());
                actorDecisionScheduler.MarkDirty(actor.Id, ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm);
                actorViewDataStore.SyncActor(actor);
                return;
            }

            var waitedDays = gameClock.CurrentDay - behavior.WaitingForInnStartedDay;
            if (worldGameSettingsRepository.GetInnBalanceSettings().AdventurerWaitDepartureDays <= waitedDays)
            {
                worldState.Guild.RemoveQueuedInnReservation(actor.Id);
                despawnAdventurerUseCase.Execute(worldState, actor, waitedDays);
                return;
            }

            var action = actor.CurrentAction;
            if (action.Type != ActorActionType.Move || action.State == ActorActionState.Completed)
            {
                var point = building.InteractionPoint;
                actor.ChangeAction(ActorAction.MoveTo(point.Position, point.ArrivalRadiusMeters));
                action = actor.CurrentAction;
            }

            var arrived = moveActorTowardDestinationUseCase.Execute(
                actor,
                action.TargetPosition,
                worldState.GroundMap.Layer,
                worldState.GroundMap,
                worldGameSettingsRepository.GetActorSimulationSettings().MoveSpeedMetersPerSecond,
                deltaGameSeconds,
                Math.Max(action.ArrivalRadiusMeters, worldState.GroundMap.Layer.CellSizeMeters * 0.5f));
            actorSpatialIndexService.SyncActor(actor);
            actorViewDataStore.SyncActor(actor);
            if (!arrived)
            {
                return;
            }

            if (!worldState.Guild.TryPeekQueuedInnReservation(innFacilityId, out var queuedActorId) ||
                !queuedActorId.Equals(actor.Id) ||
                !worldState.Guild.CanReserveInn(innFacilityId))
            {
                actor.ChangeAction(ActorAction.Wait());
                actorViewDataStore.SyncActor(actor);
                return;
            }

            actor.CompleteCurrentAction();
            actorFacilityPresenceService.Enter(actor.Id, innFacilityId);
            actorViewDataStore.SyncActor(actor);
            var facility = worldState.Guild.GetFacility(innFacilityId);
            await facilityInteractionOrchestrator.ExecuteAsync(worldState, actor, facility);
        }

        void RemoveInvalidQueuedInnReservations(IGameWorldState worldState, Guid innFacilityId)
        {
            while (worldState.Guild.TryPeekQueuedInnReservation(innFacilityId, out var queuedActorId))
            {
                var queuedActor = worldState.FindActor(queuedActorId);
                if (IsValidQueuedInnReservation(worldState, queuedActor, innFacilityId))
                {
                    return;
                }

                worldState.Guild.RemoveQueuedInnReservation(queuedActorId);
            }
        }

        static bool IsValidQueuedInnReservation(IGameWorldState worldState, Actor actor, Guid innFacilityId)
        {
            if (actor == null)
            {
                return false;
            }

            if (worldState.Guild.HasActiveInnReservation(actor.Id))
            {
                return false;
            }

            if (!actor.Position.LayerId.Equals(MapLayerId.Ground))
            {
                return false;
            }

            if (actor.Behavior is not AdventurerBehavior behavior)
            {
                return false;
            }

            if (behavior.LifecycleState != AdventurerLifecycleState.WaitingForInn)
            {
                return false;
            }

            return worldState.Guild.TryGetQueuedInnFacility(actor.Id, out var queuedInnFacilityId) &&
                queuedInnFacilityId.Equals(innFacilityId);
        }

        bool ShouldEnterFacility(IGameWorldState worldState, Actor actor, Facility facility)
        {
            if (facility.Type != FacilityType.Inn)
            {
                return true;
            }

            if (worldState.Guild.HasActiveInnReservation(actor.Id))
            {
                return true;
            }

            if (!worldState.Guild.CanReserveInn(facility.Id))
            {
                return false;
            }

            return !worldState.Guild.TryPeekQueuedInnReservation(facility.Id, out var queuedActorId) ||
                queuedActorId.Equals(actor.Id);
        }
    }
}
