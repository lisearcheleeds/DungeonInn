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
    }
}
