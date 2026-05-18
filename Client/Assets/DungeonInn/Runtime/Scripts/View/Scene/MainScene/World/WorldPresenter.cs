using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.World;
using R3;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldPresenter : IWorldPresenter, IDisposable
    {
        readonly IEventSubscriber eventSubscriber;
        readonly WorldMapView worldMapView;
        DisposableBag bag;

        [Inject]
        public WorldPresenter(
            IEventSubscriber eventSubscriber,
            WorldMapView worldMapView)
        {
            this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
            this.worldMapView = worldMapView ?? throw new ArgumentNullException(nameof(worldMapView));
        }

        void IWorldPresenter.Setup()
        {
            eventSubscriber.OnEvent<MapLayerAddedEvent>()
                .Subscribe(OnMapLayerAdded)
                .AddTo(ref bag);

            eventSubscriber.OnEvent<DungeonFloorRegeneratedEvent>()
                .Subscribe(OnDungeonFloorRegenerated)
                .AddTo(ref bag);
        }

        void IWorldPresenter.OnEnter()
        {
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnMapLayerAdded(MapLayerAddedEvent gameEvent)
        {
            worldMapView.NotifyLayerAdded(gameEvent.LayerId);
        }

        void OnDungeonFloorRegenerated(DungeonFloorRegeneratedEvent gameEvent)
        {
            worldMapView.InvalidateLayer(gameEvent.LayerId);
        }
    }
}
