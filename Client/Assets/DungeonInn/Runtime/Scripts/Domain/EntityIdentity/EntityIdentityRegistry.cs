using System;
using System.Collections.Generic;

namespace DungeonInn.Domain.EntityIdentity
{
    public sealed class EntityIdentityRegistry
    {
        readonly Dictionary<Guid, EntityIdentity> identities = new();

        public IReadOnlyDictionary<Guid, EntityIdentity> Identities => identities;

        public void Register(EntityIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (identities.ContainsKey(identity.Id))
            {
                throw new InvalidOperationException("Entity identity already exists.");
            }

            identities.Add(identity.Id, identity);
        }

        public EntityIdentity Resolve(Guid id)
        {
            if (!identities.TryGetValue(id, out var identity))
            {
                throw new InvalidOperationException("Entity identity does not exist.");
            }

            return identity;
        }

        public bool TryResolve(Guid id, out EntityIdentity identity)
        {
            return identities.TryGetValue(id, out identity);
        }

        public void MarkRemoved(Guid id, int occurredAtTick)
        {
            Resolve(id).MarkRemoved(occurredAtTick);
        }
    }
}
