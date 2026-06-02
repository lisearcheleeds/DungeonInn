using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.Bridge;
using VContainer;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class WorldActorStatusPresenter
    {
        readonly IActorStatusViewDataProvider actorStatusViewDataProvider;
        readonly GetActorStatusSummaryQuery actorStatusSummaryQuery;
        readonly ActorStatusViewPool actorStatusViewPool;
        readonly IActorWorldAnchorProvider actorWorldAnchorProvider;
        readonly IWorldHudCameraProvider worldHudCameraProvider;
        readonly IActiveLayerProvider activeLayerProvider;
        readonly ActorEffectIconSpriteCatalog actorEffectIconSpriteCatalog;
        readonly List<ActorViewData> activeActors = new();

        [Inject]
        public WorldActorStatusPresenter(
            IActorStatusViewDataProvider actorStatusViewDataProvider,
            GetActorStatusSummaryQuery actorStatusSummaryQuery,
            ActorStatusViewPool actorStatusViewPool,
            IActorWorldAnchorProvider actorWorldAnchorProvider,
            IWorldHudCameraProvider worldHudCameraProvider,
            IActiveLayerProvider activeLayerProvider,
            ActorEffectIconSpriteCatalog actorEffectIconSpriteCatalog)
        {
            this.actorStatusViewDataProvider =
                actorStatusViewDataProvider ?? throw new ArgumentNullException(nameof(actorStatusViewDataProvider));
            this.actorStatusSummaryQuery =
                actorStatusSummaryQuery ?? throw new ArgumentNullException(nameof(actorStatusSummaryQuery));
            this.actorStatusViewPool =
                actorStatusViewPool ?? throw new ArgumentNullException(nameof(actorStatusViewPool));
            this.actorWorldAnchorProvider =
                actorWorldAnchorProvider ?? throw new ArgumentNullException(nameof(actorWorldAnchorProvider));
            this.worldHudCameraProvider =
                worldHudCameraProvider ?? throw new ArgumentNullException(nameof(worldHudCameraProvider));
            this.activeLayerProvider = activeLayerProvider ?? throw new ArgumentNullException(nameof(activeLayerProvider));
            this.actorEffectIconSpriteCatalog =
                actorEffectIconSpriteCatalog ?? throw new ArgumentNullException(nameof(actorEffectIconSpriteCatalog));
        }

        public void UpdatePositions()
        {
            var removedActorIds = actorStatusViewDataProvider.ConsumeRemovedActorIds();
            for (var index = 0; index < removedActorIds.Count; index++)
            {
                actorStatusViewPool.Return(removedActorIds[index]);
            }

            actorStatusViewDataProvider.CopyActiveActorsTo(activeActors);
            var activeLayerId = activeLayerProvider.ActiveLayerId;
            foreach (var actor in activeActors)
            {
                if (!activeLayerId.HasValue ||
                    actor.Position.LayerId.Value != activeLayerId.Value)
                {
                    actorStatusViewPool.TryReturnIfActive(actor.ActorId);
                    continue;
                }

                var statusData = actorStatusSummaryQuery.Query(actor.ActorId);
                if (!statusData.HasValue)
                {
                    actorStatusViewPool.TryReturnIfActive(actor.ActorId);
                    continue;
                }

                if (!actorWorldAnchorProvider.TryGetWorldAnchor(
                        actor.ActorId,
                        out var worldPosition,
                        out _))
                {
                    actorStatusViewPool.TryReturnIfActive(actor.ActorId);
                    continue;
                }

                var view = actorStatusViewPool.Rent(actor.ActorId);
                view.SetHpRatio(statusData.Value.HpRatio);
                view.SetStatusIcons(statusData.Value.ActiveEffects, actorEffectIconSpriteCatalog);
                view.SetScreenScale(worldHudCameraProvider.WorldUnitsPerPixel);
                view.SetWorldPosition(worldPosition, worldHudCameraProvider.CameraRotation);
            }
        }
    }
}
