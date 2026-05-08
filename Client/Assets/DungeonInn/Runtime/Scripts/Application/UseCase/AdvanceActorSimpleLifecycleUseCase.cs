using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceActorSimpleLifecycleUseCase : IDisposable
    {
        readonly Dictionary<Guid, LayerPosition> exploringDestinations = new();
        readonly MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase;
        readonly UseDungeonStairUseCase useDungeonStairUseCase;
        readonly IActorNavigationService navigationService;
        readonly IActorCombatService actorCombatService;
        readonly IGameRandom gameRandom;
        readonly IGameEventBus eventBus;
        readonly IDisposable deathSubscription;

        [Inject]
        public AdvanceActorSimpleLifecycleUseCase(
            MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase,
            UseDungeonStairUseCase useDungeonStairUseCase,
            IActorNavigationService navigationService,
            IActorCombatService actorCombatService,
            IGameRandom gameRandom,
            IGameEventBus eventBus)
        {
            this.moveActorTowardDestinationUseCase = moveActorTowardDestinationUseCase
                ?? throw new ArgumentNullException(nameof(moveActorTowardDestinationUseCase));
            this.useDungeonStairUseCase = useDungeonStairUseCase
                ?? throw new ArgumentNullException(nameof(useDungeonStairUseCase));
            this.navigationService = navigationService
                ?? throw new ArgumentNullException(nameof(navigationService));
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.gameRandom = gameRandom
                ?? throw new ArgumentNullException(nameof(gameRandom));
            this.eventBus = eventBus
                ?? throw new ArgumentNullException(nameof(eventBus));
            deathSubscription = eventBus.OnEvent<ActorDefeated>()
                .Subscribe(e => { exploringDestinations.Remove(e.ActorId); });
        }

        public void Dispose()
        {
            deathSubscription.Dispose();
        }

        public async UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var actors = worldState.Actors;
            foreach (var actor in actors)
            {
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
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.GoingToDungeon);
                    break;

                case AdventurerLifecycleState.GoingToDungeon:
                    await AdvanceGoingToDungeonAsync(actor, behavior, worldState, deltaGameSeconds);
                    break;

                case AdventurerLifecycleState.Exploring:
                    AdvanceExploring(actor, behavior, worldState, deltaGameSeconds);
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
                pos => groundMap.IsWalkable(pos),
                GameConstants.ActorMoveSpeedMetersPerSecond,
                deltaGameSeconds);

            if (arrived)
            {
                var arrivalPosition = await useDungeonStairUseCase.ExecuteAsync(
                    worldState.Dungeon,
                    groundMap,
                    actor.Position,
                    DungeonStairType.Down,
                    Array.Empty<DungeonDepthBandConfig>());

                actor.MoveTo(arrivalPosition);
                navigationService.InvalidatePath(actor.Id);
                actorCombatService.ClearCombatHistory(actor.Id);
                behavior.ResetExplorationRoomArrivalCount();
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Exploring);
                eventBus.Publish(new ActorEnteredDungeon(actor.Id, arrivalPosition.LayerId.Value));
            }
        }

        void AdvanceExploring(
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
            if (!exploringDestinations.TryGetValue(actor.Id, out var destination)
                || !destination.LayerId.Equals(actor.Position.LayerId))
            {
                if (!TryPickRoomDestination(floor, actor.Position, out destination))
                {
                    return;
                }

                exploringDestinations[actor.Id] = destination;
            }

            var arrived = moveActorTowardDestinationUseCase.Execute(
                actor,
                destination,
                floor.Layer,
                pos => floor.IsWalkable(pos),
                GameConstants.ActorMoveSpeedMetersPerSecond,
                deltaGameSeconds);

            if (arrived)
            {
                behavior.RecordExplorationRoomArrival();
                exploringDestinations.Remove(actor.Id);
                navigationService.InvalidatePath(actor.Id);

                if (GameConstants.AdventurerExplorationRoomArrivalTarget <= behavior.ExplorationRoomArrivalCount)
                {
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.Returning);
                }
            }
        }

        async UniTask AdvanceReturningAsync(Actor actor, AdventurerBehavior behavior, IGameWorldState worldState, float deltaGameSeconds)
        {
            if (actor.Position.LayerId.Equals(MapLayerId.Ground))
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
                pos => floor.IsWalkable(pos),
                GameConstants.ActorMoveSpeedMetersPerSecond,
                deltaGameSeconds);

            if (arrived)
            {
                var returnPosition = await useDungeonStairUseCase.ExecuteAsync(
                    worldState.Dungeon,
                    worldState.GroundMap,
                    actor.Position,
                    DungeonStairType.Up,
                    Array.Empty<DungeonDepthBandConfig>());

                actor.MoveTo(returnPosition);
                navigationService.InvalidatePath(actor.Id);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Recovering);

                if (returnPosition.LayerId.Equals(MapLayerId.Ground))
                {
                    eventBus.Publish(new ActorExitedDungeon(actor.Id));
                }
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
