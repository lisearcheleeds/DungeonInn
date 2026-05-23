using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using DungeonInn.View.Scene.MainScene.World;
using LighthouseExtends.Addressable;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
        public void WorldMapViewDataProviderReturnsCachedLayerByLayerId()
        {
            var dungeon = new Dungeon(123);
            var worldState = new TestWorldState(CreateGroundMap(), dungeon);
            var provider = new WorldMapViewDataProvider(worldState);

            var groundLayer = provider.GetLayer(MapLayerId.Ground);
            Assert.That(groundLayer.LayerId, Is.EqualTo(MapLayerId.Ground));

            var floor = CreateDungeonFloor(1);
            dungeon.AddFloor(floor);

            var dungeonLayer = provider.GetLayer(MapLayerId.DungeonFloor(1));
            Assert.That(dungeonLayer.LayerId, Is.EqualTo(MapLayerId.DungeonFloor(1)));
            Assert.That(
                dungeonLayer.GetCellKind(new GridPosition(0, 0)),
                Is.EqualTo(WorldMapCellViewKind.StairUp));
            Assert.That(
                dungeonLayer.GetCellKind(new GridPosition(1, 1)),
                Is.EqualTo(WorldMapCellViewKind.StairDown));

            var repeatedLayer = provider.GetLayer(MapLayerId.DungeonFloor(1));
            Assert.That(repeatedLayer, Is.EqualTo(dungeonLayer));
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
        public void ActorViewDataStoreConsumesStatusRemoval()
        {
            var store = new ActorViewDataStore();
            var actor = CreateActor(new LayerPosition(MapLayerId.Ground, 5f, 5f));

            store.SyncActor(actor);
            store.ConsumeRemovedActorIds();
            store.RemoveActor(actor.Id);
            var removedIds = store.ConsumeRemovedActorIds();

            Assert.That(removedIds.Count, Is.EqualTo(1));
            Assert.That(removedIds[0], Is.EqualTo(actor.Id));
            Assert.That(store.ConsumeRemovedActorIds(), Is.Empty);
        }

        [Test]
        public void GetActorStatusSummaryQueryReturnsClampedHpAndActiveEffectsOnly()
        {
            var repository = new HardcodedMasterRepository();
            var actor = CreateActor(new LayerPosition(MapLayerId.Ground, 5f, 5f));
            actor.ReceiveDamage(999);
            actor.AddActorEffect(repository.GetActorEffectMaster(1));
            actor.ActorEffects[0].Advance(999f);
            actor.ActorEffects[0].StatusEffects[0].Advance(999f);
            var worldState = new ActorStatusWorldState(actor);
            var query = new GetActorStatusSummaryQuery(worldState, repository);

            var statusData = query.Query(actor.Id);

            Assert.That(statusData.HasValue, Is.True);
            Assert.That(statusData.Value.ActorId, Is.EqualTo(actor.Id));
            Assert.That(statusData.Value.HpRatio, Is.EqualTo(0f));
            Assert.That(statusData.Value.ActiveEffects, Is.Empty);
        }

        [Test]
        public void GetActorStatusSummaryQueryReturnsNullForMissingActor()
        {
            var query = new GetActorStatusSummaryQuery(
                new ActorStatusWorldState(null),
                new HardcodedMasterRepository());

            Assert.That(query.Query(Guid.NewGuid()).HasValue, Is.False);
        }

        [Test]
        public void GetActorStatusSummaryQueryRefreshesCachedEffectRemainingSeconds()
        {
            var repository = new HardcodedMasterRepository();
            var actor = CreateActor(new LayerPosition(MapLayerId.Ground, 5f, 5f));
            actor.AddActorEffect(repository.GetActorEffectMaster(1));
            var worldState = new ActorStatusWorldState(actor);
            var query = new GetActorStatusSummaryQuery(worldState, repository);

            var first = query.Query(actor.Id);
            actor.ActorEffects[0].Advance(2.1f);
            var second = query.Query(actor.Id);

            Assert.That(first.HasValue, Is.True);
            Assert.That(second.HasValue, Is.True);
            Assert.That(
                first.Value.ActiveEffects[0].RemainingSeconds,
                Is.GreaterThan(second.Value.ActiveEffects[0].RemainingSeconds));
        }

        [TestCaseSource(nameof(ActorBehaviorTypeCases))]
        public void ActorViewDataStoreMapsBehaviorToViewBehaviorType(
            IActorBehavior behavior,
            ActorBehaviorType expectedType)
        {
            var store = new ActorViewDataStore();
            var actor = CreateActor(new LayerPosition(MapLayerId.Ground, 5f, 5f), behavior);

            store.SyncActor(actor);
            var changes = store.ConsumeChanges();

            Assert.That(changes.ChangedActors.Count, Is.EqualTo(1));
            Assert.That(changes.ChangedActors[0].BehaviorType, Is.EqualTo(expectedType));
        }

        [Test]
        public void WorldActorViewRegistryReusesPooledViewAcrossRepeatedSpawnAndDespawn()
        {
            var viewRoot = new WorldViewRoot();
            var layerRegistry = new MapLayerViewRegistry(viewRoot);
            var prefabSource = new ActorPrefabSource(null);
            var pool = new WorldActorViewPool(prefabSource);
            var registry = new WorldActorViewRegistry(pool, layerRegistry);

            try
            {
                var firstActorId = Guid.NewGuid();
                var firstView = registry.GetOrCreateActorView(
                    firstActorId,
                    new LayerPosition(MapLayerId.Ground, 1f, 1f),
                    out var firstCreated);

                for (var i = 0; i < 128; i++)
                {
                    registry.RemoveActorObject(firstActorId);
                    firstActorId = Guid.NewGuid();
                    firstView = registry.GetOrCreateActorView(
                        firstActorId,
                        new LayerPosition(MapLayerId.Ground, i, i),
                        out var repeatedCreated);

                    Assert.That(repeatedCreated, Is.True);
                }

                Assert.That(firstCreated, Is.True);
                Assert.That(pool.CreatedCount, Is.EqualTo(1));
                Assert.That(firstView.gameObject.activeSelf, Is.True);
            }
            finally
            {
                registry.Dispose();
                pool.Dispose();
                prefabSource.Dispose();
                layerRegistry.Dispose();
                var rootObject = GameObject.Find("WorldViewRoot");
                if (rootObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(rootObject);
                }
            }
        }

        [Test]
        public void WorldActorPresenterOffsetsActorLocalYByVisualSizeHalfHeight()
        {
            var actorId = Guid.NewGuid();
            var actorPosition = new LayerPosition(MapLayerId.Ground, 2f, 3f);
            var viewRoot = new WorldViewRoot();
            var layerRegistry = new MapLayerViewRegistry(viewRoot);
            var prefabSource = new ActorPrefabSource(null);
            var pool = new WorldActorViewPool(prefabSource);
            var registry = new WorldActorViewRegistry(pool, layerRegistry);
            var cameraObject = new GameObject("WorldActorPresenterBillboardCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var cameraController = new WorldCameraController(CreateWorldCameraSettingsForPresenterTest());
            cameraController.BindCamera(camera);
            cameraController.UpdateCamera(0f);
            var loader = new VisualConfigLoader(
                new ThrowingAssetManager(),
                new VisualConfigSettings(null, null));
            var actorSpriteVisualConfig = new ActorSpriteVisualConfig(loader);
            var presenter = new WorldActorPresenter(
                new FixedActorViewDataProvider(
                    new ActorViewData(actorId, actorPosition, ActorBehaviorType.Adventurer)),
                new LayerPositionViewMapper(new LayerPositionViewSettings(-240f, 0f)),
                registry,
                actorSpriteVisualConfig,
                cameraController,
                new ActorCombatAnimationPresenter(TestEventSubscriber.Instance));

            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[ActorSpriteVisualConfig] Using placeholder actor sprite. BehaviorType=Adventurer");

                presenter.UpdateVisuals();
                var actorView = registry.GetOrCreateActorView(actorId, actorPosition, out var created);

                Assert.That(created, Is.False);
                Assert.That(actorView.transform.localPosition.x, Is.EqualTo(2f).Within(0.0001f));
                Assert.That(actorView.transform.localPosition.y, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(actorView.transform.localPosition.z, Is.EqualTo(3f).Within(0.0001f));
                Assert.That(
                    Quaternion.Angle(actorView.transform.rotation, Quaternion.Euler(45f, 45f, 0f)),
                    Is.LessThan(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                registry.Dispose();
                pool.Dispose();
                prefabSource.Dispose();
                actorSpriteVisualConfig.Dispose();
                loader.Dispose();
                layerRegistry.Dispose();
                viewRoot.Dispose();
            }
        }

        [Test]
        public void WorldMapViewSkipsQueuedChunksFromInvalidatedLayerBuild()
        {
            var provider = new VersionedMapViewDataProvider();
            var viewRoot = new WorldViewRoot();
            var layerRegistry = new MapLayerViewRegistry(viewRoot);
            var loader = new VisualConfigLoader(
                new ThrowingAssetManager(),
                new VisualConfigSettings(null, null));
            var materialSet = new MapMaterialSet(loader);
            var tileConfig = new MapTileVisualConfig(materialSet);
            var mapView = new WorldMapView(
                provider,
                layerRegistry,
                new MapMeshBuildService(tileConfig, materialSet, DungeonInn.View.Scene.MainScene.World.WorldMapViewSettings.CreateDefault()),
                new NavMeshBuildService(layerRegistry),
                new EnvironmentObjectPlacer(
                    new WorldAddressableViewFactory(new ThrowingAssetManager(), new HardcodedMasterRepository()),
                    new HardcodedMasterRepository()),
                DungeonInn.View.Scene.MainScene.World.WorldMapViewSettings.CreateDefault());

            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[MapMaterialSet] Using fallback map material. Kind=GroundWalkable");

                mapView.NotifyLayerAdded(MapLayerId.Ground);
                mapView.UpdateVisuals();

                provider.AdvanceVersion();
                mapView.InvalidateLayer(MapLayerId.Ground);
                mapView.NotifyLayerAdded(MapLayerId.Ground);
                mapView.UpdateVisuals();
                mapView.UpdateVisuals();

                Assert.That(GameObject.Find("GroundV0_Chunk_16_0"), Is.Null);
                Assert.That(GameObject.Find("GroundV1_Chunk_0_0"), Is.Not.Null);
            }
            finally
            {
                mapView.Dispose();
                materialSet.Dispose();
                loader.Dispose();
                layerRegistry.Dispose();
                viewRoot.Dispose();
            }
        }

        [Test]
        public void EnvironmentObjectPlacerInvalidatesPropsByLayer()
        {
            var loader = new VisualConfigLoader(
                new ThrowingAssetManager(),
                new VisualConfigSettings(null, null));
            var masterRepository = new HardcodedMasterRepository();
            var placer = new EnvironmentObjectPlacer(
                new WorldAddressableViewFactory(new ThrowingAssetManager(), masterRepository),
                masterRepository);
            var groundRoot = new GameObject("GroundPropRoot");
            var dungeonRoot = new GameObject("DungeonPropRoot");

            try
            {
                placer.PlaceChunkProps(
                    groundRoot.transform,
                    CreatePropLayerData(MapLayerId.Ground),
                    0,
                    0,
                    2,
                    2);
                placer.PlaceChunkProps(
                    dungeonRoot.transform,
                    CreatePropLayerData(MapLayerId.DungeonFloor(1)),
                    0,
                    0,
                    2,
                    2);

                placer.InvalidateLayer(MapLayerId.Ground);

                Assert.That(groundRoot.transform.childCount, Is.EqualTo(0));
                Assert.That(dungeonRoot.transform.childCount, Is.EqualTo(2));
                Assert.That(GetEnvironmentPropLayerCount(placer), Is.EqualTo(1));
            }
            finally
            {
                placer.Dispose();
                loader.Dispose();
                UnityEngine.Object.DestroyImmediate(groundRoot);
                UnityEngine.Object.DestroyImmediate(dungeonRoot);
            }
        }

        [Test]
        public void ActorStatusPresenterShowsHudOnlyForActiveLayerActors()
        {
            var groundActor = CreateActor(new LayerPosition(MapLayerId.Ground, 1f, 1f));
            var dungeonActor = CreateActor(new LayerPosition(MapLayerId.DungeonFloor(1), 1f, 1f));
            var store = new ActorViewDataStore();
            store.SyncActor(groundActor);
            store.SyncActor(dungeonActor);
            var worldState = new ActorStatusWorldState(groundActor, dungeonActor);
            var viewRoot = new WorldViewRoot();
            var layerRegistry = new MapLayerViewRegistry(viewRoot);
            var canvasProvider = new WorldHudCanvasProvider();
            var viewFactory = new WorldAddressableViewFactory(
                new ThrowingAssetManager(),
                new HardcodedMasterRepository());
            var pool = new ActorHUDViewPool(viewFactory, canvasProvider);
            var presenter = new WorldActorStatusPresenter(
                store,
                new GetActorStatusSummaryQuery(worldState, new HardcodedMasterRepository()),
                pool,
                new WorldCameraController(CreateWorldCameraSettings()),
                new LayerPositionViewMapper(new LayerPositionViewSettings(-240f, 0f)),
                layerRegistry);

            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[World] WorldUIModuleScene.HUDCanvas was not found. Using a fallback HUD canvas for development/test execution.");
                canvasProvider.Initialize();
                layerRegistry.GetOrCreateActorRoot(MapLayerId.Ground);
                layerRegistry.GetOrCreateActorRoot(MapLayerId.DungeonFloor(1));

                presenter.UpdatePositions();

                Assert.That(pool.TryGetActive(groundActor.Id, out _), Is.True);
                Assert.That(pool.TryGetActive(dungeonActor.Id, out _), Is.False);

                layerRegistry.SelectNextLayer();
                presenter.UpdatePositions();

                Assert.That(pool.TryGetActive(groundActor.Id, out _), Is.False);
                Assert.That(pool.TryGetActive(dungeonActor.Id, out _), Is.True);
            }
            finally
            {
                pool.Dispose();
                canvasProvider.Dispose();
                layerRegistry.Dispose();
                viewRoot.Dispose();
            }
        }

        [Test]
        public void ActorCameraFollowControllerSelectsLayerWhenSelectedActorChangesFloor()
        {
            var actor = CreateActor(new LayerPosition(MapLayerId.Ground, 1f, 1f));
            var worldState = new ActorStatusWorldState(actor);
            var selectionService = new ActorSelectionService(worldState);
            var viewRoot = new WorldViewRoot();
            var layerRegistry = new MapLayerViewRegistry(viewRoot);
            var cameraController = new WorldCameraController(CreateWorldCameraSettings());
            var followController = new WorldActorCameraFollowController(
                selectionService,
                cameraController,
                CreateWorldCameraSettings(),
                worldState,
                new LayerPositionViewMapper(new LayerPositionViewSettings(-240f, 0f)),
                layerRegistry);

            try
            {
                layerRegistry.GetOrCreateActorRoot(MapLayerId.Ground);
                layerRegistry.GetOrCreateActorRoot(MapLayerId.DungeonFloor(1));
                followController.Initialize();

                selectionService.Select(actor.Id);
                followController.UpdateFollowPosition();

                Assert.That(layerRegistry.ActiveLayerId, Is.EqualTo(MapLayerId.Ground));

                actor.MoveTo(new LayerPosition(MapLayerId.DungeonFloor(1), 2f, 2f));
                followController.UpdateFollowPosition();

                Assert.That(layerRegistry.ActiveLayerId, Is.EqualTo(MapLayerId.DungeonFloor(1)));
            }
            finally
            {
                followController.Dispose();
                layerRegistry.Dispose();
                viewRoot.Dispose();
            }
        }

        static Actor CreateActor(LayerPosition position)
        {
            return CreateActor(
                position,
                new AdventurerBehavior(0, AdventurerLifecycleState.Arrived));
        }

        static Actor CreateActor(LayerPosition position, IActorBehavior behavior)
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
                behavior,
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static IEnumerable<TestCaseData> ActorBehaviorTypeCases()
        {
            yield return new TestCaseData(
                    new AdventurerBehavior(0, AdventurerLifecycleState.Arrived),
                    ActorBehaviorType.Adventurer)
                .SetName("Adventurer maps to Adventurer");
            yield return new TestCaseData(
                    new MonsterBehavior(1, Array.Empty<ActorDropEntry>()),
                    ActorBehaviorType.Monster)
                .SetName("Monster maps to Monster");
            yield return new TestCaseData(
                    new GuildStaffBehavior(Array.Empty<ItemStack>()),
                    ActorBehaviorType.GuildStaff)
                .SetName("GuildStaff maps to GuildStaff");
            yield return new TestCaseData(
                    new PetBehavior(Guid.NewGuid()),
                    ActorBehaviorType.Pet)
                .SetName("Pet maps to Pet");
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

        static WorldMapLayerViewData CreatePropLayerData(MapLayerId layerId)
        {
            var cellKinds = new[]
            {
                WorldMapCellViewKind.StairUp,
                WorldMapCellViewKind.GroundWalkable,
                WorldMapCellViewKind.GroundWalkable,
                WorldMapCellViewKind.StairDown
            };

            return new WorldMapLayerViewData(
                layerId,
                $"Layer{layerId.Value}",
                2,
                2,
                cellKinds);
        }

        static int GetEnvironmentPropLayerCount(EnvironmentObjectPlacer placer)
        {
            var field = typeof(EnvironmentObjectPlacer).GetField(
                "propsByLayer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var propsByLayer = (IReadOnlyDictionary<int, List<GameObject>>)field.GetValue(placer);
            return propsByLayer.Count;
        }

        static WorldCameraSettings CreateWorldCameraSettings()
        {
            return new WorldCameraSettings(
                Vector3.zero,
                initialPitchDegrees: 45f,
                initialYawDegrees: 45f,
                initialOrthographicSize: 48f,
                moveSpeed: 32f,
                rotationSensitivity: 0.2f,
                zoomSensitivity: 0.02f,
                minOrthographicSize: 12f,
                maxOrthographicSize: 120f,
                actorViewportMargin: 0.08f,
                actorSelectionZoomRatio: 0.2f);
        }

        static WorldCameraSettings CreateWorldCameraSettingsForPresenterTest()
        {
            return new WorldCameraSettings(
                new Vector3(-10f, 10f, -10f),
                initialPitchDegrees: 45f,
                initialYawDegrees: 45f,
                initialOrthographicSize: 48f,
                moveSpeed: 32f,
                rotationSensitivity: 0.2f,
                zoomSensitivity: 0.02f,
                minOrthographicSize: 12f,
                maxOrthographicSize: 120f,
                actorViewportMargin: 0.08f,
                actorSelectionZoomRatio: 0.2f);
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

        sealed class ActorStatusWorldState : IGameWorldStateReader
        {
            readonly IReadOnlyList<Actor> actors;
            readonly Dictionary<Guid, Actor> actorsById = new();

            public ActorStatusWorldState(params Actor[] actors)
            {
                this.actors = actors == null
                    ? Array.Empty<Actor>()
                    : actors.Where(actor => actor != null).ToArray();
                foreach (var actor in this.actors)
                {
                    actorsById.Add(actor.Id, actor);
                }
            }

            public bool IsInitialized => true;
            public AdventurerGuild Guild => null;
            public GroundMap GroundMap => null;
            public Dungeon Dungeon => null;
            public InnEconomyState InnEconomy { get; } = new();
            public IReadOnlyList<Actor> Actors => actors;
            public IReadOnlyList<ItemInstance> Items => Array.Empty<ItemInstance>();
            public IReadOnlyList<ProjectileInstance> Projectiles => Array.Empty<ProjectileInstance>();
            public IReadOnlyList<AreaEffectInstance> AreaEffects => Array.Empty<AreaEffectInstance>();
            public SpawnScheduleState SpawnSchedule { get; } = new();

            public Actor FindActor(Guid actorId)
            {
                return actorsById.TryGetValue(actorId, out var actor) ? actor : null;
            }
        }

        sealed class VersionedMapViewDataProvider : IWorldMapViewDataProvider
        {
            int version;

            public void AdvanceVersion()
            {
                version++;
            }

            public WorldMapLayerViewData GetLayer(MapLayerId layerId)
            {
                var cells = new WorldMapCellViewKind[32 * 16];
                for (var index = 0; index < cells.Length; index++)
                {
                    cells[index] = WorldMapCellViewKind.GroundWalkable;
                }

                return new WorldMapLayerViewData(
                    layerId,
                    $"GroundV{version}",
                    32,
                    16,
                    cells);
            }

            public void InvalidateLayer(MapLayerId layerId)
            {
            }
        }

        sealed class FixedActorViewDataProvider : IActorViewDataProvider
        {
            readonly ActorViewDataChangeBuffer initialChanges;
            bool consumed;

            public FixedActorViewDataProvider(ActorViewData actor)
            {
                initialChanges = new ActorViewDataChangeBuffer(
                    new[] { actor },
                    Array.Empty<Guid>());
            }

            public ActorViewDataChangeBuffer ConsumeChanges()
            {
                if (consumed)
                {
                    return new ActorViewDataChangeBuffer(
                        Array.Empty<ActorViewData>(),
                        Array.Empty<Guid>());
                }

                consumed = true;
                return initialChanges;
            }
        }

        sealed class ThrowingAssetManager : IAssetManager
        {
            public IAssetScope CreateScope()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
