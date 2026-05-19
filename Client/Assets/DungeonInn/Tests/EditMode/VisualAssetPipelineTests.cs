using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
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
        const string ActorSpriteVisualConfigPath = "Assets/DungeonInn/Runtime/StaticResources/Visual/ActorSpriteVisualConfig.asset";
        const string LayerPositionViewSettingsPath = "Assets/DungeonInn/Runtime/StaticResources/Visual/LayerPositionViewSettings.asset";
        const string WorldCameraSettingsPath = "Assets/DungeonInn/Runtime/StaticResources/Visual/WorldCameraSettings.asset";
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
        }

        [Test]
        public void WorldViewSettingsAssetsExistAndConvertToRuntimeSettings()
        {
            var layerSettingsSo = AssetDatabase.LoadAssetAtPath<LayerPositionViewSettingsSO>(LayerPositionViewSettingsPath);
            var cameraSettingsSo = AssetDatabase.LoadAssetAtPath<WorldCameraSettingsSO>(WorldCameraSettingsPath);

            Assert.That(layerSettingsSo, Is.Not.Null);
            Assert.That(cameraSettingsSo, Is.Not.Null);

            var layerSettings = layerSettingsSo.ToSettings();
            var cameraSettings = cameraSettingsSo.ToSettings();

            Assert.That(layerSettings.LayerHeightOffset, Is.EqualTo(-240f));
            Assert.That(layerSettings.ActorHeightOffset, Is.EqualTo(0f));
            Assert.That(cameraSettings.InitialPosition, Is.EqualTo(new Vector3(64f, 80f, -64f)));
            Assert.That(cameraSettings.InitialPitchDegrees, Is.EqualTo(45f));
            Assert.That(cameraSettings.InitialYawDegrees, Is.EqualTo(45f));
            Assert.That(cameraSettings.InitialOrthographicSize, Is.EqualTo(48f));
            Assert.That(cameraSettings.MoveSpeed, Is.EqualTo(32f));
            Assert.That(cameraSettings.RotationSensitivity, Is.EqualTo(0.2f));
            Assert.That(cameraSettings.ZoomSensitivity, Is.EqualTo(0.02f));
            Assert.That(cameraSettings.MinOrthographicSize, Is.EqualTo(12f));
            Assert.That(cameraSettings.MaxOrthographicSize, Is.EqualTo(120f));
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
                maxOrthographicSize: 40f);
            var controller = new WorldCameraController(settings);

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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ActorSpriteVisualConfigContainsVisualSizeTierForEveryEntry()
        {
            var config = AssetDatabase.LoadAssetAtPath<ActorSpriteVisualConfigSO>(ActorSpriteVisualConfigPath);
            Assert.That(config, Is.Not.Null);

            using var serialized = new SerializedObject(config);
            var entries = serialized.FindProperty("entries");
            Assert.That(entries.arraySize, Is.GreaterThan(0));

            for (var index = 0; index < entries.arraySize; index++)
            {
                var entry = entries.GetArrayElementAtIndex(index);
                var visualSizeTier = (ActorVisualSizeTier)entry
                    .FindPropertyRelative("VisualSizeTier")
                    .enumValueIndex;

                Assert.That(
                    ActorVisualSizeTierCatalog.GetCanvasHeightMeters(visualSizeTier),
                    Is.GreaterThan(0f));
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
        public void ActorSpriteVisualConfigLogsWarningWhenPlaceholderIsUsed()
        {
            var loader = new VisualConfigLoader(
                new ThrowingAssetManager(),
                new VisualConfigSettings(null, null, null));
            var config = new ActorSpriteVisualConfig(loader);

            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[ActorSpriteVisualConfig] Using placeholder actor sprite. BehaviorType=GuildStaff");

                var sprite = config.GetSprite(
                    ActorBehaviorType.GuildStaff,
                    ActorAnimationDirection.NE,
                    isWalking: false,
                    walkFrameIndex: 0);

                Assert.That(sprite, Is.Not.Null);
            }
            finally
            {
                config.Dispose();
                loader.Dispose();
            }
        }

        [Test]
        public void MapMaterialSetLogsWarningWhenFallbackIsUsed()
        {
            var loader = new VisualConfigLoader(
                new ThrowingAssetManager(),
                new VisualConfigSettings(null, null, null));
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
                new VisualConfigSettings(null, null, null));
            var materialSet = new MapMaterialSet(loader);
            var tileConfig = new MapTileVisualConfig(materialSet);
            var service = new MapMeshBuildService(tileConfig, materialSet);

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
