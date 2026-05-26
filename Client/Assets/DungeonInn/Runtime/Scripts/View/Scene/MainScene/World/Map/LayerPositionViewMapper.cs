using System;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class LayerPositionViewMapper
    {
        readonly ILayerPositionViewSettingsRepository settingsRepository;

        [Inject]
        public LayerPositionViewMapper(ILayerPositionViewSettingsRepository settingsRepository)
        {
            this.settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        }

        public Vector3 ToUnityPosition(LayerPosition position)
        {
            return new Vector3(position.X, ResolveLayerY(position.LayerId), position.Z);
        }

        public Vector3 ToActorUnityPosition(LayerPosition position)
        {
            return ToUnityPosition(position) + Vector3.up * settingsRepository.Get().ActorHeightOffset;
        }

        public Vector3 ToLayerLocalPosition(LayerPosition position)
        {
            return new Vector3(position.X, 0f, position.Z);
        }

        public Vector3 ToActorLayerLocalPosition(LayerPosition position)
        {
            return ToLayerLocalPosition(position) + Vector3.up * settingsRepository.Get().ActorHeightOffset;
        }

        public float ResolveLayerY(MapLayerId layerId)
        {
            return layerId.Value * settingsRepository.Get().LayerHeightOffset;
        }
    }
}
