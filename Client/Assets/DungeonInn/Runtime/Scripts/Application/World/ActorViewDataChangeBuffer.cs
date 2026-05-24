using System;
using System.Collections.Generic;

namespace DungeonInn.Application.World
{
    public readonly struct ActorViewDataChangeBuffer
    {
        public ActorViewDataChangeBuffer(
            IReadOnlyList<ActorViewData> changedActors,
            IReadOnlyList<Guid> removedActorIds)
        {
            ChangedActors = changedActors ?? throw new ArgumentNullException(nameof(changedActors));
            RemovedActorIds = removedActorIds ?? throw new ArgumentNullException(nameof(removedActorIds));
        }

        public IReadOnlyList<ActorViewData> ChangedActors { get; }
        public IReadOnlyList<Guid> RemovedActorIds { get; }
    }
}
