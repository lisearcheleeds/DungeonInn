using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class LayerPositionViewMapper
    {
        const float LayerHeightOffset = -240f;
        const float ActorHeightOffset = 1.5f;

        public Vector3 ToUnityPosition(LayerPosition position)
        {
            return new Vector3(position.X, ResolveLayerY(position.LayerId), position.Z);
        }

        public Vector3 ToActorUnityPosition(LayerPosition position)
        {
            return ToUnityPosition(position) + Vector3.up * ActorHeightOffset;
        }

        public float ResolveLayerY(MapLayerId layerId)
        {
            return layerId.Value * LayerHeightOffset;
        }
    }
}
