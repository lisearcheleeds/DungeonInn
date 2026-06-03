using System.Collections.Generic;
using DungeonInn.View.Scene.ModuleScene.GameUI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonInn.Tests.EditMode
{
    public sealed class MinimapViewTests
    {
        [Test]
        public void AdventurerDotsUseBlueCircleSpriteAndMapRotation()
        {
            var viewObject = new GameObject("MinimapViewTest", typeof(RectTransform));
            var viewRect = viewObject.GetComponent<RectTransform>();
            viewRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
            viewRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200f);
            var view = viewObject.AddComponent<MinimapView>();

            try
            {
                view.SetMapRotation(45f);
                view.SetAdventurerDots(new List<Vector2> { new(0.75f, 0.25f) });

                var dot = viewObject.transform.Find("ActorDots/ActorDot");
                Assert.That(dot, Is.Not.Null);

                var image = dot.GetComponent<Image>();
                Assert.That(image.sprite, Is.Not.Null);
                Assert.That(image.color, Is.EqualTo(Color.blue));
                Assert.That(dot.gameObject.activeSelf, Is.True);
                Assert.That(dot.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(50f, -50f)));
                Assert.That(viewRect.localEulerAngles.z, Is.EqualTo(45f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
            }
        }
    }
}
