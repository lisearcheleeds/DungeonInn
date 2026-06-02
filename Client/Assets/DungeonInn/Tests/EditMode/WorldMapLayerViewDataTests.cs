using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using DungeonInn.View.Scene;
using DungeonInn.View.Scene.Bridge;
using DungeonInn.View.Scene.MainScene.World;
using DungeonInn.View.Scene.ModuleScene.GameHUD;
using LighthouseExtends.Addressable;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            var layerData = new WorldMapLayerViewData(MapLayerId.Ground, "Ground", 2, 2, 1f, cellKinds);

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
            var store = ActorViewDataStoreTestFactory.Create();
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
            var store = ActorViewDataStoreTestFactory.Create();
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
            var store = ActorViewDataStoreTestFactory.Create();
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

        [Test]
        public void ActorStatusViewAppliesHpRatioToWorldSpaceFillRenderer()
        {
            var viewObject = new GameObject("ActorStatusView");
            var hpBarObject = new GameObject("HpBar");
            hpBarObject.transform.SetParent(viewObject.transform, false);
            var view = viewObject.AddComponent<ActorStatusView>();
            var hpBarRenderer = hpBarObject.AddComponent<SpriteRenderer>();
            view.EditorAssign(null, hpBarRenderer, Array.Empty<SpriteRenderer>());

            try
            {
                view.SetHpRatio(0.25f);

                Assert.That(hpBarRenderer.color.a, Is.EqualTo(0.25f).Within(0.001f));
                Assert.That(hpBarRenderer.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(hpBarRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
            }
        }

        [Test]
        public void ActorStatusViewAppliesScreenScaleFromWorldUnitsPerPixel()
        {
            var viewObject = new GameObject("ActorStatusViewScale");
            var hpBarObject = new GameObject("HpBar");
            hpBarObject.transform.SetParent(viewObject.transform, false);
            var view = viewObject.AddComponent<ActorStatusView>();
            var hpBarRenderer = hpBarObject.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(10, 10, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 10f, 10f),
                new Vector2(0.5f, 0.5f),
                10f);
            hpBarRenderer.sprite = sprite;
            hpBarRenderer.drawMode = SpriteDrawMode.Sliced;
            hpBarRenderer.size = new Vector2(0.8f, 0.08f);
            view.EditorAssign(null, hpBarRenderer, Array.Empty<SpriteRenderer>());

            try
            {
                view.SetScreenScale(0.02f);

                Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(hpBarRenderer.size.x, Is.EqualTo(2.4f).Within(0.0001f));
                Assert.That(hpBarRenderer.size.y, Is.EqualTo(0.56f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(viewObject);
            }
        }

        [TestCaseSource(nameof(ActorBehaviorTypeCases))]
        public void ActorViewDataStoreMapsBehaviorToViewBehaviorType(
            IActorBehavior behavior,
            ActorBehaviorType expectedType)
        {
            var store = ActorViewDataStoreTestFactory.Create();
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
            var cameraController = new WorldCameraController(new FixedWorldCameraSettingsRepository(CreateWorldCameraSettingsForPresenterTest()));
            cameraController.BindCamera(camera);
            cameraController.UpdateCamera(0f);
            var visualDefinitionLoader = new ActorVisualDefinitionLoader(
                new ThrowingAssetManager(),
                new HardcodedMasterRepository());
            var presenter = new WorldActorPresenter(
                new FixedActorViewDataProvider(
                    new ActorViewData(
                        actorId,
                        actorPosition,
                        ActorBehaviorType.Adventurer,
                        "adventurer_novice")),
                new FixedActorViewDataProvider(
                    new ActorViewData(
                        actorId,
                        actorPosition,
                        ActorBehaviorType.Adventurer,
                        "adventurer_novice")),
                new LayerPositionViewMapper(new FixedLayerPositionViewSettingsRepository(new LayerPositionViewSettings(-240f, 0f))),
                registry,
                visualDefinitionLoader,
                cameraController,
                new ActorCombatAnimationPresenter(TestEventSubscriber.Instance));

            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[ActorVisualDefinitionLoader] Failed to load actor visual definition. VisualId=adventurer_novice SkinId=0 Error=Specified method is not supported.");

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
                visualDefinitionLoader.Dispose();
                layerRegistry.Dispose();
                viewRoot.Dispose();
            }
        }

        [Test]
        public void WorldActorPresenterRefreshesActiveLayerActorPositionWithoutDirtyChange()
        {
            var actorId = Guid.NewGuid();
            var actorPosition = new LayerPosition(MapLayerId.DungeonFloor(1), 8f, 9f);
            var viewRoot = new WorldViewRoot();
            var layerRegistry = new MapLayerViewRegistry(viewRoot);
            var prefabSource = new ActorPrefabSource(null);
            var pool = new WorldActorViewPool(prefabSource);
            var registry = new WorldActorViewRegistry(pool, layerRegistry);
            var cameraObject = new GameObject("WorldActorPresenterRefreshLayerCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var cameraController = new WorldCameraController(new FixedWorldCameraSettingsRepository(CreateWorldCameraSettingsForPresenterTest()));
            cameraController.BindCamera(camera);
            cameraController.UpdateCamera(0f);
            var visualDefinitionLoader = new ActorVisualDefinitionLoader(
                new ThrowingAssetManager(),
                new HardcodedMasterRepository());
            var provider = new FixedActorViewDataProvider(
                Array.Empty<ActorViewData>(),
                new[]
                {
                    new ActorViewData(
                        actorId,
                        actorPosition,
                        ActorBehaviorType.Adventurer,
                        "adventurer_novice")
                });
            var presenter = new WorldActorPresenter(
                provider,
                provider,
                new LayerPositionViewMapper(new FixedLayerPositionViewSettingsRepository(new LayerPositionViewSettings(-240f, 0f))),
                registry,
                visualDefinitionLoader,
                cameraController,
                new ActorCombatAnimationPresenter(TestEventSubscriber.Instance));

            try
            {
                presenter.UpdateVisuals();
                var actorView = registry.GetOrCreateActorView(actorId, actorPosition, out var createdBeforeRefresh);
                Assert.That(createdBeforeRefresh, Is.True);
                Assert.That(actorView.transform.localPosition.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(actorView.transform.localPosition.z, Is.EqualTo(0f).Within(0.0001f));

                presenter.RefreshLayerActors(MapLayerId.DungeonFloor(1));

                Assert.That(actorView.transform.localPosition.x, Is.EqualTo(8f).Within(0.0001f));
                Assert.That(actorView.transform.localPosition.z, Is.EqualTo(9f).Within(0.0001f));
                Assert.That(actorView.transform.parent, Is.EqualTo(layerRegistry.GetOrCreateActorRoot(MapLayerId.DungeonFloor(1))));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                registry.Dispose();
                pool.Dispose();
                prefabSource.Dispose();
                visualDefinitionLoader.Dispose();
                layerRegistry.Dispose();
                viewRoot.Dispose();
            }
        }

        [Test]
        public void WorldActorWorldAnchorProviderReturnsAnchorWithoutVisibilityCulling()
        {
            var layerPosition = new LayerPosition(MapLayerId.Ground, 7.5f, 10.5f);
            var viewRoot = new WorldViewRoot();
            var layerRegistry = new MapLayerViewRegistry(viewRoot);
            var prefabSource = new ActorPrefabSource(null);
            var pool = new WorldActorViewPool(prefabSource);
            var actorRegistry = new WorldActorViewRegistry(pool, layerRegistry);
            var positionMapper = new LayerPositionViewMapper(
                new FixedLayerPositionViewSettingsRepository(new LayerPositionViewSettings(-240f, 0f)));
            var provider = new WorldActorWorldAnchorProvider(
                new WorldCameraController(new FixedWorldCameraSettingsRepository(CreateWorldCameraSettingsForPresenterTest())),
                positionMapper,
                layerRegistry,
                actorRegistry);

            try
            {
                var actorId = Guid.NewGuid();
                var actorView = actorRegistry.GetOrCreateActorView(actorId, layerPosition, out _);
                actorView.UpdateFacing(layerPosition);
                var actorRoot = layerRegistry.GetOrCreateActorRoot(layerPosition.LayerId);
                var expectedWorldPosition = actorRoot.TransformPoint(
                    positionMapper.ToActorLayerLocalPosition(layerPosition) + Vector3.up * 1.15f);

                var result = provider.TryGetWorldAnchor(actorId, out var worldPosition, out var layerId);

                Assert.That(result, Is.True);
                Assert.That(layerId.Value, Is.EqualTo(layerPosition.LayerId.Value));
                Assert.That(worldPosition.x, Is.EqualTo(expectedWorldPosition.x).Within(0.0001f));
                Assert.That(worldPosition.y, Is.EqualTo(expectedWorldPosition.y).Within(0.0001f));
                Assert.That(worldPosition.z, Is.EqualTo(expectedWorldPosition.z).Within(0.0001f));
            }
            finally
            {
                actorRegistry.Dispose();
                pool.Dispose();
                prefabSource.Dispose();
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
                new MapMeshBuildService(tileConfig, materialSet, new FixedWorldGameSettingsRepository()),
                new NavMeshBuildService(layerRegistry),
                new EnvironmentObjectPlacer(
                    new WorldAddressableViewFactory(new ThrowingAssetManager(), new HardcodedMasterRepository()),
                    new HardcodedMasterRepository()),
                new FixedWorldGameSettingsRepository());

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
            var store = ActorViewDataStoreTestFactory.Create();
            store.SyncActor(groundActor);
            store.SyncActor(dungeonActor);
            var worldState = new ActorStatusWorldState(groundActor, dungeonActor);
            var hudObject = new GameObject("GameHUDTest");
            var gameHUDModuleScene = hudObject.AddComponent<GameHUDModuleScene>();
            var actorEffectIconSpriteCatalog = hudObject.AddComponent<ActorEffectIconSpriteCatalog>();
            var viewFactory = new GameHUDViewFactory(new ThrowingAssetManager());
            var actorStatusPrefab = CreateActorStatusViewPrefab();
            AssignActorStatusViewPrefab(viewFactory, actorStatusPrefab);
            var pool = new ActorStatusViewPool(viewFactory, gameHUDModuleScene);
            var activeLayerProvider = new TestActiveLayerProvider(MapLayerId.Ground.Value);
            var anchorProvider = new TestActorWorldAnchorProvider();
            var presenter = new WorldActorStatusPresenter(
                store,
                new GetActorStatusSummaryQuery(worldState, new HardcodedMasterRepository()),
                pool,
                anchorProvider,
                anchorProvider,
                activeLayerProvider,
                actorEffectIconSpriteCatalog);

            try
            {
                presenter.UpdatePositions();

                Assert.That(pool.TryGetActive(groundActor.Id, out _), Is.True);
                Assert.That(pool.TryGetActive(dungeonActor.Id, out _), Is.False);

                activeLayerProvider.ActiveLayerId = MapLayerId.DungeonFloor(1).Value;
                presenter.UpdatePositions();

                Assert.That(pool.TryGetActive(groundActor.Id, out _), Is.False);
                Assert.That(pool.TryGetActive(dungeonActor.Id, out _), Is.True);
            }
            finally
            {
                pool.Dispose();
                UnityEngine.Object.DestroyImmediate(actorStatusPrefab.gameObject);
                UnityEngine.Object.DestroyImmediate(hudObject);
            }
        }

        [Test]
        public void ActorStatusViewPoolRequiresLoadedPrefab()
        {
            var hudObject = new GameObject("GameHUDMissingPrefabTest");
            var gameHUDModuleScene = hudObject.AddComponent<GameHUDModuleScene>();
            var viewFactory = new GameHUDViewFactory(new ThrowingAssetManager());
            var pool = new ActorStatusViewPool(viewFactory, gameHUDModuleScene);

            try
            {
                Assert.Throws<InvalidOperationException>(() => pool.Rent(Guid.NewGuid()));
                Assert.That(GameObject.Find("ActorStatusView_Fallback"), Is.Null);
            }
            finally
            {
                pool.Dispose();
                UnityEngine.Object.DestroyImmediate(hudObject);
            }
        }

        [Test]
        public void DamageNumberViewPoolRequiresLoadedPrefab()
        {
            var hudObject = new GameObject("GameHUDMissingDamagePrefabTest");
            var gameHUDModuleScene = hudObject.AddComponent<GameHUDModuleScene>();
            var viewFactory = new GameHUDViewFactory(new ThrowingAssetManager());
            var pool = new DamageNumberViewPool(viewFactory, gameHUDModuleScene, new TestActorWorldAnchorProvider());

            try
            {
                Assert.Throws<InvalidOperationException>(() => pool.Spawn(8, Vector3.one));
                Assert.That(GameObject.Find("DamageNumberView_Fallback"), Is.Null);
            }
            finally
            {
                pool.Dispose();
                UnityEngine.Object.DestroyImmediate(hudObject);
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
            var cameraController = new WorldCameraController(new FixedWorldCameraSettingsRepository(CreateWorldCameraSettings()));
            var followController = new WorldActorCameraFollowController(
                selectionService,
                cameraController,
                new FixedWorldCameraSettingsRepository(CreateWorldCameraSettings()),
                worldState,
                new LayerPositionViewMapper(new FixedLayerPositionViewSettingsRepository(new LayerPositionViewSettings(-240f, 0f))),
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
                    new MonsterBehavior(1),
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
                1f,
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

        static ActorStatusView CreateActorStatusViewPrefab()
        {
            var viewObject = new GameObject("ActorStatusViewPrefab");
            var view = viewObject.AddComponent<ActorStatusView>();
            var billboardRoot = new GameObject("BillboardRoot").transform;
            billboardRoot.SetParent(viewObject.transform, false);
            var hpBar = new GameObject("HpBar").AddComponent<SpriteRenderer>();
            hpBar.transform.SetParent(billboardRoot, false);
            var statusIcons = new SpriteRenderer[4];
            for (var index = 0; index < statusIcons.Length; index++)
            {
                statusIcons[index] = new GameObject($"StatusIcon{index + 1}").AddComponent<SpriteRenderer>();
                statusIcons[index].transform.SetParent(billboardRoot, false);
            }

            view.EditorAssign(billboardRoot, hpBar, statusIcons);
            return view;
        }

        static void AssignActorStatusViewPrefab(GameHUDViewFactory viewFactory, ActorStatusView prefab)
        {
            typeof(GameHUDViewFactory)
                .GetField("<ActorStatusViewPrefab>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(viewFactory, prefab);
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

        sealed class ActorStatusWorldState : IGameWorldStateReader, IActorSelectionCandidateProvider
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

            public void CopySelectionCandidatesTo(List<ActorViewData> results)
            {
                results.Clear();
                foreach (var actor in actors)
                {
                    results.Add(new ActorViewData(actor.Id, actor.Position, ActorBehaviorType.None, "dummy"));
                }
            }

            public void CopyActorIdsTo(List<Guid> results)
            {
                results.Clear();
                foreach (var id in actorsById.Keys)
                {
                    results.Add(id);
                }
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
                    1f,
                    cells);
            }

            public void InvalidateLayer(MapLayerId layerId)
            {
            }
        }

        sealed class FixedActorViewDataProvider : IActorViewDataProvider, IActorStatusViewDataProvider
        {
            readonly ActorViewDataChangeBuffer initialChanges;
            readonly IReadOnlyList<ActorViewData> activeActors;
            bool consumed;

            public FixedActorViewDataProvider(ActorViewData actor)
                : this(new[] { actor }, new[] { actor })
            {
            }

            public FixedActorViewDataProvider(
                IReadOnlyList<ActorViewData> initialChangedActors,
                IReadOnlyList<ActorViewData> activeActors)
            {
                initialChanges = new ActorViewDataChangeBuffer(
                    initialChangedActors,
                    Array.Empty<Guid>());
                this.activeActors = activeActors;
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

            public IReadOnlyList<Guid> ConsumeRemovedActorIds()
            {
                return Array.Empty<Guid>();
            }

            public void CopyActiveActorsTo(List<ActorViewData> results)
            {
                results.Clear();
                for (var i = 0; i < activeActors.Count; i++)
                {
                    results.Add(activeActors[i]);
                }
            }
        }

        sealed class TestActorWorldAnchorProvider : IActorWorldAnchorProvider, IWorldHudCameraProvider
        {
            public Quaternion CameraRotation => Quaternion.identity;
            public float WorldUnitsPerPixel => 0.02f;

            public bool TryGetWorldAnchor(Guid actorId, out Vector3 worldPosition, out MapLayerId layerId)
            {
                worldPosition = Vector3.up;
                layerId = MapLayerId.Ground;
                return true;
            }
        }

        sealed class TestActiveLayerProvider : IActiveLayerProvider
        {
            public TestActiveLayerProvider(int? activeLayerId)
            {
                ActiveLayerId = activeLayerId;
            }

            public int? ActiveLayerId { get; set; }
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


