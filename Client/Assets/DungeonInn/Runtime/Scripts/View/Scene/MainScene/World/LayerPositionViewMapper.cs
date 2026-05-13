using System;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class LayerPositionViewMapper
    {
        readonly LayerPositionViewSettings settings;

        [Inject]
        public LayerPositionViewMapper(LayerPositionViewSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Vector3 ToUnityPosition(LayerPosition position)
        {
            return new Vector3(position.X, ResolveLayerY(position.LayerId), position.Z);
        }

        public Vector3 ToActorUnityPosition(LayerPosition position)
        {
            return ToUnityPosition(position) + Vector3.up * settings.ActorHeightOffset;
        }

        public float ResolveLayerY(MapLayerId layerId)
        {
            return layerId.Value * settings.LayerHeightOffset;
        }
    }
}
