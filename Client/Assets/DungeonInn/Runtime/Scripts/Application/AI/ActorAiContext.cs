using System;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.AI
{
    public sealed class ActorAiContext
    {
        public Actor Actor { get; }
        public int CurrentTick { get; }
        public ActorAiRuntimeState RuntimeState { get; }

        public ActorAiContext(Actor actor, int currentTick, ActorAiRuntimeState runtimeState)
        {
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            CurrentTick = currentTick;
            RuntimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
        }
    }
}
