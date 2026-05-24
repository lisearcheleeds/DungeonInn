using System;
using DungeonInn.View.Scene.ModuleScene.GameHUD;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonInn.Editor.OneShot
{
    public static class SetupPlayerEventLogViewPrefabOneShot
    {
        const string GameHUDPrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/GameHUD";
        const string PrefabPath = GameHUDPrefabDirectory + "/PlayerEventLogView.prefab";
        const int LineCount = 10;
        const float LineHeight = 18f;
        const float PanelWidth = 420f;
        const float PanelPadding = 8f;

        public static void Run()
        {
            EnsureDirectory(GameHUDPrefabDirectory);
            var prefabExists = AssetDatabase.AssetPathExists(PrefabPath);
            var root = prefabExists
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject("PlayerEventLogView", typeof(RectTransform));

            ConfigurePrefab(root);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

            if (prefabExists)
                PrefabUtility.UnloadPrefabContents(root);
            else
                Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonInn] Updated {PrefabPath}");
        }

        static void ConfigurePrefab(GameObject root)
        {
            root.name = "PlayerEventLogView";
            var rootRect = root.GetComponent<RectTransform>() ?? root.AddComponent<RectTransform>();

            while (0 < rootRect.childCount)
                Object.DestroyImmediate(rootRect.GetChild(0).gameObject);

            var panelHeight = LineCount * LineHeight + PanelPadding * 2f;
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0f, 0f);
            rootRect.anchoredPosition = new Vector2(8f, 8f);
            rootRect.sizeDelta = new Vector2(PanelWidth, panelHeight);

            var panel = CreatePanel(rootRect);

            var textType = ResolveTextMeshProType();
            var logLineRefs = new Component[LineCount];
            for (var i = 0; i < LineCount; i++)
            {
                var y = PanelPadding + (LineCount - 1 - i) * LineHeight;
                logLineRefs[i] = CreateLogLine(panel, textType, i, y);
            }

            var view = root.GetComponent<PlayerEventLogView>() ?? root.AddComponent<PlayerEventLogView>();
            var so = new SerializedObject(view);
            var linesProp = so.FindProperty("logLines");
            linesProp.arraySize = LineCount;
            for (var i = 0; i < LineCount; i++)
                linesProp.GetArrayElementAtIndex(i).objectReferenceValue = logLineRefs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static RectTransform CreatePanel(RectTransform parent)
        {
            var obj = new GameObject("Panel", typeof(RectTransform));
            var rt = obj.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.45f);
            img.raycastTarget = false;
            return rt;
        }

        static Component CreateLogLine(RectTransform parent, Type textType, int index, float yFromBottom)
        {
            var obj = new GameObject($"LogLine{index}", typeof(RectTransform));
            var rt = obj.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(PanelPadding, yFromBottom);
            rt.sizeDelta = new Vector2(-PanelPadding * 2f, LineHeight);

            var label = (Component)obj.AddComponent(textType);
            var so = new SerializedObject(label);
            so.FindProperty("m_text").stringValue = string.Empty;
            so.FindProperty("m_fontSize").floatValue = 13f;
            so.FindProperty("m_fontColor").colorValue = new Color(0.92f, 0.92f, 0.85f, 1f);
            so.FindProperty("m_RaycastTarget").boolValue = false;
            so.FindProperty("m_HorizontalAlignment").intValue = 1;
            so.FindProperty("m_VerticalAlignment").intValue = 512;
            so.ApplyModifiedPropertiesWithoutUndo();
            return label;
        }

        static Type ResolveTextMeshProType()
        {
            var t = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (t == null)
                throw new InvalidOperationException("TextMeshProUGUI type was not found.");
            return t;
        }

        static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
