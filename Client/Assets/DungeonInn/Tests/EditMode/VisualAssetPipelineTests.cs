using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using DungeonInn.GameSession.Settings;
using DungeonInn.View.Scene.ModuleScene.GameHUD;
using DungeonInn.View.Scene.MainScene.World;
using LighthouseExtends.Addressable;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonInn.Tests.EditMode
{
    public sealed class VisualAssetPipelineTests
    {
        const string MapMaterialSetPath = "Assets/DungeonInn/Runtime/StaticResources/Visual/MapMaterialSet.asset";
        const string LayerPositionViewSettingsPath = "Assets/DungeonInn/Runtime/StaticResources/Visual/LayerPositionViewSettings.asset";
        const string WorldCameraSettingsPath = "Assets/DungeonInn/Runtime/StaticResources/Visual/WorldCameraSettings.asset";
        const string WorldGameSettingsPath = "Assets/DungeonInn/Runtime/StaticResources/Visual/WorldGameSettings.asset";
        const string ActorViewPrefabPath = "Assets/DungeonInn/Runtime/Prefab/World/ActorView.prefab";
        const string ActorStatusViewPrefabPath = "Assets/DungeonInn/Runtime/Prefab/GameHUD/ActorStatusView.prefab";
        const string DamageNumberViewPrefabPath = "Assets/DungeonInn/Runtime/Prefab/GameHUD/DamageNumberView.prefab";
        const string BaseSpritePath = "Assets/DungeonInn/Runtime/StaticResources/Scene/Common/Sprite/BaseSprite.png";
        const string DamageDigitsPath = "Assets/DungeonInn/Runtime/Art/Sprites/UI/DamageDigits.png";
        const string GameHUDSortingLayerName = "GameHUD";
        const string GameHUDOverlaySortingLayerName = "GameHUDOverlay";
        const string AddressablesGroupName = "DungeonInn Visual";

        [Test]
        public void MapMaterialSetAssetContainsEveryTileVisualKindAndAddress()
        {
            var mapMaterialSet = AssetDatabase.LoadAssetAtPath<MapMaterialSetSO>(MapMaterialSetPath);
            Assert.That(mapMaterialSet, Is.Not.Null);

            using var serialized = new SerializedObject(mapMaterialSet);
            var entries = serialized.FindProperty("entries");
            var expectedKinds = Enum.GetValues(typeof(TileVisualKind)).Cast<TileVisualKind>().ToArray();
            var actualKinds = new HashSet<TileVisualKind>();

            for (var index = 0; index < entries.arraySize; index++)
            {
                var entry = entries.GetArrayElementAtIndex(index);
                var kind = (TileVisualKind)entry.FindPropertyRelative("Kind").enumValueIndex;
                var address = entry.FindPropertyRelative("MaterialAddress").stringValue;
                actualKinds.Add(kind);
                Assert.That(address, Is.Not.Empty, $"Material address is empty. Kind={kind}");
            }

            Assert.That(actualKinds, Is.EquivalentTo(expectedKinds));
        }

        [Test]
        public void DungeonInnVisualAddressablesGroupContainsSchemasAndVisualAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            Assert.That(settings, Is.Not.Null);

            var group = settings.FindGroup(AddressablesGroupName);
            Assert.That(group, Is.Not.Null);
            Assert.That(group.GetSchema<BundledAssetGroupSchema>(), Is.Not.Null);
            Assert.That(group.GetSchema<ContentUpdateGroupSchema>(), Is.Not.Null);

            var addresses = group.entries.Select(entry => entry.address).ToHashSet();
            foreach (var name in new[] { "GroundWalkable", "GroundBlocked", "DungeonWalkable", "DungeonBlocked", "StairUp", "StairDown", "Facility" })
            {
                Assert.That(addresses, Contains.Item($"Materials/Map/{name}"));
            }

            foreach (var actor in new[] { "Adventurer", "Goblin" })
            {
                foreach (var direction in new[] { "NE", "NW", "SE", "SW" })
                {
                    Assert.That(addresses, Contains.Item($"Sprites/{actor}/Idle{direction}"));
                    Assert.That(addresses, Contains.Item($"Sprites/{actor}/Walk{direction}1"));
                    Assert.That(addresses, Contains.Item($"Sprites/{actor}/Walk{direction}2"));
                }
            }

            foreach (var actorVisual in new[]
            {
                "AdventurerNovice",
                "MonsterGoblin",
                "MonsterOrc",
                "MonsterOgre",
                "MonsterGoblinArcher"
            })
            {
                Assert.That(addresses, Contains.Item($"World/ActorVisual/{actorVisual}"));
            }

            foreach (var hudAddress in new[]
            {
                "GameHUD/ActorStatusView",
                "GameHUD/DamageNumberView",
                "GameUI/SelectedActorInspectorView",
                "GameUI/PlayerEventLogView",
                "GameUI/WorldHudView",
                "GameUI/InnStatusPanelView",
                "GameUI/MinimapView"
            })
            {
                Assert.That(addresses, Contains.Item(hudAddress));
            }
        }

        [Test]
        public void DamageNumberViewPrefabReferencesDigitSprites()
        {
            var view = AssetDatabase.LoadAssetAtPath<DamageNumberView>(DamageNumberViewPrefabPath);
            Assert.That(view, Is.Not.Null);

            using var serialized = new SerializedObject(view);
            Assert.That(serialized.FindProperty("digitRoot").objectReferenceValue, Is.Not.Null);
            var digitTemplate = serialized.FindProperty("digitTemplate").objectReferenceValue as SpriteRenderer;
            Assert.That(digitTemplate, Is.Not.Null);
            Assert.That(digitTemplate.sprite, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(digitTemplate.sprite), Is.EqualTo(DamageDigitsPath));

            var digitSprites = serialized.FindProperty("digitSprites");
            Assert.That(digitSprites.arraySize, Is.EqualTo(10));
            for (var index = 0; index < digitSprites.arraySize; index++)
            {
                var digitSprite = digitSprites.GetArrayElementAtIndex(index).objectReferenceValue as Sprite;
                Assert.That(digitSprite, Is.Not.Null, $"digitSprites[{index}]");
                Assert.That(digitSprite.name, Is.EqualTo($"DamageDigit_{index}"));
                Assert.That(AssetDatabase.GetAssetPath(digitSprite), Is.EqualTo(DamageDigitsPath));
            }
        }

        [Test]
        public void ActorStatusViewPrefabReferencesRendererSprites()
        {
            var view = AssetDatabase.LoadAssetAtPath<ActorStatusView>(ActorStatusViewPrefabPath);
            Assert.That(view, Is.Not.Null);

            var renderers = view.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(renderers.Count(renderer => renderer.sprite != null), Is.EqualTo(renderers.Length));
        }

        [Test]
        public void ActorStatusViewPrefabUsesSingleShaderDrivenHpBar()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActorStatusViewPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            var hpBar = renderers.FirstOrDefault(renderer => renderer.name == "HpBar");
            var baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BaseSpritePath);

            Assert.That(hpBar, Is.Not.Null);
            Assert.That(hpBar.sprite, Is.EqualTo(baseSprite));
            Assert.That(hpBar.sharedMaterial, Is.Not.Null);
            Assert.That(hpBar.sharedMaterial.shader.name, Is.EqualTo("DungeonInn/GameHUD/HpBar"));
            Assert.That(hpBar.drawMode, Is.EqualTo(SpriteDrawMode.Sliced));
            Assert.That(hpBar.sortingLayerName, Is.EqualTo(GameHUDSortingLayerName));
            Assert.That(hpBar.sortingOrder, Is.EqualTo(100));
            Assert.That(renderers.Any(renderer => renderer.name == "HpBackground"), Is.False);
            Assert.That(renderers.Any(renderer => renderer.name == "HpFill"), Is.False);
            Assert.That(hpBar.size.x, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(hpBar.size.y, Is.EqualTo(0.28f).Within(0.0001f));
            Assert.That(CalculateVisualWidthHeightRatio(hpBar), Is.EqualTo(120f / 28f).Within(0.0001f));
        }

        [Test]
        public void ActorStatusViewHpRatioUpdatesRendererVertexAlpha()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActorStatusViewPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var view = instance.GetComponent<ActorStatusView>();
                var hpBar = instance.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(renderer => renderer.name == "HpBar");
                Assert.That(view, Is.Not.Null);
                Assert.That(hpBar, Is.Not.Null);

                var sizeBefore = hpBar.size;
                var positionBefore = hpBar.transform.localPosition;

                view.SetHpRatio(0.5f);

                Assert.That(hpBar.color.a, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(hpBar.size, Is.EqualTo(sizeBefore));
                Assert.That(hpBar.transform.localPosition, Is.EqualTo(positionBefore));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GameHUDSortingLayersExist()
        {
            Assert.That(SortingLayer.NameToID("World"), Is.Not.EqualTo(0));
            Assert.That(SortingLayer.NameToID("WorldEffect"), Is.Not.EqualTo(0));
            Assert.That(SortingLayer.NameToID(GameHUDSortingLayerName), Is.Not.EqualTo(0));
            Assert.That(SortingLayer.NameToID(GameHUDOverlaySortingLayerName), Is.Not.EqualTo(0));
        }

        [Test]
        public void DamageNumberViewPrefabUsesGameHUDOverlaySortingLayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DamageNumberViewPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var digitTemplate = prefab.GetComponentsInChildren<SpriteRenderer>(true)
                .FirstOrDefault(renderer => renderer.name == "DigitTemplate");

            Assert.That(digitTemplate, Is.Not.Null);
            Assert.That(digitTemplate.sharedMaterial, Is.Not.Null);
            Assert.That(digitTemplate.sharedMaterial.shader.name, Is.EqualTo("DungeonInn/GameHUD/SpriteOverlay"));
            Assert.That(digitTemplate.sortingLayerName, Is.EqualTo(GameHUDOverlaySortingLayerName));
            Assert.That(digitTemplate.sortingOrder, Is.EqualTo(200));
        }

        [Test]
        public void GameHUDPrefabsUseWorldRenderingLayer()
        {
            var worldLayer = LayerMask.NameToLayer("World");
            Assert.That(worldLayer, Is.GreaterThanOrEqualTo(0));

            foreach (var prefabPath in new[] { ActorStatusViewPrefabPath, DamageNumberViewPrefabPath })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab, Is.Not.Null, prefabPath);
                var transforms = prefab.GetComponentsInChildren<Transform>(true);
                Assert.That(
                    transforms.Select(transform => transform.gameObject.layer),
                    Is.All.EqualTo(worldLayer),
                    prefabPath);
            }
        }

        static float CalculateVisualWidthHeightRatio(SpriteRenderer renderer)
        {
            if (renderer.drawMode != SpriteDrawMode.Simple)
            {
                return renderer.size.x / renderer.size.y;
            }

            var width = renderer.sprite.bounds.size.x * Mathf.Abs(renderer.transform.localScale.x);
            var height = renderer.sprite.bounds.size.y * Mathf.Abs(renderer.transform.localScale.y);
            return width / height;
        }

        [Test]
        public void WorldViewSettingsAssetsExistAndConvertToRuntimeSettings()
        {
            var layerSettingsSo = AssetDatabase.LoadAssetAtPath<LayerPositionViewSettingsSO>(LayerPositionViewSettingsPath);
            var cameraSettingsSo = AssetDatabase.LoadAssetAtPath<WorldCameraSettingsSO>(WorldCameraSettingsPath);
            var gameSettingsSo = AssetDatabase.LoadAssetAtPath<WorldGameSettingsSO>(WorldGameSettingsPath);

            Assert.That(layerSettingsSo, Is.Not.Null);
            Assert.That(cameraSettingsSo, Is.Not.Null);
            Assert.That(gameSettingsSo, Is.Not.Null);

            var layerSettings = layerSettingsSo.ToSettings();
            var cameraSettings = cameraSettingsSo.ToSettings();
            var groundMapSettings = gameSettingsSo.ToGroundMapGenerationSettings();
            var dungeonMapSettings = gameSettingsSo.ToDungeonMapGenerationSettings();
            var initialWorldSettings = gameSettingsSo.ToInitialWorldSettings();
            var innBalanceSettings = gameSettingsSo.ToInnBalanceSettings();
            var actorSimulationSettings = gameSettingsSo.ToActorSimulationSettings();
            var spawnBalanceSettings = gameSettingsSo.ToSpawnBalanceSettings();
            var returnPolicySettings = gameSettingsSo.ToAdventurerReturnPolicySettings();
            var combatBalanceSettings = gameSettingsSo.ToCombatBalanceSettings();
            var worldMapViewSettings = new WorldMapViewSettings(
                gameSettingsSo.MapChunkTileSize,
                gameSettingsSo.MapChunkBuildsPerFrame,
                gameSettingsSo.MapTileHeightMeters);

            Assert.That(layerSettings.LayerHeightOffset, Is.EqualTo(-240f));
            Assert.That(layerSettings.ActorHeightOffset, Is.EqualTo(0f));
            Assert.That(cameraSettings.InitialPosition, Is.EqualTo(new Vector3(64f, 80f, -64f)));
            Assert.That(cameraSettings.InitialPitchDegrees, Is.EqualTo(45f));
            Assert.That(cameraSettings.InitialYawDegrees, Is.EqualTo(45f));
            Assert.That(cameraSettings.InitialOrthographicSize, Is.EqualTo(24f));
            Assert.That(cameraSettings.MoveSpeed, Is.EqualTo(32f));
            Assert.That(cameraSettings.RotationSensitivity, Is.EqualTo(0.2f));
            Assert.That(cameraSettings.ZoomSensitivity, Is.EqualTo(0.02f));
            Assert.That(cameraSettings.MinOrthographicSize, Is.EqualTo(12f));
            Assert.That(cameraSettings.MaxOrthographicSize, Is.EqualTo(120f));
            Assert.That(cameraSettings.ActorViewportMargin, Is.EqualTo(0.08f));
            Assert.That(cameraSettings.ActorSelectionZoomRatio, Is.EqualTo(0.2f));
            Assert.That(groundMapSettings.Width, Is.EqualTo(30));
            Assert.That(dungeonMapSettings.FloorWidth, Is.EqualTo(50));
            Assert.That(initialWorldSettings.DungeonSeed, Is.EqualTo(12345));
            Assert.That(innBalanceSettings.FeePerStay, Is.EqualTo(10));
            Assert.That(actorSimulationSettings.MoveSpeedMetersPerSecond, Is.EqualTo(5f));
            using (var serializedGameSettings = new SerializedObject(gameSettingsSo))
            {
                Assert.That(serializedGameSettings.FindProperty("actorMoveArrivalDistanceMeters"), Is.Null);
            }

            Assert.That(spawnBalanceSettings.MaxAdventurerCount, Is.EqualTo(30));
            Assert.That(returnPolicySettings.GoalCompletedScore, Is.EqualTo(100));
            Assert.That(combatBalanceSettings.ProjectileHitRadiusMeters, Is.EqualTo(0.5f));
            Assert.That(worldMapViewSettings.ChunkTileSize, Is.EqualTo(16));
        }

        [Test]
        public void WorldCameraControllerInitializesCameraFromSettings()
        {
            var cameraObject = new GameObject("WorldCameraControllerInitializesCameraFromSettings");
            var camera = cameraObject.AddComponent<Camera>();
            var settings = new WorldCameraSettings(
                new Vector3(10f, 20f, -30f),
                initialPitchDegrees: 33f,
                initialYawDegrees: 77f,
                initialOrthographicSize: 22f,
                moveSpeed: 1f,
                rotationSensitivity: 1f,
                zoomSensitivity: 1f,
                minOrthographicSize: 5f,
                maxOrthographicSize: 40f,
                actorViewportMargin: 0.25f,
                actorSelectionZoomRatio: 0.2f);
            var controller = new WorldCameraController(new FixedWorldCameraSettingsRepository(settings));

            try
            {
                camera.transform.SetPositionAndRotation(
                    Vector3.zero,
                    Quaternion.Euler(1f, 2f, 3f));
                camera.orthographicSize = 9f;

                controller.BindCamera(camera);
                controller.UpdateCamera(0f);

                Assert.That(camera.transform.position, Is.EqualTo(new Vector3(10f, 20f, -30f)));
                Assert.That(camera.transform.eulerAngles.x, Is.EqualTo(33f).Within(0.0001f));
                Assert.That(camera.transform.eulerAngles.y, Is.EqualTo(77f).Within(0.0001f));
                Assert.That(camera.transform.eulerAngles.z, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(camera.orthographicSize, Is.EqualTo(22f));
                Assert.That(controller.CurrentYawDegrees, Is.EqualTo(77f));
                Assert.That(
                    Quaternion.Angle(controller.CurrentCameraRotation, Quaternion.Euler(33f, 77f, 0f)),
                    Is.LessThan(0.0001f));
                Assert.That(controller.ActorViewportMargin, Is.EqualTo(0.25f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void WorldCameraControllerCalculatesWorldUnitsPerPixelFromOrthographicCamera()
        {
            var cameraObject = new GameObject("WorldCameraControllerWorldUnitsPerPixel");
            var camera = cameraObject.AddComponent<Camera>();
            var settings = new WorldCameraSettings(
                Vector3.zero,
                initialPitchDegrees: 45f,
                initialYawDegrees: 45f,
                initialOrthographicSize: 20f,
                moveSpeed: 1f,
                rotationSensitivity: 1f,
                zoomSensitivity: 1f,
                minOrthographicSize: 1f,
                maxOrthographicSize: 40f,
                actorViewportMargin: 0.25f,
                actorSelectionZoomRatio: 0.2f);
            var controller = new WorldCameraController(new FixedWorldCameraSettingsRepository(settings));

            try
            {
                camera.pixelRect = new Rect(0f, 0f, 800f, 400f);
                controller.BindCamera(camera);
                controller.UpdateCamera(0f);

                Assert.That(controller.WorldUnitsPerPixel, Is.EqualTo(0.1f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void WorldCameraControllerDetectsViewportVisibilityWithMargin()
        {
            var cameraObject = new GameObject("WorldCameraControllerDetectsViewportVisibilityWithMargin");
            var camera = cameraObject.AddComponent<Camera>();
            var settings = new WorldCameraSettings(
                Vector3.zero,
                initialPitchDegrees: 0f,
                initialYawDegrees: 0f,
                initialOrthographicSize: 5f,
                moveSpeed: 1f,
                rotationSensitivity: 1f,
                zoomSensitivity: 1f,
                minOrthographicSize: 1f,
                maxOrthographicSize: 10f,
                actorViewportMargin: 0.1f,
                actorSelectionZoomRatio: 0.2f);
            var controller = new WorldCameraController(new FixedWorldCameraSettingsRepository(settings));

            try
            {
                camera.orthographic = true;
                controller.BindCamera(camera);
                controller.UpdateCamera(0f);

                Assert.That(controller.IsWorldPositionVisible(new Vector3(0f, 0f, 5f), 0f), Is.True);
                Assert.That(controller.IsWorldPositionVisible(new Vector3(100f, 0f, 5f), 0f), Is.False);
                Assert.That(controller.IsWorldPositionVisible(new Vector3(0f, 0f, -5f), 0f), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void WorldCameraControllerKeepsGroundFocusWhenRotating()
        {
            var cameraObject = new GameObject("WorldCameraControllerKeepsGroundFocusWhenRotating");
            var camera = cameraObject.AddComponent<Camera>();
            var settings = new WorldCameraSettings(
                new Vector3(0f, 10f, -10f),
                initialPitchDegrees: 45f,
                initialYawDegrees: 0f,
                initialOrthographicSize: 10f,
                moveSpeed: 1f,
                rotationSensitivity: 1f,
                zoomSensitivity: 1f,
                minOrthographicSize: 1f,
                maxOrthographicSize: 20f,
                actorViewportMargin: 0.1f,
                actorSelectionZoomRatio: 0.2f);
            var controller = new WorldCameraController(new FixedWorldCameraSettingsRepository(settings));

            try
            {
                controller.BindCamera(camera);
                controller.UpdateCamera(0f);
                var beforeFocus = ResolveGroundCenterFocus(camera);

                controller.SetRotating(true);
                controller.AddLookDelta(new Vector2(90f, 0f));
                controller.UpdateCamera(1f / 60f);

                var afterFocus = ResolveGroundCenterFocus(camera);
                Assert.That(afterFocus.x, Is.EqualTo(beforeFocus.x).Within(0.0001f));
                Assert.That(afterFocus.z, Is.EqualTo(beforeFocus.z).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void WorldCameraControllerFollowsActorByMovingGroundFocus()
        {
            var cameraObject = new GameObject("WorldCameraControllerFollowsActorByMovingGroundFocus");
            var camera = cameraObject.AddComponent<Camera>();
            var settings = new WorldCameraSettings(
                new Vector3(0f, 10f, -10f),
                initialPitchDegrees: 45f,
                initialYawDegrees: 0f,
                initialOrthographicSize: 10f,
                moveSpeed: 1f,
                rotationSensitivity: 1f,
                zoomSensitivity: 1f,
                minOrthographicSize: 1f,
                maxOrthographicSize: 20f,
                actorViewportMargin: 0.1f,
                actorSelectionZoomRatio: 0.2f);
            var controller = new WorldCameraController(new FixedWorldCameraSettingsRepository(settings));

            try
            {
                var actorPosition = new Vector3(5f, 0f, 5f);

                controller.BindCamera(camera);
                controller.UpdateCamera(0f);
                controller.BeginFollow(0.5f);
                controller.UpdateFollowPosition(actorPosition);
                controller.UpdateCamera(1f);

                var focus = ResolveGroundCenterFocus(camera);
                Assert.That(focus.x, Is.EqualTo(actorPosition.x).Within(0.0001f));
                Assert.That(focus.z, Is.EqualTo(actorPosition.z).Within(0.0001f));
                Assert.That(Vector3.Distance(camera.transform.position, actorPosition), Is.GreaterThan(5f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ActorViewScalesSpriteCanvasToVisualHeight()
        {
            var gameObject = new GameObject("ActorViewScaleTest");
            var actorView = gameObject.AddComponent<ActorView>();
            var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 512f, 512f),
                new Vector2(0.5f, 0f),
                16f);

            try
            {
                actorView.SetVisualCanvasHeight(
                    ActorVisualSizeTierCatalog.GetCanvasHeightMeters(ActorVisualSizeTier.AdventurerS));
                actorView.SetSprite(sprite);

                Assert.That(actorView.transform.localScale.y, Is.EqualTo(1.5f / 32f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ActorVisualSizeTierGroundAnchorOffsetUsesHalfCanvasHeight()
        {
            Assert.That(
                ActorVisualSizeTierCatalog.GetGroundAnchorOffsetMeters(ActorVisualSizeTier.AdventurerS),
                Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void ActorVisualDefinitionConvertsEntriesToRuntimeClips()
        {
            var definitionSo = ScriptableObject.CreateInstance<ActorVisualDefinitionSO>();
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0f),
                16f);

            try
            {
                using var serialized = new SerializedObject(definitionSo);
                serialized.FindProperty("visualId").stringValue = "test_visual";
                serialized.FindProperty("visualSizeTier").enumValueIndex = (int)ActorVisualSizeTier.MonsterS;
                foreach (ActorAnimationKey animationKey in Enum.GetValues(typeof(ActorAnimationKey)))
                {
                    var entries = serialized.FindProperty(GetActorVisualEntriesFieldName(animationKey));
                    entries.arraySize = Enum.GetValues(typeof(ActorAnimationDirection)).Length;
                    var entryIndex = 0;
                    foreach (ActorAnimationDirection direction in Enum.GetValues(typeof(ActorAnimationDirection)))
                    {
                        var entry = entries.GetArrayElementAtIndex(entryIndex);
                        var isTarget = animationKey == ActorAnimationKey.Attack &&
                            direction == ActorAnimationDirection.NW;
                        entry.FindPropertyRelative("direction").enumValueIndex = (int)direction;
                        entry.FindPropertyRelative("fps").floatValue = isTarget ? 12f : 1f;
                        entry.FindPropertyRelative("loop").boolValue = !isTarget;
                        var sprites = entry.FindPropertyRelative("sprites");
                        sprites.arraySize = 1;
                        sprites.GetArrayElementAtIndex(0).objectReferenceValue = sprite;
                        entryIndex++;
                    }
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var runtimeDefinition = definitionSo.ToRuntimeDefinition();
                var found = runtimeDefinition.TryGetClip(
                    ActorAnimationKey.Attack,
                    ActorAnimationDirection.NW,
                    out var clip);

                Assert.That(runtimeDefinition.VisualId, Is.EqualTo("test_visual"));
                Assert.That(runtimeDefinition.VisualSizeTier, Is.EqualTo(ActorVisualSizeTier.MonsterS));
                Assert.That(found, Is.True);
                Assert.That(clip.Fps, Is.EqualTo(12f));
                Assert.That(clip.Loop, Is.False);
                Assert.That(clip.GetSprite(0), Is.EqualTo(sprite));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(definitionSo);
            }
        }

        [Test]
        public void ActorVisualDefinitionRejectsDuplicateAnimationEntries()
        {
            var definitionSo = ScriptableObject.CreateInstance<ActorVisualDefinitionSO>();

            try
            {
                using var serialized = new SerializedObject(definitionSo);
                serialized.FindProperty("visualId").stringValue = "duplicate_visual";
                var entries = serialized.FindProperty("idleEntries");
                entries.arraySize = 2;
                for (var index = 0; index < entries.arraySize; index++)
                {
                    var entry = entries.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("direction").enumValueIndex = (int)ActorAnimationDirection.SE;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.Throws<InvalidOperationException>(() => definitionSo.ToRuntimeDefinition());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definitionSo);
            }
        }

        static string GetActorVisualEntriesFieldName(ActorAnimationKey animationKey)
        {
            switch (animationKey)
            {
                case ActorAnimationKey.Idle:
                    return "idleEntries";
                case ActorAnimationKey.Walk:
                    return "walkEntries";
                case ActorAnimationKey.Work:
                    return "workEntries";
                case ActorAnimationKey.Attack:
                    return "attackEntries";
                case ActorAnimationKey.Damage:
                    return "damageEntries";
                case ActorAnimationKey.Dead:
                    return "deadEntries";
                default:
                    throw new ArgumentOutOfRangeException(nameof(animationKey), animationKey, null);
            }
        }

        [Test]
        public void ActorVisualDefinitionsContainEveryAnimationKeyAndDirection()
        {
            foreach (var path in new[]
            {
                "Assets/DungeonInn/Runtime/StaticResources/Visual/ActorVisualDefinitions/AdventurerNovice.asset",
                "Assets/DungeonInn/Runtime/StaticResources/Visual/ActorVisualDefinitions/MonsterGoblin.asset",
                "Assets/DungeonInn/Runtime/StaticResources/Visual/ActorVisualDefinitions/MonsterOrc.asset",
                "Assets/DungeonInn/Runtime/StaticResources/Visual/ActorVisualDefinitions/MonsterOgre.asset",
                "Assets/DungeonInn/Runtime/StaticResources/Visual/ActorVisualDefinitions/MonsterGoblinArcher.asset"
            })
            {
                var definition = AssetDatabase.LoadAssetAtPath<ActorVisualDefinitionSO>(path);
                Assert.That(definition, Is.Not.Null, path);
                Assert.That(definition.VisualId, Is.Not.Empty, path);
                Assert.That(
                    ActorVisualSizeTierCatalog.GetCanvasHeightMeters(definition.VisualSizeTier),
                    Is.GreaterThan(0f),
                    path);

                var runtimeDefinition = definition.ToRuntimeDefinition();
                foreach (ActorAnimationKey animationKey in Enum.GetValues(typeof(ActorAnimationKey)))
                {
                    foreach (ActorAnimationDirection direction in Enum.GetValues(typeof(ActorAnimationDirection)))
                    {
                        Assert.That(
                            runtimeDefinition.TryGetClip(animationKey, direction, out var clip),
                            Is.True,
                            $"{path} {animationKey} {direction}");
                        Assert.That(clip.FrameCount, Is.GreaterThan(0), $"{path} {animationKey} {direction}");
                    }
                }
            }
        }

        [Test]
        public void ActorViewPrefabDoesNotReferenceActorSpriteAnimationClips()
        {
            var actorView = AssetDatabase.LoadAssetAtPath<ActorView>(ActorViewPrefabPath);
            Assert.That(actorView, Is.Not.Null);

            using var serialized = new SerializedObject(actorView);
            Assert.That(serialized.FindProperty("idleAnimationClip"), Is.Null);
            Assert.That(serialized.FindProperty("walkAnimationClip"), Is.Null);
            Assert.That(serialized.FindProperty("combatAnimationClip"), Is.Null);
            Assert.That(serialized.FindProperty("hitAnimationClip"), Is.Null);
            Assert.That(serialized.FindProperty("deadAnimationClip"), Is.Null);
        }

        [Test]
        public void ActorViewVisibilityTogglesSpriteRendererWithoutChangingGameObjectActiveState()
        {
            var gameObject = new GameObject("ActorViewVisibilityTest");
            var actorView = gameObject.AddComponent<ActorView>();

            try
            {
                var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

                actorView.SetVisible(false);

                Assert.That(spriteRenderer.enabled, Is.False);
                Assert.That(gameObject.activeSelf, Is.True);

                actorView.SetVisible(true);

                Assert.That(spriteRenderer.enabled, Is.True);
                Assert.That(gameObject.activeSelf, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ActorViewAppliesFullBillboardRotation()
        {
            var gameObject = new GameObject("ActorViewBillboardRotationTest");
            var actorView = gameObject.AddComponent<ActorView>();
            var rotation = Quaternion.Euler(45f, 77f, 0f);

            try
            {
                actorView.SetBillboardRotation(rotation);

                Assert.That(Quaternion.Angle(actorView.transform.rotation, rotation), Is.LessThan(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ActorViewKeepsScaleStableWhenSameVisualValuesAreAppliedRepeatedly()
        {
            var gameObject = new GameObject("ActorViewRepeatedVisualUpdateTest");
            var actorView = gameObject.AddComponent<ActorView>();
            var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 512f, 512f),
                new Vector2(0.5f, 0f),
                16f);

            try
            {
                actorView.SetSprite(sprite);
                actorView.SetVisualCanvasHeight(1.5f);
                var firstScale = actorView.transform.localScale;

                actorView.SetSprite(sprite);
                actorView.SetVisualCanvasHeight(1.5f);

                Assert.That(actorView.transform.localScale, Is.EqualTo(firstScale));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MapMaterialSetLogsWarningWhenFallbackIsUsed()
        {
            var loader = new VisualConfigLoader(
                new ThrowingAssetManager(),
                new VisualConfigSettings(null, null));
            var materialSet = new MapMaterialSet(loader);

            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[MapMaterialSet] Using fallback map material. Kind=GroundWalkable");

                var material = materialSet.Get(TileVisualKind.GroundWalkable);

                Assert.That(material, Is.Not.Null);
            }
            finally
            {
                materialSet.Dispose();
                loader.Dispose();
            }
        }

        [TestCase(TileVisualKind.GroundWalkable, 4, 6)]
        [TestCase(TileVisualKind.GroundBlocked, 20, 30)]
        [TestCase(TileVisualKind.StairUp, 4, 6)]
        public void MapMeshBuildServiceBuildsExpectedTileGeometry(
            TileVisualKind visualKind,
            int expectedVertexCount,
            int expectedTriangleIndexCount)
        {
            var loader = new VisualConfigLoader(
                new ThrowingAssetManager(),
                new VisualConfigSettings(null, null));
            var materialSet = new MapMaterialSet(loader);
            var tileConfig = new MapTileVisualConfig(materialSet);
            var service = new MapMeshBuildService(tileConfig, materialSet, new FixedWorldGameSettingsRepository());

            LogAssert.Expect(
                LogType.Warning,
                $"[MapMaterialSet] Using fallback map material. Kind={visualKind}");

            var chunkMesh = service.BuildChunk(
                MapLayerId.Ground,
                0,
                0,
                1,
                1,
                _ => visualKind);

            try
            {
                Assert.That(chunkMesh.Mesh.vertexCount, Is.EqualTo(expectedVertexCount));
                Assert.That(chunkMesh.Mesh.triangles.Length, Is.EqualTo(expectedTriangleIndexCount));
                Assert.That(chunkMesh.Materials.Length, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(chunkMesh.Mesh);
                materialSet.Dispose();
                loader.Dispose();
            }
        }

        [Test]
        public void MapMeshBuildServiceUsesSeparateTileWidthAndHeightConstants()
        {
            var loader = new VisualConfigLoader(
                new ThrowingAssetManager(),
                new VisualConfigSettings(null, null));
            var materialSet = new MapMaterialSet(loader);
            var tileConfig = new MapTileVisualConfig(materialSet);
            var service = new MapMeshBuildService(tileConfig, materialSet, new FixedWorldGameSettingsRepository());

            LogAssert.Expect(
                LogType.Warning,
                "[MapMaterialSet] Using fallback map material. Kind=GroundBlocked");

            var chunkMesh = service.BuildChunk(
                MapLayerId.Ground,
                0,
                0,
                1,
                1,
                _ => TileVisualKind.GroundBlocked);

            try
            {
                Assert.That(chunkMesh.Mesh.bounds.size.x, Is.EqualTo(GameConstants.MapCellWidthMeters).Within(0.0001f));
                Assert.That(chunkMesh.Mesh.bounds.size.y, Is.EqualTo(WorldMapViewSettings.CreateDefault().TileHeightMeters).Within(0.0001f));
                Assert.That(chunkMesh.Mesh.bounds.size.z, Is.EqualTo(GameConstants.MapCellWidthMeters).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(chunkMesh.Mesh);
                materialSet.Dispose();
                loader.Dispose();
            }
        }

        static Vector3 ResolveGroundCenterFocus(Camera camera)
        {
            var ray = new Ray(camera.transform.position, camera.transform.forward);
            var distance = -ray.origin.y / ray.direction.y;
            return ray.origin + ray.direction * distance;
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

