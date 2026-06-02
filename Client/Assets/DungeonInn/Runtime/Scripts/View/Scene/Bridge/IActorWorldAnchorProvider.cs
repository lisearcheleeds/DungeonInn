using System;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.Bridge
{
    public interface IActorWorldAnchorProvider
    {
        bool TryGetWorldAnchor(Guid actorId, out Vector3 worldPosition, out MapLayerId layerId);
    }

    public interface IActorWorldAnchorProviderRegistry
    {
        void Register(IActorWorldAnchorProvider provider);
        void Unregister(IActorWorldAnchorProvider provider);
    }
}
