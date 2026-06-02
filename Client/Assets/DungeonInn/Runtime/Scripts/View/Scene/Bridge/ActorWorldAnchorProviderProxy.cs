using System;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.Bridge
{
    public sealed class ActorWorldAnchorProviderProxy :
        IActorWorldAnchorProvider,
        IActorWorldAnchorProviderRegistry
    {
        IActorWorldAnchorProvider current;

        public bool TryGetWorldAnchor(Guid actorId, out Vector3 worldPosition, out MapLayerId layerId)
        {
            if (current == null)
            {
                worldPosition = default;
                layerId = default;
                return false;
            }

            return current.TryGetWorldAnchor(actorId, out worldPosition, out layerId);
        }

        public void Register(IActorWorldAnchorProvider provider)
        {
            if (current != null)
            {
                throw new InvalidOperationException("Duplicate actor world anchor provider registration.");
            }

            current = provider;
        }

        public void Unregister(IActorWorldAnchorProvider provider)
        {
            if (ReferenceEquals(current, provider))
            {
                current = null;
            }
        }
    }
}
