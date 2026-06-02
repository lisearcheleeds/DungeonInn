using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonInn.Application.World;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class ActorStatusView : MonoBehaviour
    {
        const int MaxStatusIconCount = 4;
        const float TargetWidthPixels = 120f;
        const float TargetHeightPixels = 28f;
        const float StatusIconSizePixels = 16f;
        const float StatusIconSpacingPixels = 20f;
        const float StatusIconTopOffsetPixels = 22f;

        [SerializeField] Transform billboardRoot;
        [SerializeField] SpriteRenderer hpBarRenderer;
        [SerializeField] SpriteRenderer[] statusIconRenderers;

        public void SetHpRatio(float ratio)
        {
            if (hpBarRenderer == null)
            {
                return;
            }

            var clampedRatio = Mathf.Clamp01(ratio);
            hpBarRenderer.color = new Color(1f, 1f, 1f, clampedRatio);
        }

        public void SetStatusIcons(
            IReadOnlyList<ActorEffectIconViewData> effects,
            ActorEffectIconSpriteCatalog iconSpriteCatalog)
        {
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (iconSpriteCatalog == null)
            {
                throw new ArgumentNullException(nameof(iconSpriteCatalog));
            }

            if (statusIconRenderers == null)
            {
                return;
            }

            for (var index = 0; index < statusIconRenderers.Length; index++)
            {
                var iconRenderer = statusIconRenderers[index];
                if (iconRenderer == null)
                {
                    continue;
                }

                var isActive = index < effects.Count;
                iconRenderer.gameObject.SetActive(isActive);
                if (isActive)
                {
                    var hasSprite = iconSpriteCatalog.TryGetSprite(effects[index], out var sprite);
                    iconRenderer.sprite = sprite;
                    iconRenderer.gameObject.SetActive(hasSprite);
                }
            }
        }

        public void SetWorldPosition(Vector3 worldPosition, Quaternion cameraRotation)
        {
            transform.position = worldPosition;
            if (billboardRoot != null)
            {
                billboardRoot.rotation = cameraRotation;
            }
        }

        public void SetScreenScale(float worldUnitsPerPixel)
        {
            var unitPerPixel = Mathf.Max(0f, worldUnitsPerPixel);
            transform.localScale = Vector3.one;
            if (hpBarRenderer != null)
            {
                hpBarRenderer.size = new Vector2(
                    TargetWidthPixels * unitPerPixel,
                    TargetHeightPixels * unitPerPixel);
            }

            ApplyStatusIconScreenLayout(unitPerPixel);
        }

        public void ResetView()
        {
            transform.localScale = Vector3.one;
            SetHpRatio(1f);
            if (statusIconRenderers == null)
            {
                return;
            }

            for (var index = 0; index < statusIconRenderers.Length; index++)
            {
                if (statusIconRenderers[index] != null)
                {
                    statusIconRenderers[index].sprite = null;
                    statusIconRenderers[index].gameObject.SetActive(false);
                }
            }
        }

        void ApplyStatusIconScreenLayout(float worldUnitsPerPixel)
        {
            if (statusIconRenderers == null)
            {
                return;
            }

            var iconSize = StatusIconSizePixels * worldUnitsPerPixel;
            var iconSpacing = StatusIconSpacingPixels * worldUnitsPerPixel;
            var topOffset = StatusIconTopOffsetPixels * worldUnitsPerPixel;
            var startX = -iconSpacing * (MaxStatusIconCount - 1) * 0.5f;
            for (var index = 0; index < statusIconRenderers.Length; index++)
            {
                var iconRenderer = statusIconRenderers[index];
                if (iconRenderer == null)
                {
                    continue;
                }

                iconRenderer.size = new Vector2(iconSize, iconSize);
                iconRenderer.transform.localScale = Vector3.one;
                iconRenderer.transform.localPosition = new Vector3(
                    startX + iconSpacing * index,
                    topOffset,
                    0f);
            }
        }

#if UNITY_EDITOR
        public void EditorAssign(
            Transform newBillboardRoot,
            SpriteRenderer newHpBarRenderer,
            SpriteRenderer[] newStatusIconRenderers)
        {
            billboardRoot = newBillboardRoot;
            hpBarRenderer = newHpBarRenderer;
            statusIconRenderers = newStatusIconRenderers;
        }
#endif
    }
}
