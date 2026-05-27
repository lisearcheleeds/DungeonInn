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
                CreateText(bodyRoot, "Dungeon information is unavailable until the world is initialized.", 20, TextAlignmentOptions.Center);
                return;
            }

            CreateColumnHeader(bodyRoot);
            for (var index = 0; index < viewData.Layers.Count; index++)
            {
                CreateLayerRow(bodyRoot, viewData.Layers[index], index);
            }
        }

        static void CreateColumnHeader(RectTransform bodyRoot)
        {
            var header = CreateText(bodyRoot, "Layer", 18, TextAlignmentOptions.Left);
            var rectTransform = header.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, 32f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            header.color = new Color(0.72f, 0.78f, 0.82f, 1f);
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
            rectTransform.anchoredPosition = new Vector2(0f, -40f - index * 76f);
            rectTransform.sizeDelta = new Vector2(0f, 64f);
            rectTransform.pivot = new Vector2(0.5f, 1f);

            var image = rowObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.16f, 0.94f);

            CreateRowText(rowObject.transform, layer.Title, 24, 16f, 0.54f, TextAlignmentOptions.Left);
            CreateRowText(rowObject.transform, layer.Status, 18, 260f, 0.53f, TextAlignmentOptions.Left)
                .color = new Color(0.8f, 0.88f, 0.82f, 1f);
            CreateRowText(rowObject.transform, layer.ActorCount, 18, 520f, 0.53f, TextAlignmentOptions.Left)
                .color = new Color(0.78f, 0.82f, 0.86f, 1f);
            CreateRowText(rowObject.transform, "Hover for spawns and drops", 16, 910f, 0.53f, TextAlignmentOptions.Right)
                .color = new Color(0.58f, 0.64f, 0.7f, 1f);

            rowObject.GetComponent<DungeonLayerListItemView>().Initialize(layer.Popup);
        }

        static TMP_Text CreateRowText(
            Transform parent,
            string text,
            int fontSize,
            float left,
            float anchorY,
            TextAlignmentOptions alignment)
        {
            var textComponent = CreateText(parent, text, fontSize, alignment);
            var rectTransform = textComponent.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, anchorY);
            rectTransform.anchorMax = new Vector2(1f, anchorY);
            rectTransform.offsetMin = new Vector2(left, -18f);
            rectTransform.offsetMax = new Vector2(-16f, 18f);
            return textComponent;
        }

        static TMP_Text CreateText(
            Transform parent,
            string text,
            int fontSize,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var textComponent = textObject.GetComponent<TMP_Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = alignment;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            return textComponent;
        }
    }
}
