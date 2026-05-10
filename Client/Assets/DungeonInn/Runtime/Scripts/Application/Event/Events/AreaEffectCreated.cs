using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Event.Events
{
    public sealed class AreaEffectCreated : IGameEvent
    {
        public Guid AreaEffectId { get; }
        public Guid AttackerActorId { get; }
        public LayerPosition CenterPosition { get; }
        public float RadiusMeters { get; }

        public AreaEffectCreated(Guid areaEffectId, Guid attackerActorId, LayerPosition centerPosition, float radiusMeters)
        {
            AreaEffectId = areaEffectId;
            AttackerActorId = attackerActorId;
            CenterPosition = centerPosition;
            RadiusMeters = Math.Max(0f, radiusMeters);
        }
    }
}
