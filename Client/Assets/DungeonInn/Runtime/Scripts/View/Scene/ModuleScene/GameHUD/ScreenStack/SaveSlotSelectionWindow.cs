using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.SaveLoad;
using LighthouseExtends.ScreenStack;
using LighthouseExtends.UIComponent.Button;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class SaveSlotSelectionWindow :
        GameHudScreenStackWindowBase,
        IScreenStackSetup<SaveSlotSelectionWindowData>
    {
        SaveSlotSelectionWindowData screenStackData;

        public void Setup(SaveSlotSelectionWindowData screenStackData)
        {
            this.screenStackData = screenStackData ?? throw new ArgumentNullException(nameof(screenStackData));
            ApplyContent(screenStackData.Title, string.Empty);
            SetBodyTextVisible(false);
            RenderSlots();
        }

        void RenderSlots()
        {
            var bodyRoot = GetBodyRoot();
            ClearChildren(bodyRoot);

            for (var index = 0; index < screenStackData.Summaries.Count; index++)
            {
                CreateSlotButton(bodyRoot, screenStackData.Summaries[index], index);
            }

            var cancelButton = CreateButton(bodyRoot, "CancelButton", "Cancel", new Vector2(0f, -280f), new Vector2(180f, 46f));
            cancelButton.onClick.AddListener(() => CloseScreenStackAsync().Forget());
        }

        void CreateSlotButton(RectTransform parent, GameSaveSlotSummary summary, int index)
        {
            var button = CreateButton(
                parent,
                $"Slot{summary.SlotId}Button",
                CreateSlotLabel(summary),
                new Vector2(0f, -24f - index * 72f),
                new Vector2(520f, 56f));
            button.interactable = screenStackData.AllowEmpty || !summary.IsEmpty;
            if (button.interactable)
            {
                button.onClick.AddListener(() =>
                {
                    screenStackData.SelectSlot(summary.SlotId);
                    CloseScreenStackAsync().Forget();
                });
            }
        }

        LHButton CreateButton(
            RectTransform parent,
            string objectName,
            string label,
            Vector2 position,
            Vector2 size)
        {
            var buttonObject = new GameObject(
                objectName,
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
            rect.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.2f, 1f);

            var button = buttonObject.GetComponent<LHButton>();
            button.targetGraphic = image;

            var text = CreateText(buttonObject.transform, label, 20, TextAlignmentOptions.Center);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = new Vector2(-12f, 0f);
            return button;
        }

        string CreateSlotLabel(GameSaveSlotSummary summary)
        {
            var activeMarker = screenStackData.ActiveSlotId.HasValue &&
                screenStackData.ActiveSlotId.Value == summary.SlotId
                    ? "* "
                    : string.Empty;
            if (summary.IsEmpty)
            {
                return $"{activeMarker}Slot {summary.SlotId}: Empty";
            }

            return $"{activeMarker}Slot {summary.SlotId}: Day {summary.DisplayDay} Seed {summary.Seed}";
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
