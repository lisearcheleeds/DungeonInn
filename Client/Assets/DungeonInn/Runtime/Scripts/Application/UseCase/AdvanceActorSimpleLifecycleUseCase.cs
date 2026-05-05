using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceActorSimpleLifecycleUseCase
    {
        public UniTask ExecuteAsync(IReadOnlyList<Actor> actors)
        {
            if (actors == null)
            {
                throw new ArgumentNullException(nameof(actors));
            }

            foreach (var actor in actors)
            {
                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    continue;
                }

                TryAdvance(actor, behavior);
            }

            return UniTask.CompletedTask;
        }

        static void TryAdvance(Actor actor, AdventurerBehavior behavior)
        {
            var before = behavior.LifecycleState;
            var after = GetNextState(before);
            if (after == null || after == before)
            {
                return;
            }

            behavior.ChangeLifecycleState(after.Value);
            UnityEngine.Debug.Log($"[Actor] {actor.Name} lifecycle {before} -> {after.Value}");
        }

        static AdventurerLifecycleState? GetNextState(AdventurerLifecycleState current)
        {
            return current switch
            {
                AdventurerLifecycleState.Arrived   => AdventurerLifecycleState.GoingToDungeon,
                AdventurerLifecycleState.Preparing => AdventurerLifecycleState.GoingToDungeon,
                _                                  => null
            };
        }
    }
}
