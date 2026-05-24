using System;
using System.Collections.Generic;
using VContainer;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.Bridge;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class WorldActorStatusPresenter
    {
        readonly IActorStatusViewDataProvider actorStatusViewDataProvider;
        readonly GetActorStatusSummaryQuery actorStatusSummaryQuery;
        readonly ActorHUDViewPool actorHUDViewPool;
        readonly IActorScreenPositionProvider screenPositionProvider;
        readonly IActiveLayerProvider activeLayerProvider;
        readonly List<ActorViewData> activeActors = new();

        [Inject]
        public WorldActorStatusPresenter(
            IActorStatusViewDataProvider actorStatusViewDataProvider,
            GetActorStatusSummaryQuery actorStatusSummaryQuery,
            ActorHUDViewPool actorHUDViewPool,
            IActorScreenPositionProvider screenPositionProvider,
            IActiveLayerProvider activeLayerProvider)
        {
            this.actorStatusViewDataProvider =
                actorStatusViewDataProvider ?? throw new ArgumentNullException(nameof(actorStatusViewDataProvider));
            this.actorStatusSummaryQuery =
                actorStatusSummaryQuery ?? throw new ArgumentNullException(nameof(actorStatusSummaryQuery));
            this.actorHUDViewPool = actorHUDViewPool ?? throw new ArgumentNullException(nameof(actorHUDViewPool));
            this.screenPositionProvider =
                screenPositionProvider ?? throw new ArgumentNullException(nameof(screenPositionProvider));
            this.activeLayerProvider = activeLayerProvider ?? throw new ArgumentNullException(nameof(activeLayerProvider));
        }

        public void UpdatePositions()
        {
            var removedActorIds = actorStatusViewDataProvider.ConsumeRemovedActorIds();
            for (var index = 0; index < removedActorIds.Count; index++)
            {
                actorHUDViewPool.Return(removedActorIds[index]);
            }

            actorStatusViewDataProvider.CopyActiveActorsTo(activeActors);
            var activeLayerId = activeLayerProvider.ActiveLayerId;
            foreach (var actor in activeActors)
            {
                if (!activeLayerId.HasValue ||
                    actor.Position.LayerId.Value != activeLayerId.Value)
                {
                    actorHUDViewPool.TryReturnIfActive(actor.ActorId);
                    continue;
                }

                var statusData = actorStatusSummaryQuery.Query(actor.ActorId);
                if (!statusData.HasValue)
                {
                    continue;
                }

                if (!screenPositionProvider.TryGetScreenPosition(actor.Position, out var screenPosition))
                {
                    actorHUDViewPool.TryReturnIfActive(actor.ActorId);
                    continue;
                }

                var view = actorHUDViewPool.Rent(actor.ActorId);
                view.SetHpRatio(statusData.Value.HpRatio);
                view.SetStatusIcons(statusData.Value.ActiveEffects);
                view.SetScreenPosition(screenPosition);
            }
        }
    }
}
