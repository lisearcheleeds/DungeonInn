using System;
using LighthouseExtends.ScreenStack;
using LighthouseExtends.UIComponent.Button;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack
{
    public sealed class SystemMenuWindow :
        GameUIScreenStackWindowBase,
        IScreenStackSetup<SystemMenuWindowData>
    {
        SystemMenuWindowData screenStackData;
        LHButton saveButton;
        LHButton loadButton;
        LHButton optionButton;
        LHButton titleButton;

        public void Setup(SystemMenuWindowData screenStackData)
        {
            this.screenStackData = screenStackData ?? throw new ArgumentNullException(nameof(screenStackData));
            ApplyContent("System Menu", string.Empty);
            SetBodyTextVisible(false);
            Render();
        }

        void Render()
        {
            var bodyRoot = GetBodyRoot();
            ClearChildren(bodyRoot);

            saveButton = CreateMenuButton(bodyRoot, "Save", new Vector2(0f, -20f), screenStackData.Save);
            loadButton = CreateMenuButton(bodyRoot, "Load", new Vector2(0f, -86f), screenStackData.Load);
            optionButton = CreateMenuButton(bodyRoot, "Option", new Vector2(0f, -152f), null);
            titleButton = CreateMenuButton(bodyRoot, "Title", new Vector2(0f, -218f), screenStackData.Title);
            optionButton.interactable = true;

            Debug.Log("[GameUI.ScreenStack] SystemMenuWindow displayed.");
        }

        LHButton CreateMenuButton(RectTransform parent, string label, Vector2 position, Action action)
        {
            var buttonObject = new GameObject(
                label + "Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LHButton));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280f, 52f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.2f, 1f);

            var button = buttonObject.GetComponent<LHButton>();
            button.targetGraphic = image;
            if (action != null)
            {
                button.onClick.AddListener(() => action.Invoke());
            }

            var text = CreateText(buttonObject.transform, label, 22, TextAlignmentOptions.Center);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
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
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            return textComponent;
        }

        static void ClearChildren(RectTransform root)
        {
            for (var index = root.childCount - 1; 0 <= index; index--)
            {
                Destroy(root.GetChild(index).gameObject);
            }
        }
    }
}

