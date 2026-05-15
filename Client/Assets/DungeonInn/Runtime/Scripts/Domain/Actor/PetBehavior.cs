using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class PetBehavior : IActorBehavior
    {
        public Guid OwnerActorId { get; }

        public PetBehavior(Guid ownerActorId)
        {
            OwnerActorId = ownerActorId;
        }

        public void OnRecovered(
            int hpAmount,
            int mpAmount,
            int fatigueReduction,
            int stressReduction,
            int injuryReduction)
        {
        }
    }
}
