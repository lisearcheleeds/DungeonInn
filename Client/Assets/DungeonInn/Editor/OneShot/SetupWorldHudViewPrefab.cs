using System;
using DungeonInn.View.Scene.ModuleScene.GameUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonInn.Editor.OneShot
{
    public static class SetupWorldHudViewPrefabOneShot
    {
        const string GameUIPrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/GameUI";
        const string WorldHudViewPrefabPath = GameUIPrefabDirectory + "/WorldHudView.prefab";

        public static void Run()
        {
            CreateOrLoadWorldHudViewPrefab();
            VisualAssetSetup.RunAddressablesSetup();
        }

        static WorldHudView CreateOrLoadWorldHudViewPrefab()
        {
            EnsureDirectory(GameUIPrefabDirectory);
            var prefabExists = AssetDatabase.AssetPathExists(WorldHudViewPrefabPath);
            var hudObject = prefabExists
                ? PrefabUtility.LoadPrefabContents(WorldHudViewPrefabPath)
                : new GameObject("WorldHudView", typeof(RectTransform));

            ConfigureWorldHudViewPrefab(hudObject);
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(hudObject, WorldHudViewPrefabPath);

            if (prefabExists)
            {
                PrefabUtility.UnloadPrefabContents(hudObject);
            }
            else
            {
                Object.DestroyImmediate(hudObject);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonInn] Updated {WorldHudViewPrefabPath}");
            return savedPrefab != null ? savedPrefab.GetComponent<WorldHudView>() : null;
        }

        static void ConfigureWorldHudViewPrefab(GameObject hudObject)
        {
            hudObject.name = "WorldHudView";
            var rootRect = hudObject.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = hudObject.AddComponent<RectTransform>();
            }

            while (0 < rootRect.childCount)
            {
                Object.DestroyImmediate(rootRect.GetChild(0).gameObject);
            }

            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(0f, 48f);

            var panel = CreatePanel(rootRect);
            var dayTimeText = CreateText(
                panel,
                "DayTimeText",
                "Day 1  00:00",
                new Vector2(16f, -9f),
                new Vector2(160f, 30f),
                18,
                new Color(0.95f, 0.95f, 0.95f, 1f));
            var goldText = CreateText(
                panel,
                "GoldText",
                "Gold: 0",
                new Vector2(190f, -9f),
                new Vector2(150f, 30f),
                18,
                new Color(1f, 0.88f, 0.42f, 1f));
            var alertText = CreateText(
                panel,
                "AlertText",
                string.Empty,
                new Vector2(580f, -10f),
                new Vector2(300f, 28f),
                16,
                new Color(1f, 0.7f, 0.52f, 1f));
            alertText.gameObject.SetActive(false);

            var pauseButton = CreateButton(panel, "PauseButton", "Pause", new Vector2(360f, -8f), new Vector2(68f, 30f));
            var normalButton = CreateButton(panel, "SpeedNormalButton", "1x", new Vector2(436f, -8f), new Vector2(52f, 30f));
            var fastButton = CreateButton(panel, "SpeedFastButton", "2x", new Vector2(496f, -8f), new Vector2(52f, 30f));

            var view = hudObject.GetComponent<WorldHudView>();
            if (view == null)
            {
                view = hudObject.AddComponent<WorldHudView>();
            }

            var serializedView = new SerializedObject(view);
            serializedView.FindProperty("dayTimeText").objectReferenceValue = dayTimeText;
            serializedView.FindProperty("goldText").objectReferenceValue = goldText;
            serializedView.FindProperty("alertText").objectReferenceValue = alertText;
            serializedView.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            serializedView.FindProperty("speedNormalButton").objectReferenceValue = normalButton;
            serializedView.FindProperty("speedFastButton").objectReferenceValue = fastButton;
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
            image.color = new Color(0.04f, 0.05f, 0.06f, 0.82f);
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

        static Component CreateButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var buttonType = ResolveLHButtonType();
            var buttonObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.23f, 0.29f, 0.95f);
            var button = (Component)buttonObject.AddComponent(buttonType);
            var serializedButton = new SerializedObject(button);
            serializedButton.FindProperty("m_TargetGraphic").objectReferenceValue = image;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();

            CreateText(
                rectTransform,
                "Label",
                label,
                new Vector2(0f, 0f),
                size,
                14,
                new Color(0.95f, 0.95f, 0.95f, 1f));
            return button;
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

        static Type ResolveLHButtonType()
        {
            var buttonType = Type.GetType(
                "LighthouseExtends.UIComponent.Button.LHButton, LighthouseExtends.UIComponent.Runtime");
            if (buttonType == null)
            {
                throw new InvalidOperationException("LHButton type was not found.");
            }

            return buttonType;
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

