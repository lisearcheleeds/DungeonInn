using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorRecoveringAtInn : IGameEvent
    {
        public Guid ActorId { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }

        public ActorRecoveringAtInn(Guid actorId, int currentHp, int maxHp)
        {
            ActorId = actorId;
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }
    }
}
