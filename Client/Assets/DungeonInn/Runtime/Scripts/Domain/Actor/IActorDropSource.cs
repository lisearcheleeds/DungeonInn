using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public interface IActorDropSource
    {
        IReadOnlyList<ActorDropEntry> DropTable { get; }
    }
}
