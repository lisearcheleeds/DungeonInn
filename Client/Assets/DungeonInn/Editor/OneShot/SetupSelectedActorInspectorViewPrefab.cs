using DungeonInn.View.Scene.ModuleScene.GameUI;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonInn.Editor.OneShot
{
    public static class SetupSelectedActorInspectorViewPrefab
    {
        const string GameUIPrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/GameUI";
        const string SelectedActorInspectorViewPrefabPath =
            GameUIPrefabDirectory + "/SelectedActorInspectorView.prefab";
        const int StatRowCount = 6;
        const int EquipmentRowCount = 3;
        const int InventoryRowCount = 12;
        const int EffectRowCount = 8;
        static readonly Color PanelColor = new(0.05f, 0.06f, 0.07f, 0.86f);
        static readonly Color GaugeBackgroundColor = new(0.10f, 0.11f, 0.12f, 1f);
        static readonly Color HeaderTextColor = new(0.95f, 0.94f, 0.88f, 1f);
        static readonly Color BodyTextColor = new(0.82f, 0.84f, 0.84f, 1f);
        static readonly Color MutedTextColor = new(0.57f, 0.60f, 0.61f, 1f);
        static readonly Color GaugeTextColor = new(0.94f, 0.94f, 0.90f, 1f);

        public static void Run()
        {
            CreateOrLoadSelectedActorInspectorViewPrefab();
        }

        static SelectedActorInspectorView CreateOrLoadSelectedActorInspectorViewPrefab()
        {
            EnsureDirectory(GameUIPrefabDirectory);
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

        static void ConfigureSelectedActorInspectorViewPrefab(GameObject viewObject)
        {
            viewObject.name = "SelectedActorInspectorView";
            var rootRect = viewObject.GetComponent<RectTransform>() ?? viewObject.AddComponent<RectTransform>();
            ClearChildren(rootRect);
            ConfigureRoot(viewObject, rootRect);
            var view = viewObject.GetComponent<SelectedActorInspectorView>()
                ?? viewObject.AddComponent<SelectedActorInspectorView>();

            var titleText = CreateText("Title", rootRect, 20, HeaderTextColor, 1);
            var roleText = CreateText("Role", rootRect, 12, MutedTextColor, 0);
            var levelText = CreateText("Level", rootRect, 13, BodyTextColor, 0);
            var hpGauge = CreateGauge(rootRect, "HPGauge");
            var mpGauge = CreateGauge(rootRect, "MPGauge");
            var locationText = CreateText("Location", rootRect, 12, BodyTextColor, 0);
            var goalText = CreateText("Goal", rootRect, 12, BodyTextColor, 0);
            var conditionText = CreateText("Condition", rootRect, 12, BodyTextColor, 0);
            var goldText = CreateText("Gold", rootRect, 12, BodyTextColor, 0);
            var statRows = CreateSection(rootRect, "Stats", StatRowCount, out var statTexts);
            var equipmentRows = CreateSection(rootRect, "Equipment", EquipmentRowCount, out var equipmentTexts);
            var inventoryRows = CreateSection(rootRect, "Items", InventoryRowCount, out var inventoryTexts);
            var effectRows = CreateSection(rootRect, "Effects", EffectRowCount, out var effectTexts);

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
            EditorUtility.SetDirty(view);
        }

        static void ConfigureRoot(GameObject viewObject, RectTransform rootRect)
        {
            rootRect.sizeDelta = new Vector2(360f, 640f);
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(16f, -16f);

            var image = viewObject.GetComponent<Image>() ?? viewObject.AddComponent<Image>();
            image.color = PanelColor;

            var fitter = viewObject.GetComponent<ContentSizeFitter>() ?? viewObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var group = viewObject.GetComponent<VerticalLayoutGroup>() ?? viewObject.AddComponent<VerticalLayoutGroup>();
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.spacing = 8f;
            group.padding = new RectOffset(14, 14, 12, 12);
        }

        static RectTransform CreateVerticalGroup(
            string objectName,
            Transform parent,
            float horizontalPadding,
            float verticalPadding,
            float spacing)
        {
            var rectTransform = CreateRectTransform(objectName, parent);
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

        static GameObject[] CreateSection(Transform parent, string title, int rowCount, out Component[] rowTexts)
        {
            var section = CreateVerticalGroup(title, parent, 0f, 0f, 3f);
            SetText(CreateText($"{title}Header", section, 12, HeaderTextColor, 1), title);
            var rows = new GameObject[rowCount];
            rowTexts = new Component[rowCount];
            for (var index = 0; index < rowCount; index++)
            {
                var rowText = CreateText($"{title}Row{index + 1:00}", section, 12, BodyTextColor, 0);
                SetText(rowText, string.Empty);
                rows[index] = rowText.gameObject;
                rowTexts[index] = rowText;
            }

            return rows;
        }

        static ValueGaugeView CreateGauge(Transform parent, string objectName)
        {
            var gaugeRoot = CreateRectTransform(objectName, parent);
            var layout = gaugeRoot.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 20f;
            layout.preferredHeight = 20f;
            var background = CreateGaugeLayer("Background", gaugeRoot, false);
            background.color = GaugeBackgroundColor;
            var fill = CreateGaugeLayer("Fill", gaugeRoot, true);
            var valueText = CreateGaugeText("Value", gaugeRoot);
            var gauge = gaugeRoot.gameObject.AddComponent<ValueGaugeView>();
            using var serializedGauge = new SerializedObject(gauge);
            serializedGauge.FindProperty("fillImage").objectReferenceValue = fill;
            serializedGauge.FindProperty("valueText").objectReferenceValue = valueText;
            serializedGauge.ApplyModifiedPropertiesWithoutUndo();
            return gauge;
        }

        static Image CreateGaugeLayer(string objectName, Transform parent, bool filled)
        {
            var rectTransform = CreateRectTransform(objectName, parent);
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

        static Component CreateGaugeText(string objectName, Transform parent)
        {
            var text = CreateText(objectName, parent, 12, GaugeTextColor, 0);
            var rectTransform = (RectTransform)text.transform;
            Stretch(rectTransform);
            SetTextAlignment(text, 2, 512);
            return text;
        }

        static Component CreateText(string objectName, Transform parent, int fontSize, Color color, int fontStyle)
        {
            var rectTransform = CreateRectTransform(objectName, parent);
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

        static RectTransform CreateRectTransform(string objectName, Transform parent)
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

        static void ClearChildren(RectTransform rootRect)
        {
            while (0 < rootRect.childCount)
            {
                Object.DestroyImmediate(rootRect.GetChild(0).gameObject);
            }
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

