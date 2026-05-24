using System;

namespace DungeonInn.Application.World
{
    public readonly struct ActorEffectIconViewData
    {
        public ActorEffectIconViewData(int actorEffectMasterId, string displayName, float remainingSeconds)
        {
            ActorEffectMasterId = actorEffectMasterId;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            RemainingSeconds = remainingSeconds;
        }

        public int ActorEffectMasterId { get; }
        public string DisplayName { get; }
        public float RemainingSeconds { get; }
    }
}
