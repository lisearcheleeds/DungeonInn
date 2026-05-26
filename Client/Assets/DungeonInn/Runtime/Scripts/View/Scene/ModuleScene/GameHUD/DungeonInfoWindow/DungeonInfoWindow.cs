using LighthouseExtends.ScreenStack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class DungeonInfoWindow :
        GameHudScreenStackWindowBase,
        IScreenStackSetup<DungeonInfoWindowData>
    {
        public void Setup(DungeonInfoWindowData screenStackData)
        {
            ApplyContent("Dungeon Info", string.Empty);
            SetBodyTextVisible(false);
            RenderLayerList(screenStackData.ViewData);
            Debug.Log($"[GameHUD.ScreenStack] DungeonInfoWindow displayed. Layers={screenStackData.ViewData.Layers.Count}");
        }

        void RenderLayerList(DungeonInfoWindowViewData viewData)
        {
            var bodyRoot = GetBodyRoot();
            for (var index = bodyRoot.childCount - 1; 0 <= index; index--)
            {
                Destroy(bodyRoot.GetChild(index).gameObject);
            }

            if (viewData.Layers.Count == 0)
            {
                CreateTextRow(bodyRoot, "Dungeon information is unavailable until the world is initialized.", 0);
                return;
            }

            for (var index = 0; index < viewData.Layers.Count; index++)
            {
                CreateLayerRow(bodyRoot, viewData.Layers[index], index);
            }
        }

        static void CreateLayerRow(RectTransform bodyRoot, DungeonLayerListItemViewData layer, int index)
        {
            var rowObject = new GameObject(
                $"LayerRow{index + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(DungeonLayerListItemView));
            rowObject.transform.SetParent(bodyRoot, false);

            var rectTransform = rowObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -index * 56f);
            rectTransform.sizeDelta = new Vector2(0f, 48f);
            rectTransform.pivot = new Vector2(0.5f, 1f);

            var image = rowObject.GetComponent<Image>();
            image.color = new Color(0.14f, 0.16f, 0.18f, 0.92f);

            var text = CreateText(rowObject.transform, $"{layer.Title} - {layer.Status}\n{layer.ActorCount}");
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);

            rowObject.GetComponent<DungeonLayerListItemView>().Initialize(layer.Popup);
        }

        static void CreateTextRow(RectTransform bodyRoot, string text, int index)
        {
            var textComponent = CreateText(bodyRoot, text);
            var rectTransform = textComponent.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -index * 40f);
            rectTransform.sizeDelta = new Vector2(0f, 36f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
        }

        static TMP_Text CreateText(Transform parent, string text)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var textComponent = textObject.GetComponent<TMP_Text>();
            textComponent.text = text;
            textComponent.fontSize = 18;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            return textComponent;
        }
    }
}
