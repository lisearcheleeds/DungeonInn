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
    public static class SetupMinimapViewPrefabOneShot
    {
        const string GameUIPrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/GameUI";
        const string MinimapViewPrefabPath = GameUIPrefabDirectory + "/MinimapView.prefab";
        const string AddressablesGroupName = "DungeonInn Visual";
        const string MinimapViewAddress = "GameUI/MinimapView";

        public static void Run()
        {
            CreateOrLoadMinimapViewPrefab();
            RegisterAddressable();
        }

        static MinimapView CreateOrLoadMinimapViewPrefab()
        {
            EnsureDirectory(GameUIPrefabDirectory);
            var prefabExists = AssetDatabase.AssetPathExists(MinimapViewPrefabPath);
            var minimapObject = prefabExists
                ? PrefabUtility.LoadPrefabContents(MinimapViewPrefabPath)
                : new GameObject("MinimapView", typeof(RectTransform));

            ConfigureMinimapViewPrefab(minimapObject);
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(minimapObject, MinimapViewPrefabPath);

            if (prefabExists)
            {
                PrefabUtility.UnloadPrefabContents(minimapObject);
            }
            else
            {
                Object.DestroyImmediate(minimapObject);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DungeonInn] Updated {MinimapViewPrefabPath}");
            return savedPrefab != null ? savedPrefab.GetComponent<MinimapView>() : null;
        }

        static void ConfigureMinimapViewPrefab(GameObject minimapObject)
        {
            minimapObject.name = "MinimapView";
            var rootRect = minimapObject.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = minimapObject.AddComponent<RectTransform>();
            }

            while (0 < rootRect.childCount)
            {
                Object.DestroyImmediate(rootRect.GetChild(0).gameObject);
            }

            rootRect.anchorMin = new Vector2(1f, 0f);
            rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(1f, 0f);
            rootRect.anchoredPosition = new Vector2(-16f, 16f);
            rootRect.sizeDelta = new Vector2(200f, 200f);

            var panel = CreatePanel(rootRect);
            var mapRoot = CreateMapRoot(panel);
            var rawImage = CreateMapImage(mapRoot);
            var actorDotRoot = CreateActorDotRoot(mapRoot);
            var actorDotTemplate = CreateActorDotTemplate(actorDotRoot);
            var view = minimapObject.GetComponent<MinimapView>();
            if (view == null)
            {
                view = minimapObject.AddComponent<MinimapView>();
            }

            var serializedView = new SerializedObject(view);
            serializedView.FindProperty("mapRoot").objectReferenceValue = mapRoot;
            serializedView.FindProperty("mapImage").objectReferenceValue = rawImage;
            serializedView.FindProperty("actorDotRoot").objectReferenceValue = actorDotRoot;
            serializedView.FindProperty("actorDotTemplate").objectReferenceValue = actorDotTemplate;
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
            image.color = new Color(0.03f, 0.035f, 0.04f, 0.82f);
            return rectTransform;
        }

        static RectTransform CreateMapRoot(RectTransform parent)
        {
            var rootObject = new GameObject("MapRoot", typeof(RectTransform));
            var rectTransform = rootObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(6f, 6f);
            rectTransform.offsetMax = new Vector2(-6f, -6f);
            return rectTransform;
        }

        static RawImage CreateMapImage(RectTransform parent)
        {
            var imageObject = new GameObject("MapImage", typeof(RectTransform));
            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            var rawImage = imageObject.AddComponent<RawImage>();
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;
            return rawImage;
        }

        static RectTransform CreateActorDotRoot(RectTransform parent)
        {
            var dotRootObject = new GameObject("ActorDots", typeof(RectTransform));
            var rectTransform = dotRootObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return rectTransform;
        }

        static Image CreateActorDotTemplate(RectTransform parent)
        {
            var dotObject = new GameObject("ActorDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rectTransform = dotObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(8f, 8f);

            var image = dotObject.GetComponent<Image>();
            image.color = Color.blue;
            image.raycastTarget = false;
            dotObject.SetActive(false);
            return image;
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
            MarkAssetAddressable(settings, group, MinimapViewPrefabPath, MinimapViewAddress);
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

