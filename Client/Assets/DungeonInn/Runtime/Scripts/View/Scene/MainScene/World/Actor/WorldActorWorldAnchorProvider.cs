using System;
using DungeonInn.Domain.Map;
using DungeonInn.View.Scene.Bridge;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorWorldAnchorProvider : IActorWorldAnchorProvider, IWorldHudCameraProvider
    {
        const float AnchorHeightOffset = 1.15f;

        readonly WorldCameraController worldCameraController;
        readonly LayerPositionViewMapper positionMapper;
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly WorldActorViewRegistry actorViewRegistry;

        [Inject]
        public WorldActorWorldAnchorProvider(
            WorldCameraController worldCameraController,
            LayerPositionViewMapper positionMapper,
            MapLayerViewRegistry layerViewRegistry,
            WorldActorViewRegistry actorViewRegistry)
        {
            this.worldCameraController =
                worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.actorViewRegistry = actorViewRegistry ?? throw new ArgumentNullException(nameof(actorViewRegistry));
        }

        public Quaternion CameraRotation => worldCameraController.CurrentCameraRotation;
        public float WorldUnitsPerPixel => worldCameraController.WorldUnitsPerPixel;

        public bool TryGetWorldAnchor(Guid actorId, out Vector3 worldPosition, out MapLayerId layerId)
        {
            if (!actorViewRegistry.TryGetActorView(actorId, out var actorView) ||
                !actorView.HasLastPosition)
            {
                worldPosition = default;
                layerId = default;
                return false;
            }

            var position = actorView.LastPosition;
            layerId = position.LayerId;
            var actorRoot = layerViewRegistry.GetOrCreateActorRoot(position.LayerId);
            var localPosition = positionMapper.ToActorLayerLocalPosition(position) + Vector3.up * AnchorHeightOffset;
            worldPosition = actorRoot.TransformPoint(localPosition);
            return true;
        }
    }
}
