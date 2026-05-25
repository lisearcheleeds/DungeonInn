using System;
using DungeonInn.Domain.Map;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActiveLayerActorViewRefresher : IInitializable, IDisposable
    {
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly WorldActorPresenter actorPresenter;

        [Inject]
        public WorldActiveLayerActorViewRefresher(
            MapLayerViewRegistry layerViewRegistry,
            WorldActorPresenter actorPresenter)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.actorPresenter = actorPresenter ?? throw new ArgumentNullException(nameof(actorPresenter));
        }

        public void Initialize()
        {
            layerViewRegistry.ActiveLayerChanged += OnActiveLayerChanged;
        }

        public void Dispose()
        {
            layerViewRegistry.ActiveLayerChanged -= OnActiveLayerChanged;
        }

        void OnActiveLayerChanged(MapLayerId layerId)
        {
            actorPresenter.RefreshLayerActors(layerId);
        }
    }
}
