using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorEnteredDungeon : IGameEvent
    {
        public Guid ActorId { get; }
        public int FloorIndex { get; }

        public ActorEnteredDungeon(Guid actorId, int floorIndex)
        {
            ActorId = actorId;
            FloorIndex = floorIndex;
        }
    }
}
