using DungeonInn.Application.World;
using System;
using System.Linq;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class WorldMapLayerViewDataTests
    {
        [Test]
        public void LayerViewDataStoresCellKindsAsSnapshot()
        {
            var cellKinds = new[]
            {
                WorldMapCellViewKind.GroundWalkable,
                WorldMapCellViewKind.GroundBlocked,
                WorldMapCellViewKind.StairUp,
                WorldMapCellViewKind.StairDown
            };
            var layerData = new WorldMapLayerViewData(MapLayerId.Ground, "Ground", 2, 2, cellKinds);

            cellKinds[0] = WorldMapCellViewKind.DungeonBlocked;

            Assert.That(
                layerData.GetCellKind(new GridPosition(0, 0)),
                Is.EqualTo(WorldMapCellViewKind.GroundWalkable));
            Assert.That(
                layerData.GetCellKind(new GridPosition(1, 0)),
                Is.EqualTo(WorldMapCellViewKind.GroundBlocked));
            Assert.That(
                layerData.GetCellKind(new GridPosition(0, 1)),
                Is.EqualTo(WorldMapCellViewKind.StairUp));
            Assert.That(
                layerData.GetCellKind(new GridPosition(1, 1)),
                Is.EqualTo(WorldMapCellViewKind.StairDown));
        }

        [Test]
        public void LayerViewDataDoesNotStoreDeferredResolver()
        {
            var delegateFields = typeof(WorldMapLayerViewData)
                .GetFields(System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public)
                .Where(field => typeof(Delegate).IsAssignableFrom(field.FieldType))
                .Select(field => field.Name)
                .ToArray();

            Assert.That(delegateFields, Is.Empty);
        }

        [Test]
        public void ActorViewDataStoreConsumesOnlyChangedActors()
        {
            var store = new ActorViewDataStore();
            var actor = CreateActor(new LayerPosition(MapLayerId.Ground, 5f, 5f));

            store.SyncActor(actor);
            var initialChanges = store.ConsumeChanges();
            Assert.That(initialChanges.ChangedActors.Count, Is.EqualTo(1));
            Assert.That(initialChanges.ChangedActors[0].ActorId, Is.EqualTo(actor.Id));
            Assert.That(initialChanges.RemovedActorIds, Is.Empty);

            var noChanges = store.ConsumeChanges();
            Assert.That(noChanges.ChangedActors, Is.Empty);
            Assert.That(noChanges.RemovedActorIds, Is.Empty);

            actor.MoveTo(new LayerPosition(MapLayerId.DungeonFloor(1), 10f, 15f));
            store.SyncActor(actor);
            var moveChanges = store.ConsumeChanges();
            Assert.That(moveChanges.ChangedActors.Count, Is.EqualTo(1));
            Assert.That(moveChanges.ChangedActors[0].Position.LayerId, Is.EqualTo(MapLayerId.DungeonFloor(1)));
        }

        [Test]
        public void ActorViewDataStoreReportsRemovedActors()
        {
            var store = new ActorViewDataStore();
            var actor = CreateActor(new LayerPosition(MapLayerId.Ground, 5f, 5f));

            store.SyncActor(actor);
            store.RemoveActor(actor.Id);

            var changes = store.ConsumeChanges();
            Assert.That(changes.ChangedActors, Is.Empty);
            Assert.That(changes.RemovedActorIds.Count, Is.EqualTo(1));
            Assert.That(changes.RemovedActorIds[0], Is.EqualTo(actor.Id));
        }

        static Actor CreateActor(LayerPosition position)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                50,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, AdventurerLifecycleState.Arrived),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }
    }
}
