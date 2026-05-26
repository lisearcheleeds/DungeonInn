using System;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Domain.Map;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActiveLayerNavigationInvalidator : IInitializable, IDisposable
    {
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly IActorNavigationService navigationService;

        [Inject]
        public WorldActiveLayerNavigationInvalidator(
            MapLayerViewRegistry layerViewRegistry,
            IActorNavigationService navigationService)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
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
            navigationService.InvalidateLayerPaths(layerId);
        }
    }
}
