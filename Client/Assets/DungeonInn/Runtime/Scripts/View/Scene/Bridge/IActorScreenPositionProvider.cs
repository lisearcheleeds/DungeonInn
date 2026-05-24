using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.Bridge
{
    public interface IActorScreenPositionProvider
    {
        bool TryGetScreenPosition(LayerPosition position, out Vector2 screenPosition);
    }

    public interface IActorScreenPositionProviderRegistry
    {
        void Register(IActorScreenPositionProvider provider);
        void Unregister(IActorScreenPositionProvider provider);
    }
}
