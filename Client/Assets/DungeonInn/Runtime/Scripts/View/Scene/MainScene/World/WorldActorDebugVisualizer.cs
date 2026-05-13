using System;
using DungeonInn.Application.GameLoop;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorDebugVisualizer
    {
        readonly IGameWorldStateReader gameWorldState;
        readonly WorldMapView mapView;
        readonly WorldActorPresenter actorPresenter;

        [Inject]
        public WorldActorDebugVisualizer(
            IGameWorldStateReader gameWorldState,
            WorldMapView mapView,
            WorldActorPresenter actorPresenter)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.mapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
            this.actorPresenter = actorPresenter ?? throw new ArgumentNullException(nameof(actorPresenter));
        }

        public void UpdateVisuals()
        {
            if (!gameWorldState.IsInitialized)
            {
                return;
            }

            mapView.UpdateVisuals();
            actorPresenter.UpdateVisuals();
        }
    }
}
