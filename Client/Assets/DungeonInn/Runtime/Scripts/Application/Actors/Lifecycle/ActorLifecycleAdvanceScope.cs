using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public readonly struct ActorLifecycleAdvanceScope
    {
        readonly MapLayerId? includeLayerId;

        ActorLifecycleAdvanceScope(MapLayerId? includeLayerId)
        {
            this.includeLayerId = includeLayerId;
        }

        public static ActorLifecycleAdvanceScope All => new(null);

        public static ActorLifecycleAdvanceScope Only(MapLayerId layerId)
        {
            return new ActorLifecycleAdvanceScope(layerId);
        }

        public bool Contains(MapLayerId layerId)
        {
            if (includeLayerId.HasValue && !includeLayerId.Value.Equals(layerId))
            {
                return false;
            }

            return true;
        }
    }
}
