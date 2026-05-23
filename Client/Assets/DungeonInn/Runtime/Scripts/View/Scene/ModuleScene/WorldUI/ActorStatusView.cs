using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.WorldUI
{
    public sealed class ActorStatusView : MonoBehaviour
    {
        [SerializeField] Image hpBarFillImage;
        [SerializeField] Image[] statusIconImages;

        public void SetHpRatio(float ratio)
        {
            if (hpBarFillImage != null)
            {
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
            ((RectTransform)transform).position = screenPosition;
        }

        public void Reset()
        {
            if (hpBarFillImage != null)
            {
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
    }
}
