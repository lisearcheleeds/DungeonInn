using System;
using System.Collections.Generic;
using DungeonInn.Application.NewGame;
using DungeonInn.Application.SaveLoad;
using LighthouseExtends.UIComponent.Button;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.MainScene.Title
{
    public sealed class TitleView : MonoBehaviour
    {
        const float ButtonWidth = 260f;
        const float ButtonHeight = 48f;
        const float ButtonSpacing = 56f;

        RectTransform root;
        RectTransform titleRoot;
        RectTransform menuRoot;
        RectTransform panelRoot;
        LHButton newGameButton;
        LHButton continueButton;
        LHButton loadGameButton;
        LHButton optionButton;
        LHButton exitButton;
        GameObject seedPanel;
        TMP_InputField seedInputField;
        LHButton seedStartButton;
        LHButton seedCancelButton;
        GameObject slotPanel;
        readonly List<LHButton> slotButtons = new();
        LHButton slotCancelButton;
        bool continueAvailable;

        public string SeedText => seedInputField == null ? string.Empty : seedInputField.text;

        public void SetMenuListeners(
            Action onNewGame,
            Action onContinue,
            Action onLoadGame,
            Action onExit)
        {
            EnsureBuilt();
            SetListener(newGameButton, onNewGame);
            SetListener(continueButton, onContinue);
            SetListener(loadGameButton, onLoadGame);
            SetListener(exitButton, onExit);
        }

        public void SetSeedPanelListeners(Action onStart, Action onCancel)
        {
            EnsureBuilt();
            SetListener(seedStartButton, onStart);
            SetListener(seedCancelButton, onCancel);
        }

        public void SetContinueInteractable(bool interactable)
        {
            EnsureBuilt();
            continueAvailable = interactable;
            if (continueButton != null)
            {
                continueButton.interactable = interactable;
            }
        }

        public void SetMenuInteractable(bool interactable)
        {
            EnsureBuilt();
            SetInteractable(newGameButton, interactable);
            SetInteractable(continueButton, interactable && continueAvailable);
            SetInteractable(loadGameButton, interactable);
            SetInteractable(exitButton, interactable);
        }

        public void ShowSeedPanel()
        {
            EnsureBuilt();
            seedInputField.text = string.Empty;
            seedInputField.placeholder.GetComponent<TMP_Text>().text = NewGameSeedParser.DefaultSeedText;
            seedPanel.SetActive(true);
            slotPanel.SetActive(false);
        }

        public void HideSeedPanel()
        {
            EnsureBuilt();
            seedPanel.SetActive(false);
        }

        public void ShowSlotPanel(
            IReadOnlyList<GameSaveSlotSummary> summaries,
            int? activeSlotId,
            Action<int> onSlotSelected)
        {
            EnsureBuilt();
            seedPanel.SetActive(false);
            slotPanel.SetActive(true);
            for (var index = 0; index < slotButtons.Count; index++)
            {
                var slotButton = slotButtons[index];
                var summary = summaries[index];
                slotButton.interactable = !summary.IsEmpty;
                SetButtonLabel(slotButton, CreateSlotLabel(summary, activeSlotId));
                var slotId = summary.SlotId;
                SetListener(slotButton, () => onSlotSelected?.Invoke(slotId));
            }
        }

        public void HideSlotPanel()
        {
            EnsureBuilt();
            slotPanel.SetActive(false);
        }

        public void SetSlotCancelListener(Action onCancel)
        {
            EnsureBuilt();
            SetListener(slotCancelButton, onCancel);
        }

        void Awake()
        {
            EnsureBuilt();
        }

        void EnsureBuilt()
        {
            if (root != null)
            {
                return;
            }

            root = gameObject.GetComponent<RectTransform>();
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
            }

            RemoveGeneratedLayoutRoots();
            BuildLayoutRoots();
            BuildTitle();
            BuildMenu();
            BuildSeedPanel();
            BuildSlotPanel();
        }

        void BuildTitle()
        {
            var title = CreateText(titleRoot, "Dungeon Inn", 56, TextAlignmentOptions.Center);
            var rectTransform = title.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        void BuildMenu()
        {
            newGameButton = CreateButton(menuRoot, "NewGame", new Vector2(0f, 112f));
            continueButton = CreateButton(menuRoot, "Continue", new Vector2(0f, 112f - ButtonSpacing));
            loadGameButton = CreateButton(menuRoot, "LoadGame", new Vector2(0f, 112f - ButtonSpacing * 2f));
            optionButton = CreateButton(menuRoot, "Option", new Vector2(0f, 112f - ButtonSpacing * 3f));
            exitButton = CreateButton(menuRoot, "Exit", new Vector2(0f, 112f - ButtonSpacing * 4f));
            optionButton.interactable = true;
        }

        void BuildSeedPanel()
        {
            seedPanel = CreatePanel(panelRoot, "SeedPanel", new Vector2(400f, 0f), new Vector2(320f, 210f));
            CreatePanelTitle(seedPanel.transform, "New Game Seed");
            seedInputField = CreateInputField(seedPanel.transform, new Vector2(0f, 28f));
            seedStartButton = CreateButton(seedPanel.transform, "Start", new Vector2(-74f, -62f), 132f, 42f);
            seedCancelButton = CreateButton(seedPanel.transform, "Cancel", new Vector2(74f, -62f), 132f, 42f);
            seedPanel.SetActive(false);
        }

        void BuildSlotPanel()
        {
            slotPanel = CreatePanel(panelRoot, "LoadSlotPanel", new Vector2(410f, 0f), new Vector2(360f, 320f));
            CreatePanelTitle(slotPanel.transform, "Load Game");
            for (var index = 0; index < 3; index++)
            {
                slotButtons.Add(CreateButton(
                    slotPanel.transform,
                    $"Slot {index + 1}",
                    new Vector2(0f, 70f - index * 62f),
                    300f,
                    48f));
            }

            slotCancelButton = CreateButton(slotPanel.transform, "Cancel", new Vector2(0f, -124f), 160f, 42f);
            slotPanel.SetActive(false);
        }

        void BuildLayoutRoots()
        {
            titleRoot = CreateLayoutRoot("TitleLayoutRoot");
            titleRoot.anchorMin = new Vector2(0f, 1f);
            titleRoot.anchorMax = new Vector2(1f, 1f);
            titleRoot.pivot = new Vector2(0.5f, 1f);
            titleRoot.offsetMin = new Vector2(0f, -150f);
            titleRoot.offsetMax = new Vector2(0f, -40f);

            menuRoot = CreateLayoutRoot("MenuLayoutRoot");
            menuRoot.anchorMin = new Vector2(0.5f, 0.5f);
            menuRoot.anchorMax = new Vector2(0.5f, 0.5f);
            menuRoot.pivot = new Vector2(0.5f, 0.5f);
            menuRoot.anchoredPosition = new Vector2(0f, -30f);
            menuRoot.sizeDelta = new Vector2(320f, 360f);

            panelRoot = CreateLayoutRoot("PanelLayoutRoot");
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.anchoredPosition = Vector2.zero;
            panelRoot.sizeDelta = new Vector2(1280f, 720f);
        }

        void RemoveGeneratedLayoutRoots()
        {
            RemoveChildIfExists("TitleLayoutRoot");
            RemoveChildIfExists("MenuLayoutRoot");
            RemoveChildIfExists("PanelLayoutRoot");
        }

        void RemoveChildIfExists(string objectName)
        {
            var existing = root.Find(objectName);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
        }

        RectTransform CreateLayoutRoot(string objectName)
        {
            var rootObject = new GameObject(objectName, typeof(RectTransform));
            rootObject.transform.SetParent(root, false);
            return rootObject.GetComponent<RectTransform>();
        }

        static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            var rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            panelObject.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.1f, 0.95f);
            return panelObject;
        }

        static void CreatePanelTitle(Transform parent, string text)
        {
            var title = CreateText(parent, text, 24, TextAlignmentOptions.Center);
            var rectTransform = title.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -18f);
            rectTransform.sizeDelta = new Vector2(280f, 36f);
        }

        static TMP_InputField CreateInputField(Transform parent, Vector2 position)
        {
            var inputObject = new GameObject(
                "SeedInput",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(TMP_InputField));
            inputObject.transform.SetParent(parent, false);
            var rectTransform = inputObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(240f, 42f);
            inputObject.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.22f, 1f);

            var text = CreateText(inputObject.transform, string.Empty, 22, TextAlignmentOptions.Left);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);

            var placeholder = CreateText(inputObject.transform, NewGameSeedParser.DefaultSeedText, 22, TextAlignmentOptions.Left);
            var placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(12f, 4f);
            placeholderRect.offsetMax = new Vector2(-12f, -4f);
            placeholder.color = new Color(0.6f, 0.64f, 0.68f, 1f);

            var inputField = inputObject.GetComponent<TMP_InputField>();
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            return inputField;
        }

        static LHButton CreateButton(Transform parent, string label, Vector2 position)
        {
            return CreateButton(parent, label, position, ButtonWidth, ButtonHeight);
        }

        static LHButton CreateButton(Transform parent, string label, Vector2 position, float width, float height)
        {
            var buttonObject = new GameObject(
                label + "Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LHButton));
            buttonObject.transform.SetParent(parent, false);
            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(width, height);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.2f, 1f);

            var button = buttonObject.GetComponent<LHButton>();
            button.targetGraphic = image;
            var labelText = CreateText(buttonObject.transform, label, 22, TextAlignmentOptions.Center);
            var textRect = labelText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        static TMP_Text CreateText(Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
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

        static void SetListener(LHButton button, Action listener)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (listener != null)
            {
                button.onClick.AddListener(() => listener.Invoke());
            }
        }

        static void SetInteractable(LHButton button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        static void SetButtonLabel(LHButton button, string label)
        {
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        static string CreateSlotLabel(GameSaveSlotSummary summary, int? activeSlotId)
        {
            var activeMarker = activeSlotId.HasValue && activeSlotId.Value == summary.SlotId ? "* " : string.Empty;
            if (summary.IsEmpty)
            {
                return $"{activeMarker}Slot {summary.SlotId}: Empty";
            }

            return $"{activeMarker}Slot {summary.SlotId}: Day {summary.DisplayDay} Seed {summary.Seed}";
        }
    }
}
