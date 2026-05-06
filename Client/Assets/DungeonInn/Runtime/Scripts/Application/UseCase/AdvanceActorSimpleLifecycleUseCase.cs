using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceActorSimpleLifecycleUseCase
    {
        readonly MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase;
        readonly UseDungeonStairUseCase useDungeonStairUseCase;

        [Inject]
        public AdvanceActorSimpleLifecycleUseCase(
            MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase,
            UseDungeonStairUseCase useDungeonStairUseCase)
        {
            this.moveActorTowardDestinationUseCase = moveActorTowardDestinationUseCase
                ?? throw new ArgumentNullException(nameof(moveActorTowardDestinationUseCase));
            this.useDungeonStairUseCase = useDungeonStairUseCase
                ?? throw new ArgumentNullException(nameof(useDungeonStairUseCase));
        }

        public async UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null) throw new ArgumentNullException(nameof(worldState));

            var actors = new List<Actor>(worldState.Actors);
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
                    var before = behavior.LifecycleState;
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.GoingToDungeon);
                    Debug.Log($"[Actor] {actor.Name} lifecycle {before} -> GoingToDungeon");
                    break;

                case AdventurerLifecycleState.GoingToDungeon:
                    await AdvanceGoingToDungeonAsync(actor, behavior, worldState, deltaGameSeconds);
                    break;

                case AdventurerLifecycleState.Exploring:
                    AdvanceExploring(actor, behavior, deltaGameSeconds);
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

            // TODO: 移動速度をマスタから取得する
            var arrived = moveActorTowardDestinationUseCase.Execute(
                actor,
                destination,
                groundMap.Layer,
                pos => groundMap.IsWalkable(pos),
                5.0f,
                deltaGameSeconds);

            if (arrived)
            {
                // TODO: DepthBandConfigsをGameWorldStateから取得する
                var arrivalPosition = await useDungeonStairUseCase.ExecuteAsync(
                    worldState.Dungeon,
                    groundMap,
                    actor.Position,
                    DungeonStairType.Down,
                    Array.Empty<DungeonDepthBandConfig>());

                actor.MoveTo(arrivalPosition);
                behavior.ResetExploringTime();
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Exploring);
                Debug.Log($"[Actor] {actor.Name} entered dungeon floor 1");
            }
            else
            {
                Debug.Log($"[Move] {actor.Name} moved toward dungeon entrance");
            }
        }

        static void AdvanceExploring(Actor actor, AdventurerBehavior behavior, float deltaGameSeconds)
        {
            if (actor.Position.LayerId.Equals(MapLayerId.Ground))
            {
                return;
            }

            behavior.AccumulateExploringTime(deltaGameSeconds);
            if (behavior.ExploringTimeSeconds >= GameConstants.AdventurerExploringDurationSeconds)
            {
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Returning);
                Debug.Log($"[Actor] {actor.Name} lifecycle Exploring -> Returning");
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
                5.0f,
                deltaGameSeconds);

            if (arrived)
            {
                Debug.Log($"[Move] {actor.Name} arrived at up stair");

                // TODO: DepthBandConfigsをGameWorldStateから取得する
                var returnPosition = await useDungeonStairUseCase.ExecuteAsync(
                    worldState.Dungeon,
                    worldState.GroundMap,
                    actor.Position,
                    DungeonStairType.Up,
                    Array.Empty<DungeonDepthBandConfig>());

                actor.MoveTo(returnPosition);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Recovering);
                Debug.Log($"[Actor] {actor.Name} returned to ground");
            }
            else
            {
                Debug.Log($"[Move] {actor.Name} moved toward up stair");
            }
        }
    }
}
