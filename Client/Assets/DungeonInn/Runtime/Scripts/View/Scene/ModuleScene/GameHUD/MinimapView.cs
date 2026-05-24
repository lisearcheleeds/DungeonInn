using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class MinimapView : MonoBehaviour
    {
        [SerializeField] RawImage mapImage;

        public void SetMapTexture(Texture2D texture)
        {
            if (mapImage == null)
            {
                return;
            }

            mapImage.texture = texture;
        }

        public void SetActorDots(IReadOnlyList<Vector2> normalizedPositions, Color[] colors)
        {
            if (normalizedPositions == null)
            {
                throw new ArgumentNullException(nameof(normalizedPositions));
            }

            if (colors == null)
            {
                throw new ArgumentNullException(nameof(colors));
            }

            if (colors.Length < normalizedPositions.Count)
            {
                throw new ArgumentException("Actor dot color count must cover all positions.", nameof(colors));
            }
        }
    }
}
