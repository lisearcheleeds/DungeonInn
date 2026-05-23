using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorStatusPresenter
    {
        const float HudHeadOffsetY = 1.5f;

        readonly IActorStatusViewDataProvider actorStatusViewDataProvider;
        readonly GetActorStatusSummaryQuery actorStatusSummaryQuery;
        readonly ActorHUDViewPool actorHUDViewPool;
        readonly WorldCameraController worldCameraController;
        readonly LayerPositionViewMapper positionMapper;
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly List<ActorViewData> activeActors = new();

        [Inject]
        public WorldActorStatusPresenter(
            IActorStatusViewDataProvider actorStatusViewDataProvider,
            GetActorStatusSummaryQuery actorStatusSummaryQuery,
            ActorHUDViewPool actorHUDViewPool,
            WorldCameraController worldCameraController,
            LayerPositionViewMapper positionMapper,
            MapLayerViewRegistry layerViewRegistry)
        {
            this.actorStatusViewDataProvider =
                actorStatusViewDataProvider ?? throw new ArgumentNullException(nameof(actorStatusViewDataProvider));
            this.actorStatusSummaryQuery =
                actorStatusSummaryQuery ?? throw new ArgumentNullException(nameof(actorStatusSummaryQuery));
            this.actorHUDViewPool = actorHUDViewPool ?? throw new ArgumentNullException(nameof(actorHUDViewPool));
            this.worldCameraController =
                worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public void UpdatePositions()
        {
            var removedActorIds = actorStatusViewDataProvider.ConsumeRemovedActorIds();
            for (var index = 0; index < removedActorIds.Count; index++)
            {
                actorHUDViewPool.Return(removedActorIds[index]);
            }

            actorStatusViewDataProvider.CopyActiveActorsTo(activeActors);
            var activeLayerId = layerViewRegistry.ActiveLayerId;
            foreach (var actor in activeActors)
            {
                if (!activeLayerId.HasValue ||
                    !actor.Position.LayerId.Equals(activeLayerId.Value))
                {
                    actorHUDViewPool.TryReturnIfActive(actor.ActorId);
                    continue;
                }

                var statusData = actorStatusSummaryQuery.Query(actor.ActorId);
                if (!statusData.HasValue)
                {
                    continue;
                }

                var localPosition = positionMapper.ToActorLayerLocalPosition(actor.Position);
                var actorRoot = layerViewRegistry.GetOrCreateActorRoot(actor.Position.LayerId);
                var worldPosition = actorRoot.TransformPoint(localPosition) + Vector3.up * HudHeadOffsetY;
                var screenPosition = worldCameraController.WorldToScreenPoint(worldPosition);
                if (screenPosition.z < 0f)
                {
                    actorHUDViewPool.TryReturnIfActive(actor.ActorId);
                    continue;
                }

                var view = actorHUDViewPool.Rent(actor.ActorId);
                view.SetHpRatio(statusData.Value.HpRatio);
                view.SetStatusIcons(statusData.Value.ActiveEffects);
                view.SetScreenPosition(new Vector2(screenPosition.x, screenPosition.y));
            }
        }
    }
}
