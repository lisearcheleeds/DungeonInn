using System;

namespace DungeonInn.Application.Actors.Ai
{
    [Flags]
    public enum ActorAiDirtyFlags
    {
        None = 0,
        ShortTerm = 1,
        MidTerm = 2,
        LongTerm = 4
    }
}
