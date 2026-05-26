using DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonInn.Editor.OneShot
{
    public static class SetupGameHudScreenStackPrefabsOneShot
    {
        const string PrefabDirectory = "Assets/DungeonInn/Runtime/Prefab/GameHUD/ScreenStack";

        public static void Run()
        {
            EnsureDirectory(PrefabDirectory);
            CreateOrUpdateWindowPrefab<DungeonInfoWindow>("DungeonInfoWindow");
            CreateOrUpdateWindowPrefab<GuildManagementWindow>("GuildManagementWindow");
            CreateOrUpdateWindowPrefab<MarketWindow>("MarketWindow");
            VisualAssetSetup.RunAddressablesSetup();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DungeonInn] GameHUD ScreenStack prefabs updated.");
        }

        static void CreateOrUpdateWindowPrefab<TWindow>(string prefabName)
            where TWindow : GameHudScreenStackWindowBase
        {
            var prefabPath = $"{PrefabDirectory}/{prefabName}.prefab";
            var windowObject = new GameObject(
                prefabName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(TWindow));

            var rectTransform = windowObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(720f, 520f);

            var image = windowObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.1f, 0.96f);

            PrefabUtility.SaveAsPrefabAsset(windowObject, prefabPath);
            Object.DestroyImmediate(windowObject);
            Debug.Log($"[DungeonInn] Updated {prefabPath}");
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
