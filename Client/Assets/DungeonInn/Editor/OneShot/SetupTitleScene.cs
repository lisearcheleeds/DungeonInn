using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonInn.Editor.OneShot
{
    public static class SetupTitleSceneOneShot
    {
        const string TitleScenePath = "Assets/DungeonInn/Runtime/Scene/MainScene/Title.unity";
        const int UiLayer = 5;

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("Title") ?? new GameObject("Title");
            SetLayerRecursively(root, UiLayer);

            var titleScene = GetOrAddComponent(root, ResolveTitleSceneType());
            var lifetimeScope = GetOrAddComponent(root, ResolveTitleLifetimeScopeType());
            var canvasGroup = GetOrAddComponent<CanvasGroup>(root);
            var canvasInitializer = GetOrAddComponent(root, ResolveSceneCanvasInitializerType());
            var transitionAnimator = GetOrAddComponent(root, ResolveTransitionAnimatorType());

            var canvas = ConfigureCanvas(root.transform);
            var titleView = ConfigureTitleContents(canvas.transform);
            SetLayerRecursively(root, UiLayer);

            SetObjectReference(titleScene, "canvasGroup", canvasGroup);
            SetObjectReference(titleScene, "canvasInitializer", canvasInitializer);
            SetObjectReference(titleScene, "sceneTransitionAnimatorManager", transitionAnimator);

            SetObjectReference(lifetimeScope, "titleScene", titleScene);
            SetObjectReference(lifetimeScope, "titleView", titleView);

            var initializer = new SerializedObject(canvasInitializer);
            var canvasList = initializer.FindProperty("sceneCanvasList");
            canvasList.arraySize = 1;
            canvasList.GetArrayElementAtIndex(0).objectReferenceValue = canvas;
            initializer.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonInn] Updated {TitleScenePath}");
        }

        static Canvas ConfigureCanvas(Transform root)
        {
            var canvasObject = GetOrCreateChild(root, "TitleCanvas");
            var canvas = GetOrAddComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.planeDistance = 100f;

            var scaler = GetOrAddComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var rectTransform = canvasObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return canvas;
        }

        static Component ConfigureTitleContents(Transform canvas)
        {
            var background = GetOrCreateChild(canvas, "TitleBackground");
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);

            var backgroundImage = GetOrAddComponent<Image>(background);
            backgroundImage.color = new Color(0.08f, 0.09f, 0.10f, 1f);

            RemoveChildIfExists(background.transform, "TitleText");
            RemoveChildIfExists(background.transform, "StartGameButton");
            RemoveChildIfExists(background.transform, "TitleLayoutRoot");
            RemoveChildIfExists(background.transform, "MenuLayoutRoot");
            RemoveChildIfExists(background.transform, "PanelLayoutRoot");
            var titleView = GetOrAddComponent(background, ResolveTitleViewType());
            return titleView;
        }

        static void ConfigureText(
            Transform parent,
            string name,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            Color color)
        {
            var textObject = GetOrCreateChild(parent, name);
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var label = GetOrAddComponent(textObject, ResolveTextMeshProType());
            var serializedLabel = new SerializedObject(label);
            serializedLabel.FindProperty("m_text").stringValue = text;
            serializedLabel.FindProperty("m_fontSize").floatValue = fontSize;
            serializedLabel.FindProperty("m_fontColor").colorValue = color;
            serializedLabel.FindProperty("m_RaycastTarget").boolValue = false;
            serializedLabel.FindProperty("m_HorizontalAlignment").intValue = 2;
            serializedLabel.FindProperty("m_VerticalAlignment").intValue = 512;
            serializedLabel.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        static void RemoveChildIfExists(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            for (var i = 0; i < target.transform.childCount; i++)
            {
                SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
            }
        }

        static Component GetOrAddComponent(GameObject target, Type type)
        {
            var component = target.GetComponent(type);
            return component != null ? component : target.AddComponent(type);
        }

        static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        static Type ResolveSceneCanvasInitializerType()
        {
            return ResolveType(
                "Lighthouse.Scene.SceneCamera.SceneCanvasInitializer, Lighthouse.Runtime",
                "SceneCanvasInitializer type was not found.");
        }

        static Type ResolveTitleSceneType()
        {
            return ResolveType(
                "DungeonInn.View.Scene.MainScene.Title.TitleScene, DungeonInn.Runtime",
                "TitleScene type was not found.");
        }

        static Type ResolveTitleLifetimeScopeType()
        {
            return ResolveType(
                "DungeonInn.View.Scene.MainScene.Title.TitleLifetimeScope, DungeonInn.Runtime",
                "TitleLifetimeScope type was not found.");
        }

        static Type ResolveTitleViewType()
        {
            return ResolveType(
                "DungeonInn.View.Scene.MainScene.Title.TitleView, DungeonInn.Runtime",
                "TitleView type was not found.");
        }

        static Type ResolveTransitionAnimatorType()
        {
            return ResolveType(
                "LighthouseExtends.Animation.LHSceneTransitionAnimatorManager, LighthouseExtends.Animation.Runtime",
                "LHSceneTransitionAnimatorManager type was not found.");
        }

        static Type ResolveTextMeshProType()
        {
            return ResolveType(
                "TMPro.TextMeshProUGUI, Unity.TextMeshPro",
                "TextMeshProUGUI type was not found.");
        }

        static Type ResolveType(string typeName, string errorMessage)
        {
            var type = Type.GetType(typeName);
            if (type == null)
            {
                throw new InvalidOperationException(errorMessage);
            }

            return type;
        }

        static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
