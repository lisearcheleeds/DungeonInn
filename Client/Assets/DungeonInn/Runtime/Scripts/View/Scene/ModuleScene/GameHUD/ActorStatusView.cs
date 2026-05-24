using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DungeonInn.Application.World;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class ActorStatusView : MonoBehaviour
    {
        [SerializeField] Image hpBarFillImage;
        [SerializeField] Image[] statusIconImages;

        void Awake()
        {
            ConfigureHpBarFillImage();
        }

        public void SetHpRatio(float ratio)
        {
            if (hpBarFillImage != null)
            {
                ConfigureHpBarFillImage();
                hpBarFillImage.fillAmount = Mathf.Clamp01(ratio);
            }
        }

        public void SetStatusIcons(IReadOnlyList<ActorEffectIconViewData> effects)
        {
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (statusIconImages == null)
            {
                return;
            }

            for (var index = 0; index < statusIconImages.Length; index++)
            {
                if (statusIconImages[index] == null)
                {
                    continue;
                }

                statusIconImages[index].gameObject.SetActive(index < effects.Count);
            }
        }

        public void SetScreenPosition(Vector2 screenPosition)
        {
            var rectTransform = (RectTransform)transform;
            if (rectTransform.parent is not RectTransform parentRectTransform)
            {
                rectTransform.position = screenPosition;
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRectTransform,
                    screenPosition,
                    camera,
                    out var localPoint))
            {
                rectTransform.anchoredPosition = localPoint;
            }
        }

        public void Reset()
        {
            if (hpBarFillImage != null)
            {
                ConfigureHpBarFillImage();
                hpBarFillImage.fillAmount = 1f;
            }

            if (statusIconImages == null)
            {
                return;
            }

            for (var index = 0; index < statusIconImages.Length; index++)
            {
                if (statusIconImages[index] != null)
                {
                    statusIconImages[index].gameObject.SetActive(false);
                }
            }
        }

        void ConfigureHpBarFillImage()
        {
            if (hpBarFillImage == null)
            {
                return;
            }

            hpBarFillImage.type = Image.Type.Filled;
            hpBarFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpBarFillImage.fillClockwise = true;
        }
    }
}
