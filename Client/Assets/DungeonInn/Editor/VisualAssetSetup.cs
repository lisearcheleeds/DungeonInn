using System;
using System.IO;
using DungeonInn.Domain.Actor;
using DungeonInn.GameSession.Settings;
using DungeonInn.View.Scene.ModuleScene.GameHUD;
using DungeonInn.View.Scene.MainScene.World;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonInn.Editor
{
    public static class VisualAssetSetup
    {
        const string SoDirectory = "Assets/DungeonInn/Runtime/StaticResources/Visual";
        const string ActorVisualDefinitionDirectory = SoDirectory + "/ActorVisualDefinitions";
        const string MapMaterialDirectory = "Assets/DungeonInn/Runtime/Art/Materials/Map";
        const string MapTextureDirectory = "Assets/DungeonInn/Runtime/Art/Textures/Map";
        const string MapMaterialSoPath = SoDirectory + "/MapMaterialSet.asset";
        const string ActorSpriteSoPath = SoDirectory + "/ActorSpriteVisualConfig.asset";
        const string LayerPositionViewSettingsPath = SoDirectory + "/LayerPositionViewSettings.asset";
        const string WorldCameraSettingsPath = SoDirectory + "/WorldCameraSettings.asset";
        const string WorldGameSettingsPath = SoDirectory + "/WorldGameSettings.asset";
        const string AdventurerNoviceVisualDefinitionPath = ActorVisualDefinitionDirectory + "/AdventurerNovice.asset";
        const string MonsterGoblinVisualDefinitionPath = ActorVisualDefinitionDirectory + "/MonsterGoblin.asset";
        const string MonsterOrcVisualDefinitionPath = ActorVisualDefinitionDirectory + "/MonsterOrc.asset";
        const string MonsterOgreVisualDefinitionPath = ActorVisualDefinitionDirectory + "/MonsterOgre.asset";
        const string MonsterGoblinArcherVisualDefinitionPath = ActorVisualDefinitionDirectory + "/MonsterGoblinArcher.asset";
        const string WorldPrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/World";
        const string GameHUDPrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/GameHUD";
        const string ActorPrefabPath = WorldPrefabDirectory + "/ActorView.prefab";
        const string StairUpPropPrefabPath = WorldPrefabDirectory + "/StairUpPropView.prefab";
        const string StairDownPropPrefabPath = WorldPrefabDirectory + "/StairDownPropView.prefab";
        const string ArrowProjectilePrefabPath = WorldPrefabDirectory + "/ArrowProjectileView.prefab";
        const string ScytheAreaEffectPrefabPath = WorldPrefabDirectory + "/ScytheAreaEffectView.prefab";
        const string ActorStatusViewPrefabPath = GameHUDPrefabDirectory + "/ActorStatusView.prefab";
        const string SelectedActorInspectorViewPrefabPath = GameHUDPrefabDirectory + "/SelectedActorInspectorView.prefab";
        const string PlayerEventLogViewPrefabPath = GameHUDPrefabDirectory + "/PlayerEventLogView.prefab";
        const string WorldHudViewPrefabPath = GameHUDPrefabDirectory + "/WorldHudView.prefab";
        const string InnStatusPanelViewPrefabPath = GameHUDPrefabDirectory + "/InnStatusPanelView.prefab";
        const string MinimapViewPrefabPath = GameHUDPrefabDirectory + "/MinimapView.prefab";
        const string DungeonInfoWindowPrefabPath = GameHUDPrefabDirectory + "/ScreenStack/DungeonInfoWindow.prefab";
        const string GuildManagementWindowPrefabPath = GameHUDPrefabDirectory + "/ScreenStack/GuildManagementWindow.prefab";
        const string MarketWindowPrefabPath = GameHUDPrefabDirectory + "/ScreenStack/MarketWindow.prefab";
        const string SystemMenuWindowPrefabPath = GameHUDPrefabDirectory + "/ScreenStack/SystemMenuWindow.prefab";
        const string SaveSlotSelectionWindowPrefabPath = GameHUDPrefabDirectory + "/ScreenStack/SaveSlotSelectionWindow.prefab";
        const string EffectDummySpritePath = "Assets/DungeonInn/Runtime/Art/Sprites/Effect/Dummy.png";
        const string AddressablesGroupName = "DungeonInn Visual";
        const string MapMaterialShaderName = "Universal Render Pipeline/Lit";

        [InitializeOnLoadMethod]
        static void AutoSetup()
        {
            ValidateSetup();
        }

        public static bool ValidateSetup()
        {
            var assetsExist = AssetDatabase.AssetPathExists(MapMaterialSoPath) &&
                AssetDatabase.AssetPathExists(ActorSpriteSoPath) &&
                AssetDatabase.AssetPathExists(LayerPositionViewSettingsPath) &&
                AssetDatabase.AssetPathExists(WorldCameraSettingsPath) &&
                AssetDatabase.AssetPathExists(WorldGameSettingsPath) &&
                AssetDatabase.AssetPathExists(ActorPrefabPath) &&
                AssetDatabase.AssetPathExists(StairUpPropPrefabPath) &&
                AssetDatabase.AssetPathExists(StairDownPropPrefabPath) &&
                AssetDatabase.AssetPathExists(ArrowProjectilePrefabPath) &&
                AssetDatabase.AssetPathExists(ScytheAreaEffectPrefabPath) &&
                AssetDatabase.AssetPathExists(ActorStatusViewPrefabPath) &&
                AssetDatabase.AssetPathExists(SelectedActorInspectorViewPrefabPath) &&
                AssetDatabase.AssetPathExists(PlayerEventLogViewPrefabPath) &&
                AssetDatabase.AssetPathExists(WorldHudViewPrefabPath) &&
                AssetDatabase.AssetPathExists(InnStatusPanelViewPrefabPath) &&
                AssetDatabase.AssetPathExists(MinimapViewPrefabPath) &&
                AssetDatabase.AssetPathExists(AdventurerNoviceVisualDefinitionPath) &&
                AssetDatabase.AssetPathExists(MonsterGoblinVisualDefinitionPath) &&
                AssetDatabase.AssetPathExists(MonsterOrcVisualDefinitionPath) &&
                AssetDatabase.AssetPathExists(MonsterOgreVisualDefinitionPath) &&
                AssetDatabase.AssetPathExists(MonsterGoblinArcherVisualDefinitionPath);
            if (!AssetDatabase.AssetPathExists(SelectedActorInspectorViewPrefabPath) ||
                !IsSelectedActorInspectorViewPrefabWired())
            {
                Debug.LogWarning(
                    "[DungeonInn] SelectedActorInspectorView prefab is missing or not wired. Run DungeonInn/Setup Visual Assets to apply setup.");
            }

            if (!assetsExist)
            {
                Debug.LogWarning(
                    "[DungeonInn] Visual assets are incomplete. Run DungeonInn/Setup Visual Assets to apply setup.");
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var group = settings?.FindGroup(AddressablesGroupName);
            if (settings == null || group == null || group.GetSchema<BundledAssetGroupSchema>() == null)
            {
                Debug.LogWarning(
                    "[DungeonInn] DungeonInn Visual Addressables group is incomplete. Run DungeonInn/Setup Visual Addressables to apply setup.");
                return false;
            }

            return assetsExist;
        }

        static bool IsSelectedActorInspectorViewPrefabWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SelectedActorInspectorViewPrefabPath);
            var view = prefab != null ? prefab.GetComponent<SelectedActorInspectorView>() : null;
            if (view == null)
            {
                return false;
            }

            using var serializedView = new SerializedObject(view);
            return AreObjectArrayElementsAssigned(serializedView.FindProperty("statRows"), 6) &&
                AreObjectArrayElementsAssigned(serializedView.FindProperty("equipmentRows"), 3) &&
                AreObjectArrayElementsAssigned(serializedView.FindProperty("inventoryRows"), 12) &&
                AreObjectArrayElementsAssigned(serializedView.FindProperty("effectRows"), 8);
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
            ApplySetup();
        }

        public static void ApplySetup()
        {
            EnsureDirectory(SoDirectory);

            var actorPrefab = CreateOrLoadActorViewPrefab();
            var mapSo = CreateOrLoadMapMaterialSO();
            var actorSo = CreateOrLoadActorSpriteSO(actorPrefab);
            CreateOrLoadPropViewPrefab(StairUpPropPrefabPath, "StairUpPropView");
            CreateOrLoadPropViewPrefab(StairDownPropPrefabPath, "StairDownPropView");
            CreateOrLoadProjectileViewPrefab();
            CreateOrLoadAreaEffectViewPrefab();
            CreateOrLoadActorStatusViewPrefab();
            CreateOrLoadSelectedActorInspectorViewPrefab();
            CreateOrLoadPlayerEventLogViewPrefab();
            CreateOrLoadWorldHudViewPrefab();
            CreateOrUpdateActorVisualDefinitions();
            var layerSettingsSo = CreateOrLoadLayerPositionViewSettingsSO();
            var cameraSettingsSo = CreateOrLoadWorldCameraSettingsSO();
            var gameSettingsSo = CreateOrLoadWorldGameSettingsSO();

            AssignToWorldLifetimeScope(mapSo, actorSo, layerSettingsSo, cameraSettingsSo, gameSettingsSo);
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

        static WorldGameSettingsSO CreateOrLoadWorldGameSettingsSO()
        {
            if (AssetDatabase.AssetPathExists(WorldGameSettingsPath))
            {
                return AssetDatabase.LoadAssetAtPath<WorldGameSettingsSO>(WorldGameSettingsPath);
            }

            var settings = ScriptableObject.CreateInstance<WorldGameSettingsSO>();
            AssetDatabase.CreateAsset(settings, WorldGameSettingsPath);
            Debug.Log($"[DungeonInn] Created {WorldGameSettingsPath}");
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

        static GameObject CreateOrLoadPropViewPrefab(string prefabPath, string prefabName)
        {
            if (AssetDatabase.AssetPathExists(prefabPath))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            EnsureDirectory(WorldPrefabDirectory);
            var propObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            propObject.name = prefabName;
            propObject.transform.localScale = Vector3.one * 0.5f;
            var collider = propObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(propObject, prefabPath);
            Object.DestroyImmediate(propObject);

            Debug.Log($"[DungeonInn] Created {prefabPath}");
            return prefab;
        }

        static ProjectileView CreateOrLoadProjectileViewPrefab()
        {
            if (AssetDatabase.AssetPathExists(ArrowProjectilePrefabPath))
            {
                var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowProjectilePrefabPath);
                return projectilePrefab != null ? projectilePrefab.GetComponent<ProjectileView>() : null;
            }

            EnsureDirectory(WorldPrefabDirectory);
            var projectileObject = new GameObject("ProjectileView");
            var spriteRenderer = projectileObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = LoadEffectDummySprite();
            spriteRenderer.color = Color.yellow;
            projectileObject.AddComponent<ProjectileView>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(projectileObject, ArrowProjectilePrefabPath);
            Object.DestroyImmediate(projectileObject);

            Debug.Log($"[DungeonInn] Created {ArrowProjectilePrefabPath}");
            return prefab != null ? prefab.GetComponent<ProjectileView>() : null;
        }

        static AreaEffectView CreateOrLoadAreaEffectViewPrefab()
        {
            if (AssetDatabase.AssetPathExists(ScytheAreaEffectPrefabPath))
            {
                var areaEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScytheAreaEffectPrefabPath);
                return areaEffectPrefab != null ? areaEffectPrefab.GetComponent<AreaEffectView>() : null;
            }

            EnsureDirectory(WorldPrefabDirectory);
            var areaEffectObject = new GameObject("AreaEffectView");
            var spriteRenderer = areaEffectObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = LoadEffectDummySprite();
            spriteRenderer.color = Color.magenta;
            areaEffectObject.AddComponent<AreaEffectView>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(areaEffectObject, ScytheAreaEffectPrefabPath);
            Object.DestroyImmediate(areaEffectObject);

            Debug.Log($"[DungeonInn] Created {ScytheAreaEffectPrefabPath}");
            return prefab != null ? prefab.GetComponent<AreaEffectView>() : null;
        }

        static ActorStatusView CreateOrLoadActorStatusViewPrefab()
        {
            if (AssetDatabase.AssetPathExists(ActorStatusViewPrefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActorStatusViewPrefabPath);
                return prefab != null ? prefab.GetComponent<ActorStatusView>() : null;
            }

            EnsureDirectory(WorldPrefabDirectory);
            var viewObject = new GameObject("ActorStatusView", typeof(RectTransform));
            viewObject.AddComponent<ActorStatusView>();
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(viewObject, ActorStatusViewPrefabPath);
            Object.DestroyImmediate(viewObject);

            Debug.Log($"[DungeonInn] Created {ActorStatusViewPrefabPath}");
            return savedPrefab != null ? savedPrefab.GetComponent<ActorStatusView>() : null;
        }

        static SelectedActorInspectorView CreateOrLoadSelectedActorInspectorViewPrefab()
        {
            EnsureDirectory(GameHUDPrefabDirectory);
            var prefabExists = AssetDatabase.AssetPathExists(SelectedActorInspectorViewPrefabPath);
            var viewObject = prefabExists
                ? PrefabUtility.LoadPrefabContents(SelectedActorInspectorViewPrefabPath)
                : new GameObject("SelectedActorInspectorView", typeof(RectTransform));

            ConfigureSelectedActorInspectorViewPrefab(viewObject);
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(viewObject, SelectedActorInspectorViewPrefabPath);

            if (prefabExists)
            {
                PrefabUtility.UnloadPrefabContents(viewObject);
            }
            else
            {
                Object.DestroyImmediate(viewObject);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonInn] Updated {SelectedActorInspectorViewPrefabPath}");
            return savedPrefab != null ? savedPrefab.GetComponent<SelectedActorInspectorView>() : null;
        }

        static PlayerEventLogView CreateOrLoadPlayerEventLogViewPrefab()
        {
            if (AssetDatabase.AssetPathExists(PlayerEventLogViewPrefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerEventLogViewPrefabPath);
                return prefab != null ? prefab.GetComponent<PlayerEventLogView>() : null;
            }

            EnsureDirectory(WorldPrefabDirectory);
            var viewObject = new GameObject("PlayerEventLogView", typeof(RectTransform));
            viewObject.AddComponent<PlayerEventLogView>();
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(viewObject, PlayerEventLogViewPrefabPath);
            Object.DestroyImmediate(viewObject);

            Debug.Log($"[DungeonInn] Created {PlayerEventLogViewPrefabPath}");
            return savedPrefab != null ? savedPrefab.GetComponent<PlayerEventLogView>() : null;
        }

        static WorldHudView CreateOrLoadWorldHudViewPrefab()
        {
            if (AssetDatabase.AssetPathExists(WorldHudViewPrefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldHudViewPrefabPath);
                return prefab != null ? prefab.GetComponent<WorldHudView>() : null;
            }

            EnsureDirectory(WorldPrefabDirectory);
            var viewObject = new GameObject("WorldHudView", typeof(RectTransform));
            viewObject.AddComponent<WorldHudView>();
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(viewObject, WorldHudViewPrefabPath);
            Object.DestroyImmediate(viewObject);

            Debug.Log($"[DungeonInn] Created {WorldHudViewPrefabPath}");
            return savedPrefab != null ? savedPrefab.GetComponent<WorldHudView>() : null;
        }

        static void ConfigureSelectedActorInspectorViewPrefab(GameObject viewObject)
        {
            const int statRowCount = 6;
            const int equipmentRowCount = 3;
            const int inventoryRowCount = 12;
            const int effectRowCount = 8;
            var panelColor = new Color(0.05f, 0.06f, 0.07f, 0.86f);
            var gaugeBackgroundColor = new Color(0.10f, 0.11f, 0.12f, 1f);
            var headerTextColor = new Color(0.95f, 0.94f, 0.88f, 1f);
            var bodyTextColor = new Color(0.82f, 0.84f, 0.84f, 1f);
            var mutedTextColor = new Color(0.57f, 0.60f, 0.61f, 1f);
            var gaugeTextColor = new Color(0.94f, 0.94f, 0.90f, 1f);

            viewObject.name = "SelectedActorInspectorView";
            var rootRect = viewObject.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = viewObject.AddComponent<RectTransform>();
            }

            while (0 < rootRect.childCount)
            {
                Object.DestroyImmediate(rootRect.GetChild(0).gameObject);
            }

            rootRect.sizeDelta = new Vector2(360f, 640f);
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(16f, -16f);
            var image = viewObject.GetComponent<Image>();
            if (image == null)
            {
                image = viewObject.AddComponent<Image>();
            }

            image.color = panelColor;
            var fitter = viewObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = viewObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var group = viewObject.GetComponent<VerticalLayoutGroup>();
            if (group == null)
            {
                group = viewObject.AddComponent<VerticalLayoutGroup>();
            }

            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.spacing = 8f;
            group.padding = new RectOffset(14, 14, 12, 12);
            var view = viewObject.GetComponent<SelectedActorInspectorView>();
            if (view == null)
            {
                view = viewObject.AddComponent<SelectedActorInspectorView>();
            }

            var titleText = CreateSelectedActorInspectorText("Title", rootRect, 20, headerTextColor, 1);
            var roleText = CreateSelectedActorInspectorText("Role", rootRect, 12, mutedTextColor, 0);
            var levelText = CreateSelectedActorInspectorText("Level", rootRect, 13, bodyTextColor, 0);
            var hpGauge = CreateSelectedActorInspectorGauge(rootRect, "HPGauge", gaugeBackgroundColor, gaugeTextColor);
            var mpGauge = CreateSelectedActorInspectorGauge(rootRect, "MPGauge", gaugeBackgroundColor, gaugeTextColor);
            var locationText = CreateSelectedActorInspectorText("Location", rootRect, 12, bodyTextColor, 0);
            var goalText = CreateSelectedActorInspectorText("Goal", rootRect, 12, bodyTextColor, 0);
            var conditionText = CreateSelectedActorInspectorText("Condition", rootRect, 12, bodyTextColor, 0);
            var goldText = CreateSelectedActorInspectorText("Gold", rootRect, 12, bodyTextColor, 0);
            var statRows = CreateSelectedActorInspectorSection(rootRect, "Stats", statRowCount, bodyTextColor, headerTextColor, out var statTexts);
            var equipmentRows = CreateSelectedActorInspectorSection(rootRect, "Equipment", equipmentRowCount, bodyTextColor, headerTextColor, out var equipmentTexts);
            var inventoryRows = CreateSelectedActorInspectorSection(rootRect, "Items", inventoryRowCount, bodyTextColor, headerTextColor, out var inventoryTexts);
            var effectRows = CreateSelectedActorInspectorSection(rootRect, "Effects", effectRowCount, bodyTextColor, headerTextColor, out var effectTexts);

            using var serializedView = new SerializedObject(view);
            serializedView.FindProperty("titleText").objectReferenceValue = titleText;
            serializedView.FindProperty("roleText").objectReferenceValue = roleText;
            serializedView.FindProperty("levelText").objectReferenceValue = levelText;
            serializedView.FindProperty("locationText").objectReferenceValue = locationText;
            serializedView.FindProperty("goalText").objectReferenceValue = goalText;
            serializedView.FindProperty("conditionText").objectReferenceValue = conditionText;
            serializedView.FindProperty("goldText").objectReferenceValue = goldText;
            serializedView.FindProperty("hpGauge").objectReferenceValue = hpGauge;
            serializedView.FindProperty("mpGauge").objectReferenceValue = mpGauge;
            AssignObjectArray(serializedView.FindProperty("statRows"), statRows);
            AssignObjectArray(serializedView.FindProperty("statRowTexts"), statTexts);
            AssignObjectArray(serializedView.FindProperty("equipmentRows"), equipmentRows);
            AssignObjectArray(serializedView.FindProperty("equipmentRowTexts"), equipmentTexts);
            AssignObjectArray(serializedView.FindProperty("inventoryRows"), inventoryRows);
            AssignObjectArray(serializedView.FindProperty("inventoryRowTexts"), inventoryTexts);
            AssignObjectArray(serializedView.FindProperty("effectRows"), effectRows);
            AssignObjectArray(serializedView.FindProperty("effectRowTexts"), effectTexts);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        static RectTransform CreateSelectedActorInspectorVerticalGroup(
            string objectName,
            Transform parent,
            float horizontalPadding,
            float verticalPadding,
            float spacing)
        {
            var rectTransform = CreateSelectedActorInspectorRectTransform(objectName, parent);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            var group = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.spacing = spacing;
            group.padding = new RectOffset(
                Mathf.RoundToInt(horizontalPadding),
                Mathf.RoundToInt(horizontalPadding),
                Mathf.RoundToInt(verticalPadding),
                Mathf.RoundToInt(verticalPadding));
            return rectTransform;
        }

        static GameObject[] CreateSelectedActorInspectorSection(
            Transform parent,
            string title,
            int rowCount,
            Color bodyTextColor,
            Color headerTextColor,
            out Component[] rowTexts)
        {
            var section = CreateSelectedActorInspectorVerticalGroup(title, parent, 0f, 0f, 3f);
            SetText(CreateSelectedActorInspectorText($"{title}Header", section, 12, headerTextColor, 1), title);
            var rows = new GameObject[rowCount];
            rowTexts = new Component[rowCount];
            for (var index = 0; index < rowCount; index++)
            {
                var rowText = CreateSelectedActorInspectorText(
                    $"{title}Row{index + 1:00}",
                    section,
                    12,
                    bodyTextColor,
                    0);
                SetText(rowText, string.Empty);
                rows[index] = rowText.gameObject;
                rowTexts[index] = rowText;
            }

            return rows;
        }

        static ValueGaugeView CreateSelectedActorInspectorGauge(
            Transform parent,
            string objectName,
            Color backgroundColor,
            Color textColor)
        {
            var gaugeRoot = CreateSelectedActorInspectorRectTransform(objectName, parent);
            var layout = gaugeRoot.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 20f;
            layout.preferredHeight = 20f;
            var background = CreateSelectedActorInspectorGaugeLayer("Background", gaugeRoot, false);
            background.color = backgroundColor;
            var fill = CreateSelectedActorInspectorGaugeLayer("Fill", gaugeRoot, true);
            var valueText = CreateSelectedActorInspectorText("Value", gaugeRoot, 12, textColor, 0);
            Stretch((RectTransform)valueText.transform);
            SetTextAlignment(valueText, 2, 512);
            var gauge = gaugeRoot.gameObject.AddComponent<ValueGaugeView>();
            using var serializedGauge = new SerializedObject(gauge);
            serializedGauge.FindProperty("fillImage").objectReferenceValue = fill;
            serializedGauge.FindProperty("valueText").objectReferenceValue = valueText;
            serializedGauge.ApplyModifiedPropertiesWithoutUndo();
            return gauge;
        }

        static Image CreateSelectedActorInspectorGaugeLayer(string objectName, Transform parent, bool filled)
        {
            var rectTransform = CreateSelectedActorInspectorRectTransform(objectName, parent);
            Stretch(rectTransform);
            var image = rectTransform.gameObject.AddComponent<Image>();
            if (filled)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                image.fillClockwise = true;
            }

            return image;
        }

        static Component CreateSelectedActorInspectorText(
            string objectName,
            Transform parent,
            int fontSize,
            Color color,
            int fontStyle)
        {
            var rectTransform = CreateSelectedActorInspectorRectTransform(objectName, parent);
            var text = (Component)rectTransform.gameObject.AddComponent(ResolveTextMeshProType());
            var serializedText = new SerializedObject(text);
            SetFloat(serializedText, "m_fontSize", fontSize);
            SetColor(serializedText, "m_fontColor", color);
            SetInt(serializedText, "m_FontStyle", fontStyle);
            SetBool(serializedText, "m_RaycastTarget", false);
            SetInt(serializedText, "m_textWrappingMode", 0);
            SetInt(serializedText, "m_overflowMode", 1);
            serializedText.ApplyModifiedPropertiesWithoutUndo();
            var layout = rectTransform.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = fontSize + 6f;
            return text;
        }

        static Type ResolveTextMeshProType()
        {
            var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (textType == null)
            {
                throw new InvalidOperationException("TextMeshProUGUI type was not found.");
            }

            return textType;
        }

        static void SetText(Component text, string value)
        {
            var serializedText = new SerializedObject(text);
            var property = serializedText.FindProperty("m_text");
            if (property != null)
            {
                property.stringValue = value;
            }

            serializedText.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetTextAlignment(Component text, int horizontalAlignment, int verticalAlignment)
        {
            var serializedText = new SerializedObject(text);
            SetInt(serializedText, "m_HorizontalAlignment", horizontalAlignment);
            SetInt(serializedText, "m_VerticalAlignment", verticalAlignment);
            serializedText.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        static void SetColor(SerializedObject serializedObject, string propertyName, Color value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.colorValue = value;
            }
        }

        static RectTransform CreateSelectedActorInspectorRectTransform(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        static void AssignObjectArray<T>(SerializedProperty property, T[] values)
            where T : Object
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        static bool AreObjectArrayElementsAssigned(SerializedProperty property, int expectedSize)
        {
            if (property.arraySize != expectedSize)
            {
                return false;
            }

            for (var i = 0; i < expectedSize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    return false;
                }
            }

            return true;
        }

        static Sprite LoadEffectDummySprite()
        {
            FixSpriteImportSettings(EffectDummySpritePath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(EffectDummySpritePath);
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
            RegisterContentPrefabs(settings, group);
            RegisterActorVisualDefinitions(settings, group);
            RegisterSettingsSOs(settings, group);

            EditorUtility.SetDirty(settings);
        }

        static void RegisterContentPrefabs(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            MarkAssetAddressable(settings, group, StairUpPropPrefabPath, "World/Prop/StairUp");
            MarkAssetAddressable(settings, group, StairDownPropPrefabPath, "World/Prop/StairDown");
            MarkAssetAddressable(settings, group, ArrowProjectilePrefabPath, "World/Projectile/Arrow");
            MarkAssetAddressable(settings, group, ScytheAreaEffectPrefabPath, "World/AreaEffect/Scythe");
            MarkAssetAddressable(settings, group, ActorStatusViewPrefabPath, "GameHUD/UI/ActorStatusView");
            MarkAssetAddressable(settings, group, SelectedActorInspectorViewPrefabPath, "GameHUD/UI/SelectedActorInspectorView");
            MarkAssetAddressable(settings, group, PlayerEventLogViewPrefabPath, "GameHUD/UI/PlayerEventLogView");
            MarkAssetAddressable(settings, group, WorldHudViewPrefabPath, "GameHUD/UI/WorldHudView");
            MarkAssetAddressable(settings, group, InnStatusPanelViewPrefabPath, "GameHUD/UI/InnStatusPanelView");
            MarkAssetAddressable(settings, group, MinimapViewPrefabPath, "GameHUD/UI/MinimapView");
            MarkAssetAddressable(settings, group, DungeonInfoWindowPrefabPath, "DungeonInfoWindow");
            MarkAssetAddressable(settings, group, GuildManagementWindowPrefabPath, "GuildManagementWindow");
            MarkAssetAddressable(settings, group, MarketWindowPrefabPath, "MarketWindow");
            MarkAssetAddressable(settings, group, SystemMenuWindowPrefabPath, "SystemMenuWindow");
            MarkAssetAddressable(settings, group, SaveSlotSelectionWindowPrefabPath, "SaveSlotSelectionWindow");
        }

        static void RegisterSettingsSOs(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            MarkAssetAddressable(settings, group, LayerPositionViewSettingsPath, "Config/LayerPositionViewSettings");
            MarkAssetAddressable(settings, group, WorldCameraSettingsPath, "Config/WorldCameraSettings");
            MarkAssetAddressable(settings, group, WorldGameSettingsPath, "Config/WorldGameSettings");
        }

        static void RegisterActorVisualDefinitions(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            MarkAssetAddressable(settings, group, AdventurerNoviceVisualDefinitionPath, "World/ActorVisual/AdventurerNovice");
            MarkAssetAddressable(settings, group, MonsterGoblinVisualDefinitionPath, "World/ActorVisual/MonsterGoblin");
            MarkAssetAddressable(settings, group, MonsterOrcVisualDefinitionPath, "World/ActorVisual/MonsterOrc");
            MarkAssetAddressable(settings, group, MonsterOgreVisualDefinitionPath, "World/ActorVisual/MonsterOgre");
            MarkAssetAddressable(settings, group, MonsterGoblinArcherVisualDefinitionPath, "World/ActorVisual/MonsterGoblinArcher");
        }

        static void CreateOrUpdateActorVisualDefinitions()
        {
            EnsureDirectory(ActorVisualDefinitionDirectory);

            CreateOrUpdateActorVisualDefinition(
                AdventurerNoviceVisualDefinitionPath,
                "adventurer_novice",
                ActorVisualSizeTier.AdventurerS,
                "Assets/DungeonInn/Runtime/Art/Sprites/Adventurer",
                "Adventurer");

            CreateOrUpdateActorVisualDefinition(
                MonsterGoblinVisualDefinitionPath,
                "monster_goblin",
                ActorVisualSizeTier.MonsterS,
                "Assets/DungeonInn/Runtime/Art/Sprites/Goblin",
                "Goblin");

            CreateOrUpdateActorVisualDefinition(
                MonsterOrcVisualDefinitionPath,
                "monster_orc",
                ActorVisualSizeTier.MonsterS,
                "Assets/DungeonInn/Runtime/Art/Sprites/Goblin",
                "Goblin");

            CreateOrUpdateActorVisualDefinition(
                MonsterOgreVisualDefinitionPath,
                "monster_ogre",
                ActorVisualSizeTier.MonsterL,
                "Assets/DungeonInn/Runtime/Art/Sprites/Goblin",
                "Goblin");

            CreateOrUpdateActorVisualDefinition(
                MonsterGoblinArcherVisualDefinitionPath,
                "monster_goblin_archer",
                ActorVisualSizeTier.MonsterS,
                "Assets/DungeonInn/Runtime/Art/Sprites/Goblin",
                "Goblin");
        }

        static void CreateOrUpdateActorVisualDefinition(
            string assetPath,
            string visualId,
            ActorVisualSizeTier visualSizeTier,
            string spriteFolder,
            string spritePrefix)
        {
            var definition = AssetDatabase.AssetPathExists(assetPath)
                ? AssetDatabase.LoadAssetAtPath<ActorVisualDefinitionSO>(assetPath)
                : null;
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<ActorVisualDefinitionSO>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            using var serialized = new SerializedObject(definition);
            serialized.FindProperty("visualId").stringValue = visualId;
            serialized.FindProperty("visualSizeTier").enumValueIndex = (int)visualSizeTier;
            var idleEntries = serialized.FindProperty("idleEntries");
            var walkEntries = serialized.FindProperty("walkEntries");
            var workEntries = serialized.FindProperty("workEntries");
            var attackEntries = serialized.FindProperty("attackEntries");
            var damageEntries = serialized.FindProperty("damageEntries");
            var deadEntries = serialized.FindProperty("deadEntries");
            idleEntries.ClearArray();
            walkEntries.ClearArray();
            workEntries.ClearArray();
            attackEntries.ClearArray();
            damageEntries.ClearArray();
            deadEntries.ClearArray();

            foreach (var direction in new[] { "NE", "NW", "SE", "SW" })
            {
                AddActorVisualEntry(
                    idleEntries,
                    direction,
                    fps: 1f,
                    loop: true,
                    new[] { LoadActorSprite(spriteFolder, spritePrefix, $"Idle{direction}") });
                AddActorVisualEntry(
                    walkEntries,
                    direction,
                    fps: 4f,
                    loop: true,
                    new[]
                    {
                        LoadActorSprite(spriteFolder, spritePrefix, $"Walk{direction}1"),
                        LoadActorSprite(spriteFolder, spritePrefix, $"Walk{direction}2")
                    });
                AddActorVisualEntry(
                    workEntries,
                    direction,
                    fps: 1f,
                    loop: true,
                    new[] { LoadActorSprite(spriteFolder, spritePrefix, $"Idle{direction}") });
                AddActorVisualEntry(
                    attackEntries,
                    direction,
                    fps: 8f,
                    loop: false,
                    new[]
                    {
                        LoadActorSprite(spriteFolder, spritePrefix, $"Walk{direction}1"),
                        LoadActorSprite(spriteFolder, spritePrefix, $"Walk{direction}2")
                    });
                AddActorVisualEntry(
                    damageEntries,
                    direction,
                    fps: 4f,
                    loop: false,
                    new[] { LoadActorSprite(spriteFolder, spritePrefix, $"Idle{direction}") });
                AddActorVisualEntry(
                    deadEntries,
                    direction,
                    fps: 1f,
                    loop: false,
                    new[] { LoadActorSprite(spriteFolder, spritePrefix, $"Idle{direction}") });
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        static Sprite LoadActorSprite(string spriteFolder, string spritePrefix, string suffix)
        {
            var path = $"{spriteFolder}/{spritePrefix}{suffix}.png";
            FixSpriteImportSettings(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void AddActorVisualEntry(
            SerializedProperty entries,
            string directionName,
            float fps,
            bool loop,
            Sprite[] sprites)
        {
            var index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("direction").enumValueIndex = (int)Enum.Parse(typeof(ActorAnimationDirection), directionName);
            entry.FindPropertyRelative("fps").floatValue = fps;
            entry.FindPropertyRelative("loop").boolValue = loop;
            var spritesProperty = entry.FindPropertyRelative("sprites");
            spritesProperty.ClearArray();
            for (var spriteIndex = 0; spriteIndex < sprites.Length; spriteIndex++)
            {
                spritesProperty.InsertArrayElementAtIndex(spriteIndex);
                spritesProperty.GetArrayElementAtIndex(spriteIndex).objectReferenceValue = sprites[spriteIndex];
            }
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
                MarkAssetAddressable(settings, group, materialPath, address);

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

        static void MarkAssetAddressable(
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
            WorldCameraSettingsSO cameraSettingsSo,
            WorldGameSettingsSO gameSettingsSo)
        {
            var allObjects = Resources.FindObjectsOfTypeAll<WorldLifetimeScope>();
            foreach (var scope in allObjects)
            {
                using var serialized = new SerializedObject(scope);
                var mapProp = serialized.FindProperty("mapMaterialSetSO");
                var actorProp = serialized.FindProperty("actorSpriteVisualConfigSO");

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
