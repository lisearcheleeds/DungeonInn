using System;
using DungeonInn.View.Scene.ModuleScene.GameUI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonInn.Editor.OneShot
{
    public static class SetupInnStatusPanelViewPrefabOneShot
    {
        const int GuestDisplayLimit = 5;
        const string GameUIPrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/GameUI";
        const string InnStatusPanelViewPrefabPath = GameUIPrefabDirectory + "/InnStatusPanelView.prefab";
        const string AddressablesGroupName = "DungeonInn Visual";
        const string InnStatusPanelViewAddress = "GameUI/InnStatusPanelView";

        public static void Run()
        {
            CreateOrLoadInnStatusPanelViewPrefab();
            RegisterAddressable();
        }

        static InnStatusPanelView CreateOrLoadInnStatusPanelViewPrefab()
        {
            EnsureDirectory(GameUIPrefabDirectory);
            var prefabExists = AssetDatabase.AssetPathExists(InnStatusPanelViewPrefabPath);
            var panelObject = prefabExists
                ? PrefabUtility.LoadPrefabContents(InnStatusPanelViewPrefabPath)
                : new GameObject("InnStatusPanelView", typeof(RectTransform));

            ConfigureInnStatusPanelViewPrefab(panelObject);
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(panelObject, InnStatusPanelViewPrefabPath);

            if (prefabExists)
            {
                PrefabUtility.UnloadPrefabContents(panelObject);
            }
            else
            {
                Object.DestroyImmediate(panelObject);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonInn] Updated {InnStatusPanelViewPrefabPath}");
            return savedPrefab != null ? savedPrefab.GetComponent<InnStatusPanelView>() : null;
        }

        static void ConfigureInnStatusPanelViewPrefab(GameObject panelObject)
        {
            panelObject.name = "InnStatusPanelView";
            var rootRect = panelObject.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = panelObject.AddComponent<RectTransform>();
            }

            while (0 < rootRect.childCount)
            {
                Object.DestroyImmediate(rootRect.GetChild(0).gameObject);
            }

            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-16f, -64f);
            rootRect.sizeDelta = new Vector2(340f, 286f);

            var panel = CreatePanel(rootRect);
            CreateText(
                panel,
                "TitleText",
                "Inn Status",
                new Vector2(12f, -10f),
                new Vector2(316f, 22f),
                18,
                new Color(0.95f, 0.95f, 0.95f, 1f));

            var guestRows = new GameObject[GuestDisplayLimit];
            var guestNameTexts = new Component[GuestDisplayLimit];
            var guestHpTexts = new Component[GuestDisplayLimit];
            var guestRecoveryTexts = new Component[GuestDisplayLimit];

            for (var i = 0; i < GuestDisplayLimit; i++)
            {
                var row = CreateGuestRow(panel, i);
                guestRows[i] = row.gameObject;
                guestNameTexts[i] = CreateText(
                    row,
                    "NameText",
                    "Guest",
                    new Vector2(0f, 0f),
                    new Vector2(136f, 20f),
                    13,
                    new Color(0.9f, 0.93f, 0.95f, 1f));
                guestHpTexts[i] = CreateText(
                    row,
                    "HpText",
                    "HP 100%",
                    new Vector2(142f, 0f),
                    new Vector2(72f, 20f),
                    13,
                    new Color(0.75f, 1f, 0.75f, 1f));
                guestRecoveryTexts[i] = CreateText(
                    row,
                    "RecoveryText",
                    "Recovery --",
                    new Vector2(220f, 0f),
                    new Vector2(96f, 20f),
                    13,
                    new Color(0.78f, 0.88f, 1f, 1f));
            }

            var emptyText = CreateText(
                panel,
                "EmptyGuestText",
                "No guests",
                new Vector2(12f, -42f),
                new Vector2(316f, 22f),
                13,
                new Color(0.68f, 0.72f, 0.76f, 1f));

            var dayText = CreateText(panel, "DayText", "Day 1", new Vector2(12f, -180f), new Vector2(316f, 18f), 13, Color.white);
            var salesText = CreateText(panel, "SalesText", "Sales 0 G", new Vector2(12f, -200f), new Vector2(316f, 18f), 13, Color.white);
            var guestCountText = CreateText(panel, "GuestCountText", "Guests 0 / Rejected 0", new Vector2(12f, -220f), new Vector2(316f, 18f), 13, Color.white);
            var occupancyText = CreateText(panel, "OccupancyText", "Rooms 0/0 (0%)", new Vector2(12f, -240f), new Vector2(316f, 18f), 13, Color.white);
            var guildGoldText = CreateText(panel, "GuildGoldText", "Guild Gold 0 G", new Vector2(12f, -260f), new Vector2(316f, 18f), 13, new Color(1f, 0.88f, 0.42f, 1f));

            var view = panelObject.GetComponent<InnStatusPanelView>();
            if (view == null)
            {
                view = panelObject.AddComponent<InnStatusPanelView>();
            }

            var serializedView = new SerializedObject(view);
            AssignObjectArray(serializedView.FindProperty("guestRows"), guestRows);
            AssignObjectArray(serializedView.FindProperty("guestNameTexts"), guestNameTexts);
            AssignObjectArray(serializedView.FindProperty("guestHpTexts"), guestHpTexts);
            AssignObjectArray(serializedView.FindProperty("guestRecoveryTexts"), guestRecoveryTexts);
            serializedView.FindProperty("emptyGuestText").objectReferenceValue = emptyText;
            serializedView.FindProperty("dayText").objectReferenceValue = dayText;
            serializedView.FindProperty("salesText").objectReferenceValue = salesText;
            serializedView.FindProperty("guestCountText").objectReferenceValue = guestCountText;
            serializedView.FindProperty("occupancyText").objectReferenceValue = occupancyText;
            serializedView.FindProperty("guildGoldText").objectReferenceValue = guildGoldText;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        static RectTransform CreatePanel(RectTransform parent)
        {
            var panelObject = new GameObject("Panel", typeof(RectTransform));
            var rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            var image = panelObject.AddComponent<Image>();
            image.color = new Color(0.05f, 0.06f, 0.07f, 0.86f);
            return rectTransform;
        }

        static RectTransform CreateGuestRow(RectTransform parent, int index)
        {
            var rowObject = new GameObject($"GuestRow{index + 1}", typeof(RectTransform));
            var rectTransform = rowObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(12f, -42f - index * 24f);
            rectTransform.sizeDelta = new Vector2(316f, 20f);
            return rectTransform;
        }

        static Component CreateText(
            Transform parent,
            string objectName,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            Color color)
        {
            var textType = ResolveTextMeshProType();
            var textObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            var label = (Component)textObject.AddComponent(textType);
            var serializedLabel = new SerializedObject(label);
            serializedLabel.FindProperty("m_text").stringValue = text;
            serializedLabel.FindProperty("m_fontSize").floatValue = fontSize;
            serializedLabel.FindProperty("m_fontColor").colorValue = color;
            serializedLabel.FindProperty("m_RaycastTarget").boolValue = false;
            serializedLabel.FindProperty("m_HorizontalAlignment").intValue = 1;
            serializedLabel.FindProperty("m_VerticalAlignment").intValue = 512;
            serializedLabel.ApplyModifiedPropertiesWithoutUndo();
            return label;
        }

        static void AssignObjectArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            property.ClearArray();
            for (var i = 0; i < values.Length; i++)
            {
                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
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

        static void RegisterAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogWarning("[DungeonInn] Could not get Addressables settings.");
                return;
            }

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
            MarkAssetAddressable(settings, group, InnStatusPanelViewPrefabPath, InnStatusPanelViewAddress);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
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

