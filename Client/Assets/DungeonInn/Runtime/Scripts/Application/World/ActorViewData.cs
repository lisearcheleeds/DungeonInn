using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.World
{
    public readonly struct ActorViewData
    {
        public ActorViewData(
            Guid actorId,
            LayerPosition position,
            ActorBehaviorType behaviorType,
            string visualId)
        {
            if (string.IsNullOrWhiteSpace(visualId))
            {
                throw new ArgumentException("Actor view visual id is required.", nameof(visualId));
            }

            ActorId = actorId;
            Position = position;
            BehaviorType = behaviorType;
            VisualId = visualId;
        }

        public Guid ActorId { get; }
        public LayerPosition Position { get; }
        public ActorBehaviorType BehaviorType { get; }
        public string VisualId { get; }
    }
}
