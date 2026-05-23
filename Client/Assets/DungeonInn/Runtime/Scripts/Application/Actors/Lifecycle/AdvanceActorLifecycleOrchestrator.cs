using System;
using Cysharp.Threading.Tasks;
using VContainer;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class AdvanceActorLifecycleOrchestrator
    {
        readonly MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase;
        readonly UseDungeonStairOrchestrator useDungeonStairUseCase;
        readonly SelectDungeonTargetFloorUseCase selectDungeonTargetFloorUseCase;
        readonly SelectDungeonExplorationGoalUseCase selectDungeonExplorationGoalUseCase;
        readonly IActorNavigationService navigationService;
        readonly IActorCombatService actorCombatService;
        readonly IGameRandom gameRandom;
        readonly IEventPublisher eventPublisher;
        readonly AdventurerExplorationStateService explorationStateService;
        readonly ActorSpatialIndexService actorSpatialIndexService;
        readonly ActorViewDataStore actorViewDataStore;
        readonly ActorProcessingCandidateService candidateService;
        readonly ActorSimulationSettings actorSimulationSettings;

        [Inject]
        public AdvanceActorLifecycleOrchestrator(
            MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase,
            UseDungeonStairOrchestrator useDungeonStairUseCase,
            SelectDungeonTargetFloorUseCase selectDungeonTargetFloorUseCase,
            SelectDungeonExplorationGoalUseCase selectDungeonExplorationGoalUseCase,
            IActorNavigationService navigationService,
            IActorCombatService actorCombatService,
            IGameRandom gameRandom,
            IEventPublisher eventPublisher,
            AdventurerExplorationStateService explorationStateService,
            ActorSpatialIndexService actorSpatialIndexService,
            ActorViewDataStore actorViewDataStore,
            ActorProcessingCandidateService candidateService,
            ActorSimulationSettings actorSimulationSettings)
        {
            this.moveActorTowardDestinationUseCase = moveActorTowardDestinationUseCase
                ?? throw new ArgumentNullException(nameof(moveActorTowardDestinationUseCase));
            this.useDungeonStairUseCase = useDungeonStairUseCase
                ?? throw new ArgumentNullException(nameof(useDungeonStairUseCase));
            this.selectDungeonTargetFloorUseCase = selectDungeonTargetFloorUseCase
                ?? throw new ArgumentNullException(nameof(selectDungeonTargetFloorUseCase));
            this.selectDungeonExplorationGoalUseCase = selectDungeonExplorationGoalUseCase
                ?? throw new ArgumentNullException(nameof(selectDungeonExplorationGoalUseCase));
            this.navigationService = navigationService
                ?? throw new ArgumentNullException(nameof(navigationService));
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.gameRandom = gameRandom
                ?? throw new ArgumentNullException(nameof(gameRandom));
            this.eventPublisher = eventPublisher
                ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.explorationStateService = explorationStateService
                ?? throw new ArgumentNullException(nameof(explorationStateService));
            this.actorSpatialIndexService = actorSpatialIndexService
                ?? throw new ArgumentNullException(nameof(actorSpatialIndexService));
            this.actorViewDataStore = actorViewDataStore
                ?? throw new ArgumentNullException(nameof(actorViewDataStore));
            this.candidateService = candidateService
                ?? throw new ArgumentNullException(nameof(candidateService));
            this.actorSimulationSettings = actorSimulationSettings
                ?? throw new ArgumentNullException(nameof(actorSimulationSettings));
        }

        public async UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            await ExecuteAsync(worldState, deltaGameSeconds, ActorLifecycleAdvanceScope.All);
        }

        public async UniTask ExecuteAsync(
            IGameWorldState worldState,
            float deltaGameSeconds,
            ActorLifecycleAdvanceScope scope)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var actors = worldState.Actors;
            foreach (var actor in actors)
            {
                if (!scope.Contains(actor.Position.LayerId))
                {
                    continue;
                }

                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    continue;
                }

                await AdvanceAsync(actor, behavior, worldState, deltaGameSeconds);
            }
        }

        async UniTask AdvanceAsync(Actor actor, AdventurerBehavior behavior, IGameWorldState worldState, float deltaGameSeconds)
        {
            switch (behavior.LifecycleState)
            {
                case AdventurerLifecycleState.Arrived:
                case AdventurerLifecycleState.Preparing:
                    await PrepareExplorationAsync(actor, behavior, worldState);
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.GoingToDungeon);
                    candidateService.SyncActor(actor);
                    break;

                case AdventurerLifecycleState.GoingToDungeon:
                    await AdvanceGoingToDungeonAsync(actor, behavior, worldState, deltaGameSeconds);
                    break;

                case AdventurerLifecycleState.Exploring:
                    await AdvanceExploringAsync(actor, behavior, worldState, deltaGameSeconds);
                    break;

                case AdventurerLifecycleState.Returning:
                    await AdvanceReturningAsync(actor, behavior, worldState, deltaGameSeconds);
                    break;
            }
        }

        async UniTask AdvanceGoingToDungeonAsync(Actor actor, AdventurerBehavior behavior, IGameWorldState worldState, float deltaGameSeconds)
        {
            if (!actor.Position.LayerId.Equals(MapLayerId.Ground))
            {
                return;
            }

            var groundMap = worldState.GroundMap;
            var destination = groundMap.Layer.GetCellCenter(groundMap.DungeonEntrancePosition);

            var arrived = moveActorTowardDestinationUseCase.Execute(
                actor,
                destination,
                groundMap.Layer,
                groundMap,
                actorSimulationSettings.MoveSpeedMetersPerSecond,
                deltaGameSeconds);

            if (arrived)
            {
                var arrivalPosition = await useDungeonStairUseCase.ExecuteAsync(
                    worldState.Dungeon,
                    groundMap,
                    actor.Position,
                    DungeonStairType.Down,
                    Array.Empty<DungeonDepthBandConfig>());

                MoveTo(actor, arrivalPosition);
                navigationService.InvalidatePath(actor.Id);
                actorCombatService.ClearCombatHistory(actor.Id);
                behavior.ResetExplorationRoomArrivalCount();
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Exploring);
                candidateService.SyncActor(actor);
                eventPublisher.Publish(new ActorEnteredDungeon(actor.Id, arrivalPosition.LayerId.Value));
            }
        }

        async UniTask PrepareExplorationAsync(
            Actor actor,
            AdventurerBehavior behavior,
            IGameWorldState worldState)
        {
            var targetFloorDepth = await selectDungeonTargetFloorUseCase.ExecuteAsync(actor);
            behavior.SetTargetFloorDepth(targetFloorDepth);

            var goal = await selectDungeonExplorationGoalUseCase.ExecuteAsync(
                worldState.Guild,
                actor,
                targetFloorDepth);
            actor.ChangeGoal(ToActorGoal(goal));
        }

        static ActorGoal ToActorGoal(DungeonExplorationGoal goal)
        {
            switch (goal.Type)
            {
                case DungeonExplorationGoalType.Leveling:
                    return new ActorGoal(ActorGoalType.LevelUp, 0, 1, 0);
                case DungeonExplorationGoalType.CollectItem:
                    return new ActorGoal(ActorGoalType.CollectItem, goal.TargetItemId, goal.TargetItemCount, 0);
                case DungeonExplorationGoalType.DefeatMonster:
                    return new ActorGoal(ActorGoalType.DefeatMonster, goal.TargetMonsterId, 1, 0);
                case DungeonExplorationGoalType.ReachFloor:
                    return new ActorGoal(ActorGoalType.ReachFloor, goal.TargetFloorId, 1, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(goal));
            }
        }

        async UniTask AdvanceExploringAsync(
            Actor actor,
            AdventurerBehavior behavior,
            IGameWorldState worldState,
            float deltaGameSeconds)
        {
            if (actor.Position.LayerId.Equals(MapLayerId.Ground))
            {
                return;
            }

            if (actorCombatService.HasTarget(actor.Id))
            {
                return;
            }

            var floor = worldState.Dungeon.GetFloor(actor.Position.LayerId.Value);
            if (floor.FloorIndex < behavior.TargetFloorDepth)
            {
                await AdvanceTowardDeeperFloorAsync(actor, floor, worldState, deltaGameSeconds);
                return;
            }

            if (!explorationStateService.TryGetDestination(actor.Id, out var destination)
                || !destination.LayerId.Equals(actor.Position.LayerId))
            {
                if (!TryPickRoomDestination(floor, actor.Position, out destination))
                {
                    return;
                }

                explorationStateService.SetDestination(actor.Id, destination);
            }

            var arrived = moveActorTowardDestinationUseCase.Execute(
                actor,
                destination,
                floor.Layer,
                floor,
                actorSimulationSettings.MoveSpeedMetersPerSecond,
                deltaGameSeconds);

            if (arrived)
            {
                behavior.RecordExplorationRoomArrival();
                explorationStateService.RemoveDestination(actor.Id);
                navigationService.InvalidatePath(actor.Id);

                if (actorSimulationSettings.ExplorationRoomArrivalTarget <= behavior.ExplorationRoomArrivalCount)
                {
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.Returning);
                    candidateService.MarkPostDungeonScheduleCandidates(actor.Id);
                }
            }
        }

        async UniTask AdvanceTowardDeeperFloorAsync(
            Actor actor,
            DungeonFloor floor,
            IGameWorldState worldState,
            float deltaGameSeconds)
        {
            var downStairDestination = floor.GetArrivalPosition(DungeonStairType.Down);
            var arrived = moveActorTowardDestinationUseCase.Execute(
                actor,
                downStairDestination,
                floor.Layer,
                floor,
                actorSimulationSettings.MoveSpeedMetersPerSecond,
                deltaGameSeconds);

            if (!arrived)
            {
                return;
            }

            var nextFloorPosition = await useDungeonStairUseCase.ExecuteAsync(
                worldState.Dungeon,
                worldState.GroundMap,
                actor.Position,
                DungeonStairType.Down,
                Array.Empty<DungeonDepthBandConfig>());

            MoveTo(actor, nextFloorPosition);
            explorationStateService.RemoveDestination(actor.Id);
            navigationService.InvalidatePath(actor.Id);
            actorCombatService.ClearCombatHistory(actor.Id);
            eventPublisher.Publish(new ActorEnteredDungeon(actor.Id, nextFloorPosition.LayerId.Value));
        }

        async UniTask AdvanceReturningAsync(Actor actor, AdventurerBehavior behavior, IGameWorldState worldState, float deltaGameSeconds)
        {
            if (actor.Position.LayerId.Equals(MapLayerId.Ground))
            {
                return;
            }

            if (actorCombatService.HasTarget(actor.Id))
            {
                return;
            }

            var floorIndex = actor.Position.LayerId.Value;
            var floor = worldState.Dungeon.GetFloor(floorIndex);
            var upStairDestination = floor.GetArrivalPosition(DungeonStairType.Up);

            var arrived = moveActorTowardDestinationUseCase.Execute(
                actor,
                upStairDestination,
                floor.Layer,
                floor,
                actorSimulationSettings.MoveSpeedMetersPerSecond,
                deltaGameSeconds);

            if (arrived)
            {
                var returnPosition = await useDungeonStairUseCase.ExecuteAsync(
                    worldState.Dungeon,
                    worldState.GroundMap,
                    actor.Position,
                    DungeonStairType.Up,
                    Array.Empty<DungeonDepthBandConfig>());

                if (returnPosition.LayerId.Equals(MapLayerId.Ground))
                {
                    MoveTo(actor, returnPosition);
                    navigationService.InvalidatePath(actor.Id);
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.Recovering);
                    candidateService.MarkRecoveryCandidate(actor.Id);
                    candidateService.MarkPostDungeonScheduleCandidates(actor.Id);
                    eventPublisher.Publish(new ActorExitedDungeon(actor.Id));
                    return;
                }

                MoveTo(actor, returnPosition);
                navigationService.InvalidatePath(actor.Id);
                eventPublisher.Publish(new ActorEnteredDungeon(actor.Id, returnPosition.LayerId.Value));
            }
        }

        bool TryPickRoomDestination(
            DungeonFloor floor,
            LayerPosition actorPosition,
            out LayerPosition destination)
        {
            if (floor.Rooms.Count == 0)
            {
                destination = default;
                return false;
            }

            var currentGrid = floor.Layer.ToGridPosition(actorPosition);
            for (var i = 0; i < floor.Rooms.Count; i++)
            {
                var room = floor.Rooms[gameRandom.Next(floor.Rooms.Count)];
                if (floor.Rooms.Count == 1 || !Contains(room, currentGrid))
                {
                    destination = PickRoomCell(floor, room);
                    return true;
                }
            }

            destination = PickRoomCell(floor, floor.Rooms[gameRandom.Next(floor.Rooms.Count)]);
            return true;
        }

        void MoveTo(Actor actor, LayerPosition position)
        {
            actor.MoveTo(position);
            actorSpatialIndexService.SyncActor(actor);
            actorViewDataStore.SyncActor(actor);
        }

        LayerPosition PickRoomCell(DungeonFloor floor, DungeonRoom room)
        {
            if (floor.IsWalkable(room.Center))
            {
                return floor.Layer.GetCellCenter(room.Center);
            }

            if (0 < room.Cells.Count)
            {
                var startIndex = gameRandom.Next(room.Cells.Count);
                for (var i = 0; i < room.Cells.Count; i++)
                {
                    var cell = room.Cells[(startIndex + i) % room.Cells.Count];
                    if (floor.IsWalkable(cell))
                    {
                        return floor.Layer.GetCellCenter(cell);
                    }
                }
            }

            return floor.Layer.GetCellCenter(room.Center);
        }

        static bool Contains(DungeonRoom room, GridPosition position)
        {
            for (var i = 0; i < room.Cells.Count; i++)
            {
                if (room.Cells[i].Equals(position))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
