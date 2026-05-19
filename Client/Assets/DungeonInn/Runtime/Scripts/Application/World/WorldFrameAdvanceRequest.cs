using System.Threading;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.World
{
    public readonly struct WorldFrameAdvanceRequest
    {
        public WorldFrameAdvanceRequest(float unscaledDeltaTimeSeconds, CancellationToken cancellationToken)
            : this(unscaledDeltaTimeSeconds, cancellationToken, null)
        {
        }

        public WorldFrameAdvanceRequest(
            float unscaledDeltaTimeSeconds,
            CancellationToken cancellationToken,
            MapLayerId? realtimeLayerId)
        {
            UnscaledDeltaTimeSeconds = unscaledDeltaTimeSeconds;
            CancellationToken = cancellationToken;
            RealtimeLayerId = realtimeLayerId;
        }

        public float UnscaledDeltaTimeSeconds { get; }
        public CancellationToken CancellationToken { get; }
        public MapLayerId? RealtimeLayerId { get; }
    }
}
