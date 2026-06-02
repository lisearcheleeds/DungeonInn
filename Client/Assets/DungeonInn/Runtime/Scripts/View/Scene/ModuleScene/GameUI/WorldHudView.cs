using LighthouseExtends.UIComponent.Button;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    public sealed class WorldHudView : MonoBehaviour
    {
        [SerializeField] TMP_Text dayTimeText;
        [SerializeField] TMP_Text goldText;
        [SerializeField] LHButton pauseButton;
        [SerializeField] LHButton speedNormalButton;
        [SerializeField] LHButton speedFastButton;
        [SerializeField] LHButton dungeonInfoButton;
        [SerializeField] LHButton guildManagementButton;
        [SerializeField] LHButton marketButton;

        TMP_Text pauseButtonLabel;
        RectTransform root;
        LHButton settingsButton;

        public void SetDayTime(string text)
        {
            if (dayTimeText != null)
            {
                dayTimeText.text = text;
            }
        }

        public void SetGold(string text)
        {
            if (goldText != null)
            {
                goldText.text = text;
            }
        }

        public void SetPauseButtonLabel(string text)
        {
            EnsurePauseButtonLabel();
            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = text;
            }
        }

        public void AddPauseListener(UnityAction onClick)
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(onClick);
            }
        }

        public void AddSpeedNormalListener(UnityAction onClick)
        {
            if (speedNormalButton != null)
            {
                speedNormalButton.onClick.AddListener(onClick);
            }
        }

        public void AddSpeedFastListener(UnityAction onClick)
        {
            if (speedFastButton != null)
            {
                speedFastButton.onClick.AddListener(onClick);
            }
        }

        public void AddDungeonInfoListener(UnityAction onClick)
        {
            if (dungeonInfoButton != null)
            {
                dungeonInfoButton.onClick.AddListener(onClick);
            }
        }

        public void AddGuildManagementListener(UnityAction onClick)
        {
            if (guildManagementButton != null)
            {
                guildManagementButton.onClick.AddListener(onClick);
            }
        }

        public void AddMarketListener(UnityAction onClick)
        {
            if (marketButton != null)
            {
                marketButton.onClick.AddListener(onClick);
            }
        }

        public void AddSettingsListener(UnityAction onClick)
        {
            EnsureSettingsButton();
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(onClick);
        }

        void Awake()
        {
            EnsurePauseButtonLabel();
            EnsureSettingsButton();
        }

        void EnsurePauseButtonLabel()
        {
            if (pauseButtonLabel != null || pauseButton == null)
            {
                return;
            }

            pauseButtonLabel = pauseButton.GetComponentInChildren<TMP_Text>(true);
        }

        void EnsureSettingsButton()
        {
            if (settingsButton != null)
            {
                return;
            }

            root = gameObject.GetComponent<RectTransform>();
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
            }

            settingsButton = CreateButton(root, "Settings", new Vector2(-92f, -34f), new Vector2(148f, 42f));
            var settingsRect = settingsButton.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(1f, 1f);
            settingsRect.anchorMax = new Vector2(1f, 1f);
        }

        static LHButton CreateButton(Transform parent, string label, Vector2 position, Vector2 size)
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
            rectTransform.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.2f, 1f);

            var button = buttonObject.GetComponent<LHButton>();
            button.targetGraphic = image;
            var text = CreateText(buttonObject.transform, label, 18, TextAlignmentOptions.Center);
            var textRect = text.GetComponent<RectTransform>();
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

    }
}

