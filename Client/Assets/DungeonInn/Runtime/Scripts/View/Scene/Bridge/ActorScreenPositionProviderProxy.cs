using System;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.Bridge
{
    public sealed class ActorScreenPositionProviderProxy :
        IActorScreenPositionProvider,
        IActorScreenPositionProviderRegistry
    {
        IActorScreenPositionProvider current;

        public bool TryGetScreenPosition(LayerPosition position, out Vector2 screenPosition)
        {
            if (current == null)
            {
                screenPosition = default;
                return false;
            }

            return current.TryGetScreenPosition(position, out screenPosition);
        }

        public void Register(IActorScreenPositionProvider provider)
        {
            if (current != null)
            {
                throw new InvalidOperationException("Duplicate actor screen position provider registration.");
            }

            current = provider;
        }

        public void Unregister(IActorScreenPositionProvider provider)
        {
            if (ReferenceEquals(current, provider))
            {
                current = null;
            }
        }
    }
}
