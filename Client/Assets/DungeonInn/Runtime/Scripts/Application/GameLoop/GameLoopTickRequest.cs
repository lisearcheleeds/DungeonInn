using System;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameLoopTickRequest
    {
        public float UnscaledDeltaTimeSeconds { get; }

        public GameLoopTickRequest(float unscaledDeltaTimeSeconds)
        {
            if (unscaledDeltaTimeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTimeSeconds));
            }

            UnscaledDeltaTimeSeconds = unscaledDeltaTimeSeconds;
        }
    }
}
