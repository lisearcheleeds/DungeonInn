using System;
using System.Collections.Generic;
using LighthouseExtends.ScreenStack;
using LighthouseExtends.UIComponent.Button;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class MarketWindow :
        GameHudScreenStackWindowBase,
        IScreenStackSetup<MarketWindowData>
    {
        MarketWindowData screenStackData;
        readonly List<LHButton> offerButtons = new();

        public void Setup(MarketWindowData screenStackData)
        {
            this.screenStackData = screenStackData ?? throw new ArgumentNullException(nameof(screenStackData));
            Refresh();
        }

        void Refresh()
        {
            ApplyContent("Market", string.Empty);
            SetBodyTextVisible(false);
            RenderOffers(screenStackData.ViewData);
            RebuildOfferButtons(screenStackData.ViewData);
            Debug.Log($"[GameHUD.ScreenStack] MarketWindow displayed. Offers={screenStackData.ViewData.Offers.Count}");
        }

        void RenderOffers(MarketWindowViewData viewData)
        {
            var bodyRoot = GetBodyRoot();
            ClearChildren(bodyRoot);

            if (viewData.Offers.Count == 0)
            {
                CreateText(bodyRoot, "No market offers are currently available.", 22, TextAlignmentOptions.Center);
                return;
            }

            for (var index = 0; index < viewData.Offers.Count; index++)
            {
                CreateOfferRow(bodyRoot, viewData.Offers[index], index);
            }
        }

        void RebuildOfferButtons(MarketWindowViewData viewData)
        {
            ClearOfferButtons();

            var index = 0;
            foreach (var offer in viewData.Offers)
            {
                var button = CreateOfferButton(offer, index);
                offerButtons.Add(button);
                index++;
            }
        }

        void ClearOfferButtons()
        {
            foreach (var button in offerButtons)
            {
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }

            offerButtons.Clear();
        }

        LHButton CreateOfferButton(MarketOfferViewData offer, int index)
        {
            var buttonObject = new GameObject(
                $"DeliverOffer{offer.OfferId}Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LHButton));
            buttonObject.transform.SetParent(GetBodyRoot(), false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-96f, -52f - index * 172f);
            rect.sizeDelta = new Vector2(144f, 42f);

            var image = buttonObject.GetComponent<Image>();
            image.color = offer.CanFulfill
                ? new Color(0.16f, 0.38f, 0.28f, 1f)
                : new Color(0.16f, 0.17f, 0.18f, 1f);

            var button = buttonObject.GetComponent<LHButton>();
            button.targetGraphic = image;
            button.interactable = offer.CanFulfill;
            if (offer.CanFulfill)
            {
                button.onClick.AddListener(() => FulfillOffer(offer.OfferId));
            }

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 16;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = offer.CanFulfill ? $"Deliver #{offer.OfferId}" : $"Need #{offer.OfferId}";

            return button;
        }

        void FulfillOffer(int offerId)
        {
            screenStackData.FulfillOffer(offerId);
            Debug.Log($"[GameHUD.ScreenStack] Market offer fulfilled from UI. OfferId={offerId}");
            screenStackData.Refresh();
            Refresh();
        }

        static void CreateOfferRow(RectTransform bodyRoot, MarketOfferViewData offer, int index)
        {
            var rowObject = new GameObject($"OfferRow{offer.OfferId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rowObject.transform.SetParent(bodyRoot, false);
            var rect = rowObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(0f, -index * 172f);
            rect.sizeDelta = new Vector2(0f, 156f);
            rect.pivot = new Vector2(0.5f, 1f);

            rowObject.GetComponent<Image>().color = offer.CanFulfill
                ? new Color(0.1f, 0.16f, 0.13f, 0.94f)
                : new Color(0.13f, 0.13f, 0.14f, 0.94f);

            CreateAnchoredText(rowObject.transform, $"Offer #{offer.OfferId}", 24, new Vector2(20f, -18f), new Vector2(260f, 34f));
            CreateAnchoredText(rowObject.transform, offer.Status, 18, new Vector2(20f, -54f), new Vector2(260f, 28f))
                .color = offer.CanFulfill ? new Color(0.65f, 0.95f, 0.74f, 1f) : new Color(0.9f, 0.72f, 0.58f, 1f);
            CreateAnchoredText(rowObject.transform, $"Reward: {offer.Reward}", 20, new Vector2(840f, -22f), new Vector2(220f, 34f));

            var requirementLines = new List<string>();
            foreach (var requirement in offer.Requirements)
            {
                requirementLines.Add($"{requirement.ItemName}: {requirement.Count} {requirement.Missing}");
            }

            CreateAnchoredText(rowObject.transform, string.Join("\n", requirementLines), 18, new Vector2(300f, -22f), new Vector2(500f, 112f));
        }

        static void ClearChildren(RectTransform root)
        {
            for (var index = root.childCount - 1; 0 <= index; index--)
            {
                Destroy(root.GetChild(index).gameObject);
            }
        }

        static TMP_Text CreateAnchoredText(Transform parent, string text, int fontSize, Vector2 position, Vector2 size)
        {
            var textComponent = CreateText(parent, text, fontSize, TextAlignmentOptions.TopLeft);
            var rect = textComponent.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.pivot = new Vector2(0f, 1f);
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
