using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    public sealed class MinimapView : MonoBehaviour
    {
        const float ActorDotSize = 8f;
        const int CircleSpriteSize = 16;

        static Sprite fallbackActorDotSprite;

        [SerializeField] RectTransform mapRoot;
        [SerializeField] RawImage mapImage;
        [SerializeField] RectTransform actorDotRoot;
        [SerializeField] Image actorDotTemplate;

        readonly List<Image> actorDots = new();

        public void SetMapTexture(Texture2D texture)
        {
            EnsureReferences();
            if (mapImage == null)
            {
                return;
            }

            mapImage.texture = texture;
        }

        public void SetMapRotation(float yawDegrees)
        {
            EnsureReferences();
            if (mapRoot != null)
            {
                mapRoot.localEulerAngles = new Vector3(0f, 0f, yawDegrees);
            }
        }

        public void SetAdventurerDots(IReadOnlyList<Vector2> normalizedPositions)
        {
            if (normalizedPositions == null)
            {
                throw new ArgumentNullException(nameof(normalizedPositions));
            }

            EnsureReferences();
            if (actorDotRoot == null || actorDotTemplate == null)
            {
                return;
            }

            EnsureDotPool(normalizedPositions.Count);
            var rootRect = actorDotRoot.rect;
            for (var index = 0; index < actorDots.Count; index++)
            {
                var dot = actorDots[index];
                var isActive = index < normalizedPositions.Count;
                dot.gameObject.SetActive(isActive);
                if (!isActive)
                {
                    continue;
                }

                var normalizedPosition = normalizedPositions[index];
                dot.rectTransform.anchoredPosition = new Vector2(
                    (normalizedPosition.x - 0.5f) * rootRect.width,
                    (normalizedPosition.y - 0.5f) * rootRect.height);
            }
        }

        void EnsureReferences()
        {
            if (mapRoot == null)
            {
                mapRoot = mapImage != null
                    ? mapImage.rectTransform.parent as RectTransform
                    : transform as RectTransform;
            }

            if (mapImage == null && mapRoot != null)
            {
                mapImage = mapRoot.GetComponentInChildren<RawImage>(true);
            }

            if (actorDotRoot == null && mapRoot != null)
            {
                actorDotRoot = mapRoot.Find("ActorDots") as RectTransform;
            }

            if (actorDotRoot == null && mapRoot != null)
            {
                actorDotRoot = CreateActorDotRoot(mapRoot);
            }

            if (actorDotTemplate == null && actorDotRoot != null)
            {
                actorDotTemplate = actorDotRoot.GetComponentInChildren<Image>(true);
            }

            if (actorDotTemplate == null && actorDotRoot != null)
            {
                actorDotTemplate = CreateActorDotTemplate(actorDotRoot);
            }

            if (actorDotTemplate != null && actorDotTemplate.sprite == null)
            {
                actorDotTemplate.sprite = GetFallbackActorDotSprite();
            }
        }

        void EnsureDotPool(int requiredCount)
        {
            if (actorDotTemplate != null && !actorDots.Contains(actorDotTemplate))
            {
                actorDots.Add(actorDotTemplate);
            }

            while (actorDots.Count < requiredCount)
            {
                var dot = Instantiate(actorDotTemplate, actorDotRoot);
                dot.gameObject.name = "ActorDot";
                actorDots.Add(dot);
            }
        }

        static RectTransform CreateActorDotRoot(RectTransform parent)
        {
            var rootObject = new GameObject("ActorDots", typeof(RectTransform));
            var rectTransform = rootObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return rectTransform;
        }

        static Image CreateActorDotTemplate(RectTransform parent)
        {
            var dotObject = new GameObject("ActorDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rectTransform = dotObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(ActorDotSize, ActorDotSize);

            var image = dotObject.GetComponent<Image>();
            image.sprite = GetFallbackActorDotSprite();
            image.color = Color.blue;
            image.raycastTarget = false;
            dotObject.SetActive(false);
            return image;
        }

        static Sprite GetFallbackActorDotSprite()
        {
            if (fallbackActorDotSprite != null)
            {
                return fallbackActorDotSprite;
            }

            var texture = new Texture2D(CircleSpriteSize, CircleSpriteSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var center = (CircleSpriteSize - 1) * 0.5f;
            var radius = center;
            for (var y = 0; y < CircleSpriteSize; y++)
            {
                for (var x = 0; x < CircleSpriteSize; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
                }
            }

            texture.Apply(false);
            fallbackActorDotSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, CircleSpriteSize, CircleSpriteSize),
                new Vector2(0.5f, 0.5f),
                CircleSpriteSize);
            return fallbackActorDotSprite;
        }
    }
}
