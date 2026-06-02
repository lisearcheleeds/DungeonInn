using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Map;
using DungeonInn.View.Scene.Bridge;
using DungeonInn.View.Scene.ModuleScene.GameHUD;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace DungeonInn.Tests.EditMode
{
    public sealed class DamageNumberPresenterTests
    {
        [Test]
        public void CombatDamageOnActiveLayerSpawnsDamageNumber()
        {
            var actorId = Guid.NewGuid();
            var eventBus = new FakeEventSubscriber();
            var spawner = new CapturingDamageNumberViewSpawner();
            var anchorProvider = new FixedActorWorldAnchorProvider(
                new Vector3(2f, 1f, 3f),
                MapLayerId.DungeonFloor(1));
            var activeLayerProvider = new FixedActiveLayerProvider(MapLayerId.DungeonFloor(1).Value);
            var presenter = new DamageNumberPresenter(
                eventBus,
                anchorProvider,
                activeLayerProvider,
                spawner);

            try
            {
                presenter.Initialize();

                eventBus.Publish(new CombatAttackOccurred(Guid.NewGuid(), actorId, 12, 8));

                Assert.That(spawner.SpawnCount, Is.EqualTo(1));
                Assert.That(spawner.LastDamage, Is.EqualTo(12));
                Assert.That(spawner.LastWorldPosition, Is.EqualTo(new Vector3(2f, 1f, 3f)));
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void CombatDamageOnInactiveLayerDoesNotSpawnDamageNumber()
        {
            var actorId = Guid.NewGuid();
            var eventBus = new FakeEventSubscriber();
            var spawner = new CapturingDamageNumberViewSpawner();
            var presenter = new DamageNumberPresenter(
                eventBus,
                new FixedActorWorldAnchorProvider(
                    new Vector3(2f, 1f, 3f),
                    MapLayerId.DungeonFloor(1)),
                new FixedActiveLayerProvider(MapLayerId.Ground.Value),
                spawner);

            try
            {
                presenter.Initialize();

                eventBus.Publish(new CombatAttackOccurred(Guid.NewGuid(), actorId, 12, 8));

                Assert.That(spawner.SpawnCount, Is.EqualTo(0));
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void ZeroDamageDoesNotSpawnDamageNumber()
        {
            var actorId = Guid.NewGuid();
            var eventBus = new FakeEventSubscriber();
            var spawner = new CapturingDamageNumberViewSpawner();
            var presenter = new DamageNumberPresenter(
                eventBus,
                new FixedActorWorldAnchorProvider(
                    new Vector3(2f, 1f, 3f),
                    MapLayerId.DungeonFloor(1)),
                new FixedActiveLayerProvider(MapLayerId.DungeonFloor(1).Value),
                spawner);

            try
            {
                presenter.Initialize();

                eventBus.Publish(new CombatAttackOccurred(Guid.NewGuid(), actorId, 0, 8));

                Assert.That(spawner.SpawnCount, Is.EqualTo(0));
            }
            finally
            {
                presenter.Dispose();
            }
        }

        sealed class FakeEventSubscriber : IEventSubscriber, IDisposable
        {
            readonly Subject<IGameEvent> subject = new();

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return subject.Where(gameEvent => gameEvent is T).Select(gameEvent => (T)(object)gameEvent);
            }

            public void Publish(IGameEvent gameEvent)
            {
                subject.OnNext(gameEvent);
            }

            public void Dispose()
            {
                subject.Dispose();
            }
        }

        sealed class FixedActorWorldAnchorProvider : IActorWorldAnchorProvider
        {
            readonly Vector3 worldPosition;
            readonly MapLayerId layerId;

            public FixedActorWorldAnchorProvider(Vector3 worldPosition, MapLayerId layerId)
            {
                this.worldPosition = worldPosition;
                this.layerId = layerId;
            }

            public bool TryGetWorldAnchor(Guid actorId, out Vector3 worldPosition, out MapLayerId layerId)
            {
                worldPosition = this.worldPosition;
                layerId = this.layerId;
                return true;
            }
        }

        sealed class FixedActiveLayerProvider : IActiveLayerProvider
        {
            public FixedActiveLayerProvider(int? activeLayerId)
            {
                ActiveLayerId = activeLayerId;
            }

            public int? ActiveLayerId { get; }
        }

        sealed class CapturingDamageNumberViewSpawner : IDamageNumberViewSpawner
        {
            public int SpawnCount { get; private set; }
            public int LastDamage { get; private set; }
            public Vector3 LastWorldPosition { get; private set; }

            public void Spawn(int damage, Vector3 worldPosition)
            {
                SpawnCount++;
                LastDamage = damage;
                LastWorldPosition = worldPosition;
            }

            public void Tick(float deltaSeconds)
            {
            }
        }
    }
}
