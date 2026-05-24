using System;
using System.Collections.Generic;

namespace DungeonInn.Application.World
{
    public interface IActorSelectionCandidateProvider
    {
        void CopySelectionCandidatesTo(List<ActorViewData> results);
        void CopyActorIdsTo(List<Guid> results);
    }
}
