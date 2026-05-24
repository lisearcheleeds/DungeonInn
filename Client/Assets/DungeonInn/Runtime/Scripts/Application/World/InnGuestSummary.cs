using System;

namespace DungeonInn.Application.World
{
    public readonly struct InnGuestSummary
    {
        public Guid ActorId { get; }
        public string Name { get; }
        public float HpRatio { get; }
        public float RecoveryRemainingSeconds { get; }

        public InnGuestSummary(
            Guid actorId,
            string name,
            float hpRatio,
            float recoveryRemainingSeconds)
        {
            ActorId = actorId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            HpRatio = hpRatio;
            RecoveryRemainingSeconds = recoveryRemainingSeconds;
        }
    }
}
