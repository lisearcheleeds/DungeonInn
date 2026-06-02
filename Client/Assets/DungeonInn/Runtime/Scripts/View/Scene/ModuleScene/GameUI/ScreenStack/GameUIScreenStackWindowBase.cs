using Cysharp.Threading.Tasks;
using DungeonInn.View.Base;
using LighthouseExtends.UIComponent.Button;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack
{
    public abstract class GameUIScreenStackWindowBase : ProductScreenStackBase
    {
        const float WindowWidth = 1280f;
        const float WindowHeight = 720f;
        const float HeaderHeight = 72f;

        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] LHButton closeButton;

        public override void ResetInAnimation()
        {
        }

        public override void EndInAnimation()
        {
        }

        public override void ResetOutAnimation()
        {
        }

        public override void EndOutAnimation()
        {
        }

        public override UniTask PlayInAnimation()
        {
            return UniTask.CompletedTask;
        }

        public override UniTask PlayOutAnimation()
        {
            return UniTask.CompletedTask;
        }

        protected void ApplyContent(string title, string body)
        {
            EnsureContentView();

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = body;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        protected RectTransform GetBodyRoot()
        {
            EnsureContentView();
            return bodyText.GetComponent<RectTransform>();
        }

        protected void SetBodyTextVisible(bool isVisible)
        {
            EnsureContentView();
            bodyText.enabled = isVisible;
        }

        void Close()
        {
            CloseScreenStackAsync().Forget();
        }

        void EnsureContentView()
        {
            if (titleText != null && bodyText != null && closeButton != null)
            {
                return;
            }

            var rootRect = GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(WindowWidth, WindowHeight);

            var image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = new Color(0.07f, 0.08f, 0.09f, 0.97f);

            titleText = titleText != null
                ? titleText
                : CreateText("HeaderTitle", transform, 28, TextAlignmentOptions.Left);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(32f, -HeaderHeight);
            titleRect.offsetMax = new Vector2(-96f, 0f);
            titleText.fontStyle = FontStyles.Bold;

            bodyText = bodyText != null
                ? bodyText
                : CreateText("Body", transform, 20, TextAlignmentOptions.TopLeft);
            var bodyRect = bodyText.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(32f, 32f);
            bodyRect.offsetMax = new Vector2(-32f, -HeaderHeight - 20f);

            closeButton = closeButton != null
                ? closeButton
                : CreateButton("CloseButton", transform, "X");
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-36f, -36f);
            closeRect.sizeDelta = new Vector2(44f, 44f);
        }

        static TMP_Text CreateText(
            string objectName,
            Transform parent,
            int fontSize,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        static LHButton CreateButton(string objectName, Transform parent, string label)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LHButton));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.2f, 0.24f, 0.28f, 1f);

            var button = buttonObject.GetComponent<LHButton>();
            button.targetGraphic = image;

            var labelText = CreateText("Label", buttonObject.transform, 18, TextAlignmentOptions.Center);
            labelText.text = label;
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }
    }
}

