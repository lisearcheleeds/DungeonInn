using System;
using DungeonInn.Domain.Map;
using DungeonInn.View.Scene.Bridge;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorScreenPositionProvider : IActorScreenPositionProvider
    {
        readonly WorldCameraController worldCameraController;
        readonly LayerPositionViewMapper positionMapper;
        readonly MapLayerViewRegistry layerViewRegistry;

        [Inject]
        public WorldActorScreenPositionProvider(
            WorldCameraController worldCameraController,
            LayerPositionViewMapper positionMapper,
            MapLayerViewRegistry layerViewRegistry)
        {
            this.worldCameraController =
                worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public bool TryGetScreenPosition(LayerPosition position, out Vector2 screenPosition)
        {
            var actorRoot = layerViewRegistry.GetOrCreateActorRoot(position.LayerId);
            var localPosition = positionMapper.ToActorLayerLocalPosition(position);
            var worldPosition = actorRoot.TransformPoint(localPosition);
            return TryGetWorldScreenPosition(worldPosition, out screenPosition);
        }

        bool TryGetWorldScreenPosition(Vector3 worldPosition, out Vector2 screenPosition)
        {
            if (!worldCameraController.IsWorldPositionVisible(worldPosition, 0f))
            {
                screenPosition = default;
                return false;
            }

            var screenPoint = worldCameraController.WorldToScreenPoint(worldPosition);
            if (screenPoint.z < 0f)
            {
                screenPosition = default;
                return false;
            }

            screenPosition = new Vector2(screenPoint.x, screenPoint.y);
            return true;
        }
    }
}
