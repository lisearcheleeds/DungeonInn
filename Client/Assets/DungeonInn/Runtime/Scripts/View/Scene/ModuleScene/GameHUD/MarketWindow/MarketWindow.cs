using System;
using System.Collections.Generic;
using System.Text;
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
            ApplyContent(
                "Market",
                Format(screenStackData.ViewData));
            RebuildOfferButtons(screenStackData.ViewData);
            Debug.Log($"[GameHUD.ScreenStack] MarketWindow displayed. Offers={screenStackData.ViewData.Offers.Count}");
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
            buttonObject.transform.SetParent(transform, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(104f + index * 148f, 36f);
            rect.sizeDelta = new Vector2(132f, 38f);

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

        static string Format(MarketWindowViewData viewData)
        {
            var builder = new StringBuilder();
            foreach (var offer in viewData.Offers)
            {
                builder.AppendLine($"Offer #{offer.OfferId} - {offer.Status} - Reward {offer.Reward}");
                foreach (var requirement in offer.Requirements)
                {
                    builder.AppendLine($"- {requirement.ItemName}: {requirement.Count} {requirement.Missing}");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
