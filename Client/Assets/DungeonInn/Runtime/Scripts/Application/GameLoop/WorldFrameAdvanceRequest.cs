using System.Threading;

namespace DungeonInn.Application.GameLoop
{
    public readonly struct WorldFrameAdvanceRequest
    {
        public WorldFrameAdvanceRequest(float unscaledDeltaTimeSeconds, CancellationToken cancellationToken)
        {
            UnscaledDeltaTimeSeconds = unscaledDeltaTimeSeconds;
            CancellationToken = cancellationToken;
        }

        public float UnscaledDeltaTimeSeconds { get; }
        public CancellationToken CancellationToken { get; }
    }
}
