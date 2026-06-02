using DungeonInn.View.Scene.ModuleScene.GameHUD;
using NUnit.Framework;
using UnityEngine;

namespace DungeonInn.Tests.EditMode
{
    public sealed class DamageNumberAnimationTests
    {
        [Test]
        public void EvaluateStartsAtInitialPositionAndVisible()
        {
            var start = new Vector2(12f, 34f);

            var sample = DamageNumberAnimation.Evaluate(start, 0f);

            Assert.That(sample.LocalOffset, Is.EqualTo(start));
            Assert.That(sample.Alpha, Is.EqualTo(1f));
            Assert.That(sample.Scale, Is.GreaterThan(1f));
        }

        [Test]
        public void EvaluateEndsAboveStartAndTransparent()
        {
            var start = new Vector2(12f, 34f);

            var sample = DamageNumberAnimation.Evaluate(start, DamageNumberAnimation.DurationSeconds);

            Assert.That(sample.LocalOffset.y, Is.GreaterThan(start.y));
            Assert.That(sample.Alpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(sample.Scale, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void DamageNumberViewAppliesScreenScaleAndPixelOffset()
        {
            var viewObject = new GameObject("DamageNumberViewScaleTest");
            var view = viewObject.AddComponent<DamageNumberView>();

            try
            {
                view.SetScreenScale(0.02f);
                view.Show(12, Vector3.one);
                view.Tick(DamageNumberAnimation.DurationSeconds);

                Assert.That(view.transform.localScale.x, Is.EqualTo(0.64f).Within(0.001f));
                Assert.That(view.transform.localScale.y, Is.EqualTo(0.64f).Within(0.001f));
                Assert.That(view.transform.position.y, Is.EqualTo(2.16f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
            }
        }
    }
}
