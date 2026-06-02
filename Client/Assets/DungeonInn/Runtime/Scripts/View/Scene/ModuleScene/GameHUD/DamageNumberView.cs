using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        const int MaxDigitCount = 8;
        const float DigitSpacing = 0.18f;
        const float TargetHeightPixels = 32f;
        const float FallbackBaseHeightUnits = 1f;

        [SerializeField] Transform digitRoot;
        [SerializeField] SpriteRenderer digitTemplate;
        [SerializeField] Sprite[] digitSprites;

        readonly List<SpriteRenderer> digitRenderers = new();
        Vector3 startPosition;
        float worldUnitsPerPixel = 1f;
        float elapsedSeconds;

        void Awake()
        {
            EnsureDigitPool(MaxDigitCount);
            ResetView();
        }

        public void Show(int damage, Vector3 worldPosition)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            var text = damage.ToString();
            EnsureDigitPool(text.Length);
            startPosition = worldPosition;
            elapsedSeconds = 0f;
            gameObject.SetActive(true);

            var leftOffset = (text.Length - 1) * DigitSpacing * -0.5f;
            for (var index = 0; index < digitRenderers.Count; index++)
            {
                var digitRenderer = digitRenderers[index];
                var isActive = index < text.Length;
                digitRenderer.gameObject.SetActive(isActive);
                if (!isActive)
                {
                    continue;
                }

                var digit = text[index] - '0';
                digitRenderer.sprite = TryGetDigitSprite(digit);
                digitRenderer.color = Color.white;
                digitRenderer.transform.localPosition = new Vector3(leftOffset + index * DigitSpacing, 0f, 0f);
            }

            ApplyAnimation(DamageNumberAnimation.Evaluate(Vector2.zero, elapsedSeconds));
        }

        public bool Tick(float deltaSeconds)
        {
            elapsedSeconds += Mathf.Max(0f, deltaSeconds);
            ApplyAnimation(DamageNumberAnimation.Evaluate(Vector2.zero, elapsedSeconds));
            return elapsedSeconds < DamageNumberAnimation.DurationSeconds;
        }

        public void ResetView()
        {
            elapsedSeconds = 0f;
            transform.position = startPosition;
            transform.localScale = Vector3.one;

            for (var index = 0; index < digitRenderers.Count; index++)
            {
                if (digitRenderers[index] != null)
                {
                    digitRenderers[index].gameObject.SetActive(false);
                }
            }
        }

        public void SetBillboardRotation(Quaternion cameraRotation)
        {
            transform.rotation = cameraRotation;
        }

        public void SetScreenScale(float newWorldUnitsPerPixel)
        {
            worldUnitsPerPixel = Mathf.Max(0f, newWorldUnitsPerPixel);
        }

        void ApplyAnimation(DamageNumberAnimationSample sample)
        {
            var screenScale = worldUnitsPerPixel * TargetHeightPixels / ResolveBaseHeightUnits() * sample.Scale;
            transform.position = startPosition + new Vector3(
                sample.LocalOffset.x * worldUnitsPerPixel,
                sample.LocalOffset.y * worldUnitsPerPixel,
                0f);
            transform.localScale = new Vector3(screenScale, screenScale, 1f);
            for (var index = 0; index < digitRenderers.Count; index++)
            {
                if (digitRenderers[index] == null)
                {
                    continue;
                }

                var color = digitRenderers[index].color;
                color.a = sample.Alpha;
                digitRenderers[index].color = color;
            }
        }

        void EnsureDigitPool(int requiredCount)
        {
            if (digitRoot == null || digitTemplate == null)
            {
                return;
            }

            while (digitRenderers.Count < requiredCount)
            {
                var digitRenderer = Instantiate(digitTemplate, digitRoot);
                digitRenderer.gameObject.name = "Digit";
                digitRenderer.gameObject.SetActive(false);
                digitRenderers.Add(digitRenderer);
            }
        }

        Sprite TryGetDigitSprite(int digit)
        {
            return digitSprites != null && 0 <= digit && digit < digitSprites.Length
                ? digitSprites[digit]
                : null;
        }

        float ResolveBaseHeightUnits()
        {
            for (var index = 0; index < digitRenderers.Count; index++)
            {
                var digitRenderer = digitRenderers[index];
                if (digitRenderer == null ||
                    !digitRenderer.gameObject.activeSelf ||
                    digitRenderer.sprite == null)
                {
                    continue;
                }

                var height = digitRenderer.sprite.bounds.size.y *
                    Mathf.Abs(digitRenderer.transform.localScale.y);
                return 0f < height ? height : FallbackBaseHeightUnits;
            }

            return FallbackBaseHeightUnits;
        }

#if UNITY_EDITOR
        public void EditorAssign(Transform newDigitRoot, SpriteRenderer newDigitTemplate, Sprite[] newDigitSprites)
        {
            digitRoot = newDigitRoot;
            digitTemplate = newDigitTemplate;
            digitSprites = newDigitSprites;
        }
#endif
    }
}
