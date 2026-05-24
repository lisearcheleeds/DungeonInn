using System;
using System.Collections.Generic;

namespace DungeonInn.Application.World
{
    public interface IActorStatusViewDataProvider
    {
        IReadOnlyList<Guid> ConsumeRemovedActorIds();
        void CopyActiveActorsTo(List<ActorViewData> results);
    }
}
