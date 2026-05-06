using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceActorSimpleLifecycleUseCase
    {
        readonly MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase;

        [Inject]
        public AdvanceActorSimpleLifecycleUseCase(MoveActorTowardDestinationUseCase moveActorTowardDestinationUseCase)
        {
            this.moveActorTowardDestinationUseCase = moveActorTowardDestinationUseCase
                ?? throw new ArgumentNullException(nameof(moveActorTowardDestinationUseCase));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null) throw new ArgumentNullException(nameof(worldState));

            foreach (var actor in worldState.Actors)
            {
                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    continue;
                }

                Advance(actor, behavior, worldState, deltaGameSeconds);
            }

            return UniTask.CompletedTask;
        }

        void Advance(Actor actor, AdventurerBehavior behavior, IGameWorldState worldState, float deltaGameSeconds)
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
                    AdvanceGoingToDungeon(actor, behavior, worldState, deltaGameSeconds);
                    break;
            }
        }

        void AdvanceGoingToDungeon(Actor actor, AdventurerBehavior behavior, IGameWorldState worldState, float deltaGameSeconds)
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
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Exploring);
                Debug.Log($"[Move] {actor.Name} arrived at dungeon entrance");
                Debug.Log($"[Actor] {actor.Name} lifecycle GoingToDungeon -> Exploring");
            }
            else
            {
                Debug.Log($"[Move] {actor.Name} moved toward dungeon entrance");
            }
        }
    }
}
