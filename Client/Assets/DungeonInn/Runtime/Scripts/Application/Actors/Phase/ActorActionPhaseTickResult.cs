using System;
using System.Collections.Generic;

namespace DungeonInn.Application.Actors.Phase
{
    public sealed class ActorActionPhaseTickResult
    {
        public IReadOnlyList<ActorActionPhaseTransition> Transitions { get; }
        public IReadOnlyList<Guid> CompletedActorIds { get; }

        public ActorActionPhaseTickResult(
            IReadOnlyList<ActorActionPhaseTransition> transitions,
            IReadOnlyList<Guid> completedActorIds)
        {
            Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
            CompletedActorIds = completedActorIds ?? throw new ArgumentNullException(nameof(completedActorIds));
        }
    }
}
