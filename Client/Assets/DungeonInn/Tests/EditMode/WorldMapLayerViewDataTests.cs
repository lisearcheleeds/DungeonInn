using DungeonInn.Application.World;
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using DungeonInn.View.Scene.MainScene.World;
using NUnit.Framework;
using UnityEngine;

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
        public void WorldMapViewDataProviderAddsCachedLayerForAddedDungeonFloor()
        {
            var dungeon = new Dungeon(123);
            var worldState = new TestWorldState(CreateGroundMap(), dungeon);
            var provider = new WorldMapViewDataProvider(worldState);

            var initialLayers = provider.GetLayers();
            Assert.That(initialLayers.Count, Is.EqualTo(1));
            Assert.That(initialLayers[0].LayerId, Is.EqualTo(MapLayerId.Ground));

            var floor = CreateDungeonFloor(1);
            dungeon.AddFloor(floor);

            var layersAfterFloorAdded = provider.GetLayers();
            Assert.That(layersAfterFloorAdded.Count, Is.EqualTo(2));
            Assert.That(layersAfterFloorAdded[1].LayerId, Is.EqualTo(MapLayerId.DungeonFloor(1)));
            Assert.That(
                layersAfterFloorAdded[1].GetCellKind(new GridPosition(0, 0)),
                Is.EqualTo(WorldMapCellViewKind.StairUp));
            Assert.That(
                layersAfterFloorAdded[1].GetCellKind(new GridPosition(1, 1)),
                Is.EqualTo(WorldMapCellViewKind.StairDown));

            var repeatedLayers = provider.GetLayers();
            Assert.That(repeatedLayers[1], Is.EqualTo(layersAfterFloorAdded[1]));
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

        [Test]
        public void WorldActorViewRegistryReusesPooledViewAcrossRepeatedSpawnAndDespawn()
        {
            var viewRoot = new WorldViewRoot();
            var layerRegistry = new MapLayerViewRegistry(viewRoot);
            var pool = new WorldActorViewPool();
            var registry = new WorldActorViewRegistry(pool, layerRegistry);

            try
            {
                var firstActorId = Guid.NewGuid();
                var firstView = registry.GetOrCreateActorView(
                    firstActorId,
                    new LayerPosition(MapLayerId.Ground, 1f, 1f),
                    null,
                    out var firstCreated);

                for (var i = 0; i < 128; i++)
                {
                    registry.RemoveActorObject(firstActorId);
                    firstActorId = Guid.NewGuid();
                    firstView = registry.GetOrCreateActorView(
                        firstActorId,
                        new LayerPosition(MapLayerId.Ground, i, i),
                        null,
                        out var repeatedCreated);

                    Assert.That(repeatedCreated, Is.True);
                }

                Assert.That(firstCreated, Is.True);
                Assert.That(pool.CreatedCount, Is.EqualTo(1));
                Assert.That(firstView.ActorObject.activeSelf, Is.True);
            }
            finally
            {
                registry.Dispose();
                pool.Dispose();
                layerRegistry.Dispose();
                var rootObject = GameObject.Find("WorldViewRoot");
                if (rootObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(rootObject);
                }
            }
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

        static GroundMap CreateGroundMap()
        {
            var layer = new MapLayer(MapLayerId.Ground, 2, 2, 1f);
            var cells = new[]
            {
                new GroundCell(new GridPosition(0, 0), GroundCellType.Open, MapCellBlockType.Walkable),
                new GroundCell(new GridPosition(1, 0), GroundCellType.Open, MapCellBlockType.Walkable),
                new GroundCell(new GridPosition(0, 1), GroundCellType.Open, MapCellBlockType.Walkable),
                new GroundCell(new GridPosition(1, 1), GroundCellType.Open, MapCellBlockType.Walkable)
            };

            return new GroundMap(layer, new GridPosition(0, 0), cells);
        }

        static DungeonFloor CreateDungeonFloor(int floorIndex)
        {
            var layer = new MapLayer(MapLayerId.DungeonFloor(floorIndex), 2, 2, 1f);
            var cells = new[]
            {
                new DungeonCell(new GridPosition(0, 0), DungeonCellType.Room),
                new DungeonCell(new GridPosition(1, 0), DungeonCellType.Corridor),
                new DungeonCell(new GridPosition(0, 1), DungeonCellType.Wall),
                new DungeonCell(new GridPosition(1, 1), DungeonCellType.Room)
            };

            return new DungeonFloor(
                floorIndex,
                layer,
                cells,
                new DungeonStair(DungeonStairType.Up, new GridPosition(0, 0)),
                new DungeonStair(DungeonStairType.Down, new GridPosition(1, 1)),
                Array.Empty<DungeonRoom>(),
                new DungeonFloorGenerationSettings(0));
        }

        sealed class TestWorldState : IGameWorldStateReader
        {
            public TestWorldState(GroundMap groundMap, Dungeon dungeon)
            {
                GroundMap = groundMap;
                Dungeon = dungeon;
            }

            public bool IsInitialized => true;
            public AdventurerGuild Guild => null;
            public GroundMap GroundMap { get; }
            public Dungeon Dungeon { get; }
            public InnEconomyState InnEconomy { get; } = new();
            public IReadOnlyList<Actor> Actors => Array.Empty<Actor>();
            public IReadOnlyList<ItemInstance> Items => Array.Empty<ItemInstance>();
            public IReadOnlyList<ProjectileInstance> Projectiles => Array.Empty<ProjectileInstance>();
            public IReadOnlyList<AreaEffectInstance> AreaEffects => Array.Empty<AreaEffectInstance>();
            public SpawnScheduleState SpawnSchedule { get; } = new();

            public Actor FindActor(Guid actorId)
            {
                return null;
            }
        }
    }
}
