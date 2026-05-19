using System.IO;
using DungeonInn.Domain.Actor;
using DungeonInn.View.Scene.MainScene.World;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace DungeonInn.Editor
{
    public static class VisualAssetSetup
    {
        const string SoDirectory = "Assets/DungeonInn/Runtime/StaticResources/Visual";
        const string MapMaterialDirectory = "Assets/DungeonInn/Runtime/Art/Materials/Map";
        const string MapTextureDirectory = "Assets/DungeonInn/Runtime/Art/Textures/Map";
        const string MapMaterialSoPath = SoDirectory + "/MapMaterialSet.asset";
        const string ActorSpriteSoPath = SoDirectory + "/ActorSpriteVisualConfig.asset";
        const string LayerPositionViewSettingsPath = SoDirectory + "/LayerPositionViewSettings.asset";
        const string WorldCameraSettingsPath = SoDirectory + "/WorldCameraSettings.asset";
        const string ActorPrefabPath = SoDirectory + "/ActorView.prefab";
        const string ActorIdleAnimationPath = SoDirectory + "/ActorIdleAnimation.asset";
        const string ActorWalkAnimationPath = SoDirectory + "/ActorWalkAnimation.asset";
        const string AddressablesGroupName = "DungeonInn Visual";
        const string MapMaterialShaderName = "Universal Render Pipeline/Lit";

        [InitializeOnLoadMethod]
        static void AutoSetup()
        {
            var assetsExist = AssetDatabase.AssetPathExists(MapMaterialSoPath) &&
                AssetDatabase.AssetPathExists(ActorSpriteSoPath) &&
                AssetDatabase.AssetPathExists(LayerPositionViewSettingsPath) &&
                AssetDatabase.AssetPathExists(WorldCameraSettingsPath) &&
                AssetDatabase.AssetPathExists(ActorPrefabPath) &&
                AssetDatabase.AssetPathExists(ActorIdleAnimationPath) &&
                AssetDatabase.AssetPathExists(ActorWalkAnimationPath);

            if (!assetsExist)
            {
                RunSetup();
            }
        }

        static void SetupAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogWarning("[DungeonInn] Could not get Addressables settings.");
                return;
            }

            EnsureAddressablesGroup(settings);

            AssetDatabase.SaveAssets();
            Debug.Log("[DungeonInn] Addressables group setup complete.");
        }

        [MenuItem("DungeonInn/Setup Visual Assets")]
        public static void RunSetup()
        {
            EnsureDirectory(SoDirectory);

            var idleClip = CreateOrLoadActorAnimationClip(ActorIdleAnimationPath, 1f, new[] { 0 });
            var walkClip = CreateOrLoadActorAnimationClip(ActorWalkAnimationPath, 4f, new[] { 0, 1 });
            var actorPrefab = CreateOrLoadActorViewPrefab();
            AssignAnimationClipsToPrefab(actorPrefab, idleClip, walkClip);
            var mapSo = CreateOrLoadMapMaterialSO();
            var actorSo = CreateOrLoadActorSpriteSO(actorPrefab);
            var layerSettingsSo = CreateOrLoadLayerPositionViewSettingsSO();
            var cameraSettingsSo = CreateOrLoadWorldCameraSettingsSO();

            AssignToWorldLifetimeScope(mapSo, actorSo, layerSettingsSo, cameraSettingsSo);
            SetupAddressables();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DungeonInn] Visual assets (SO + Prefab) created.");
        }

        [MenuItem("DungeonInn/Setup Visual Addressables")]
        public static void RunAddressablesSetup()
        {
            SetupAddressables();
        }

        static LayerPositionViewSettingsSO CreateOrLoadLayerPositionViewSettingsSO()
        {
            if (AssetDatabase.AssetPathExists(LayerPositionViewSettingsPath))
            {
                return AssetDatabase.LoadAssetAtPath<LayerPositionViewSettingsSO>(LayerPositionViewSettingsPath);
            }

            var settings = ScriptableObject.CreateInstance<LayerPositionViewSettingsSO>();
            AssetDatabase.CreateAsset(settings, LayerPositionViewSettingsPath);
            Debug.Log($"[DungeonInn] Created {LayerPositionViewSettingsPath}");
            return settings;
        }

        static WorldCameraSettingsSO CreateOrLoadWorldCameraSettingsSO()
        {
            if (AssetDatabase.AssetPathExists(WorldCameraSettingsPath))
            {
                return AssetDatabase.LoadAssetAtPath<WorldCameraSettingsSO>(WorldCameraSettingsPath);
            }

            var settings = ScriptableObject.CreateInstance<WorldCameraSettingsSO>();
            AssetDatabase.CreateAsset(settings, WorldCameraSettingsPath);
            Debug.Log($"[DungeonInn] Created {WorldCameraSettingsPath}");
            return settings;
        }

        static ActorView CreateOrLoadActorViewPrefab()
        {
            if (AssetDatabase.AssetPathExists(ActorPrefabPath))
            {
                return AssetDatabase.LoadAssetAtPath<ActorView>(ActorPrefabPath);
            }

            var go = new GameObject("ActorView");
            go.AddComponent<SpriteRenderer>();
            var actorView = go.AddComponent<ActorView>();
            go.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ActorPrefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[DungeonInn] Created {ActorPrefabPath}");
            return prefab.GetComponent<ActorView>();
        }

        static ActorSpriteAnimationClip CreateOrLoadActorAnimationClip(
            string assetPath,
            float fps,
            int[] frameIndices)
        {
            if (AssetDatabase.AssetPathExists(assetPath))
            {
                return AssetDatabase.LoadAssetAtPath<ActorSpriteAnimationClip>(assetPath);
            }

            var clip = ScriptableObject.CreateInstance<ActorSpriteAnimationClip>();
            using (var serializedClip = new SerializedObject(clip))
            {
                serializedClip.FindProperty("fps").floatValue = fps;
                serializedClip.FindProperty("loop").boolValue = true;
                var frameIndicesProp = serializedClip.FindProperty("frameIndices");
                frameIndicesProp.ClearArray();
                for (var index = 0; index < frameIndices.Length; index++)
                {
                    frameIndicesProp.InsertArrayElementAtIndex(index);
                    frameIndicesProp.GetArrayElementAtIndex(index).intValue = frameIndices[index];
                }

                serializedClip.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.CreateAsset(clip, assetPath);
            Debug.Log($"[DungeonInn] Created {assetPath}");
            return clip;
        }

        static void AssignAnimationClipsToPrefab(
            ActorView actorPrefab,
            ActorSpriteAnimationClip idleClip,
            ActorSpriteAnimationClip walkClip)
        {
            if (actorPrefab == null)
            {
                return;
            }

            using var serializedPrefab = new SerializedObject(actorPrefab);
            var idleClipProp = serializedPrefab.FindProperty("idleAnimationClip");
            var walkClipProp = serializedPrefab.FindProperty("walkAnimationClip");
            var changed = false;

            if (idleClipProp != null && idleClipProp.objectReferenceValue == null)
            {
                idleClipProp.objectReferenceValue = idleClip;
                changed = true;
            }

            if (walkClipProp != null && walkClipProp.objectReferenceValue == null)
            {
                walkClipProp.objectReferenceValue = walkClip;
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            serializedPrefab.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(actorPrefab);
        }

        static MapMaterialSetSO CreateOrLoadMapMaterialSO()
        {
            if (AssetDatabase.AssetPathExists(MapMaterialSoPath))
            {
                return AssetDatabase.LoadAssetAtPath<MapMaterialSetSO>(MapMaterialSoPath);
            }

            var so = ScriptableObject.CreateInstance<MapMaterialSetSO>();

            // Empty Material addresses — fallback colors are in MapMaterialSet.cs
            // Addresses will be filled when Map Material assets are created in Phase 4.

            AssetDatabase.CreateAsset(so, MapMaterialSoPath);
            Debug.Log($"[DungeonInn] Created {MapMaterialSoPath}");
            return so;
        }

        static ActorSpriteVisualConfigSO CreateOrLoadActorSpriteSO(ActorView actorPrefab)
        {
            if (AssetDatabase.AssetPathExists(ActorSpriteSoPath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<ActorSpriteVisualConfigSO>(ActorSpriteSoPath);
                AssignPrefabToSO(existing, actorPrefab);
                using var serializedExisting = new SerializedObject(existing);
                AssignActorSpriteEntries(serializedExisting);
                serializedExisting.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var so = ScriptableObject.CreateInstance<ActorSpriteVisualConfigSO>();

            using (var serializedSo = new SerializedObject(so))
            {
                AssignActorSpriteEntries(serializedSo);
                serializedSo.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.CreateAsset(so, ActorSpriteSoPath);

            AssignPrefabToSO(so, actorPrefab);

            Debug.Log($"[DungeonInn] Created {ActorSpriteSoPath}");
            return so;
        }

        static void AssignPrefabToSO(ActorSpriteVisualConfigSO so, ActorView actorPrefab)
        {
            if (actorPrefab == null)
            {
                return;
            }

            using var serializedSo = new SerializedObject(so);
            var prefabProp = serializedSo.FindProperty("actorPrefab");
            if (prefabProp != null && prefabProp.objectReferenceValue == null)
            {
                prefabProp.objectReferenceValue = actorPrefab;
                serializedSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(so);
            }
        }

        static void AssignActorSpriteEntries(SerializedObject serializedSo)
        {
            var entriesProp = serializedSo.FindProperty("entries");
            entriesProp.ClearArray();

            AddEntry(entriesProp, ActorBehaviorType.Adventurer,
                "Sprites/Adventurer",
                new Color(0.1f, 0.45f, 1f, 1f),
                ActorVisualSizeTier.AdventurerS);

            AddEntry(entriesProp, ActorBehaviorType.Monster,
                "Sprites/Goblin",
                new Color(0.9f, 0.15f, 0.1f, 1f),
                ActorVisualSizeTier.MonsterS);

            AddEntry(entriesProp, ActorBehaviorType.GuildStaff,
                "",
                new Color(0.1f, 0.8f, 0.4f, 1f),
                ActorVisualSizeTier.AdventurerS);

            AddEntry(entriesProp, ActorBehaviorType.Pet,
                "",
                new Color(0.8f, 0.5f, 0.1f, 1f),
                ActorVisualSizeTier.MonsterS);

            AddEntry(entriesProp, ActorBehaviorType.None,
                "",
                new Color(1f, 0.85f, 0.1f, 1f),
                ActorVisualSizeTier.AdventurerS);
        }

        static void AddEntry(
            SerializedProperty entriesProp,
            ActorBehaviorType behaviorType,
            string address,
            Color fallbackColor,
            ActorVisualSizeTier visualSizeTier)
        {
            var index = entriesProp.arraySize;
            entriesProp.InsertArrayElementAtIndex(index);
            var element = entriesProp.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("BehaviorType").enumValueIndex = (int)behaviorType;
            element.FindPropertyRelative("AddressPrefix").stringValue = address;
            element.FindPropertyRelative("FallbackColor").colorValue = fallbackColor;
            element.FindPropertyRelative("VisualSizeTier").enumValueIndex = (int)visualSizeTier;
        }

        static void EnsureAddressablesGroup(AddressableAssetSettings settings)
        {
            var group = settings.FindGroup(AddressablesGroupName)
                ?? settings.CreateGroup(
                    AddressablesGroupName,
                    false,
                    false,
                    false,
                    null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));

            EnsureGroupSchemas(settings, group);

            RegisterAnimationSet(
                settings,
                group,
                "Assets/DungeonInn/Runtime/Art/Sprites/Adventurer",
                "Adventurer",
                "Sprites/Adventurer");

            RegisterAnimationSet(
                settings,
                group,
                "Assets/DungeonInn/Runtime/Art/Sprites/Goblin",
                "Goblin",
                "Sprites/Goblin");

            RegisterMapMaterials(settings, group);

            EditorUtility.SetDirty(settings);
        }

        static void EnsureGroupSchemas(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundledSchema == null)
            {
                bundledSchema = group.AddSchema<BundledAssetGroupSchema>();
            }

            var contentUpdateSchema = group.GetSchema<ContentUpdateGroupSchema>();
            if (contentUpdateSchema == null)
            {
                contentUpdateSchema = group.AddSchema<ContentUpdateGroupSchema>();
            }

            bundledSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundledSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            contentUpdateSchema.StaticContent = false;
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(bundledSchema);
            EditorUtility.SetDirty(contentUpdateSchema);
        }

        static void RegisterMapMaterials(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            EnsureDirectory(MapMaterialDirectory);

            var tileKindEntries = new[]
            {
                (TileVisualKind.GroundWalkable, "GroundWalkable"),
                (TileVisualKind.GroundBlocked, "GroundBlocked"),
                (TileVisualKind.DungeonWalkable, "DungeonWalkable"),
                (TileVisualKind.DungeonBlocked, "DungeonBlocked"),
                (TileVisualKind.StairUp, "StairUp"),
                (TileVisualKind.StairDown, "StairDown"),
                (TileVisualKind.Facility, "Facility")
            };

            var mapSo = AssetDatabase.LoadAssetAtPath<MapMaterialSetSO>(MapMaterialSoPath);
            if (mapSo == null)
            {
                Debug.LogWarning("[DungeonInn] MapMaterialSet.asset not found. Run Setup Visual Assets first.");
                return;
            }

            using var serializedSo = new SerializedObject(mapSo);
            var entriesProp = serializedSo.FindProperty("entries");
            entriesProp.ClearArray();

            foreach (var (kind, name) in tileKindEntries)
            {
                var texturePath = $"{MapTextureDirectory}/{name}.png";
                var materialPath = $"{MapMaterialDirectory}/{name}.mat";
                var address = $"Materials/Map/{name}";

                EnsureMapMaterial(texturePath, materialPath);
                MarkMaterialAddressable(settings, group, materialPath, address);

                var index = entriesProp.arraySize;
                entriesProp.InsertArrayElementAtIndex(index);
                var element = entriesProp.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("Kind").enumValueIndex = (int)kind;
                element.FindPropertyRelative("MaterialAddress").stringValue = address;
                element.FindPropertyRelative("FallbackColor").colorValue = Color.white;
            }

            serializedSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mapSo);
        }

        static void EnsureMapMaterial(string texturePath, string materialPath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogWarning($"[DungeonInn] Texture not found: {texturePath}");
                return;
            }

            var shader = Shader.Find(MapMaterialShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[DungeonInn] Shader not found: {MapMaterialShaderName}");
                return;
            }

            if (AssetDatabase.AssetPathExists(materialPath))
            {
                var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (existingMaterial == null)
                {
                    return;
                }

                existingMaterial.shader = shader;
                existingMaterial.SetTexture("_BaseMap", texture);
                existingMaterial.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(existingMaterial);
                return;
            }

            var material = new Material(shader);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"[DungeonInn] Created material: {materialPath}");
        }

        static void MarkMaterialAddressable(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetPath,
            string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[DungeonInn] Asset not found: {assetPath}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
        }

        static void RegisterAnimationSet(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetFolder,
            string filePrefix,
            string addressPrefix)
        {
            var directions = new[] { "NE", "NW", "SE", "SW" };
            foreach (var direction in directions)
            {
                MarkSpriteAddressable(
                    settings,
                    group,
                    $"{assetFolder}/{filePrefix}Idle{direction}.png",
                    $"{addressPrefix}/Idle{direction}");

                for (var frame = 1; frame <= 2; frame++)
                {
                    MarkSpriteAddressable(
                        settings,
                        group,
                        $"{assetFolder}/{filePrefix}Walk{direction}{frame}.png",
                        $"{addressPrefix}/Walk{direction}{frame}");
                }
            }
        }

        static void MarkSpriteAddressable(AddressableAssetSettings settings, AddressableAssetGroup group, string assetPath, string address)
        {
            FixSpriteImportSettings(assetPath);

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[DungeonInn] Asset not found: {assetPath}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
        }

        static void FixSpriteImportSettings(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (importer.textureType == TextureImporterType.Sprite)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0f);
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        static void AssignToWorldLifetimeScope(
            MapMaterialSetSO mapSo,
            ActorSpriteVisualConfigSO actorSo,
            LayerPositionViewSettingsSO layerSettingsSo,
            WorldCameraSettingsSO cameraSettingsSo)
        {
            var allObjects = Resources.FindObjectsOfTypeAll<WorldLifetimeScope>();
            foreach (var scope in allObjects)
            {
                using var serialized = new SerializedObject(scope);
                var mapProp = serialized.FindProperty("mapMaterialSetSO");
                var actorProp = serialized.FindProperty("actorSpriteVisualConfigSO");
                var layerSettingsProp = serialized.FindProperty("layerPositionViewSettingsSO");
                var cameraSettingsProp = serialized.FindProperty("worldCameraSettingsSO");

                var changed = false;

                if (mapProp != null && mapProp.objectReferenceValue == null)
                {
                    mapProp.objectReferenceValue = mapSo;
                    changed = true;
                }

                if (actorProp != null && actorProp.objectReferenceValue == null)
                {
                    actorProp.objectReferenceValue = actorSo;
                    changed = true;
                }

                if (layerSettingsProp != null && layerSettingsProp.objectReferenceValue == null)
                {
                    layerSettingsProp.objectReferenceValue = layerSettingsSo;
                    changed = true;
                }

                if (cameraSettingsProp != null && cameraSettingsProp.objectReferenceValue == null)
                {
                    cameraSettingsProp.objectReferenceValue = cameraSettingsSo;
                    changed = true;
                }

                if (changed)
                {
                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(scope);
                    Debug.Log($"[DungeonInn] Assigned SOs to WorldLifetimeScope on {scope.gameObject.scene.name}");
                }
            }
        }

        static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
