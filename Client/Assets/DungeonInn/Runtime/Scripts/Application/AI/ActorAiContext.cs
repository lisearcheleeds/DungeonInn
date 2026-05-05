using System;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.AI
{
    public sealed class ActorAiContext
    {
        public Actor Actor { get; }
        public float CurrentTimeSeconds { get; }
        public ActorAiRuntimeState RuntimeState { get; }

        public ActorAiContext(Actor actor, float currentTimeSeconds, ActorAiRuntimeState runtimeState)
        {
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            CurrentTimeSeconds = currentTimeSeconds;
            RuntimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
        }
    }
}
