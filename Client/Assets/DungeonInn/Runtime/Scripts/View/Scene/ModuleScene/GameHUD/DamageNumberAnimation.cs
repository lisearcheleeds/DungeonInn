using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public readonly struct DamageNumberAnimationSample
    {
        public DamageNumberAnimationSample(Vector2 localOffset, float alpha, float scale)
        {
            LocalOffset = localOffset;
            Alpha = alpha;
            Scale = scale;
        }

        public Vector2 LocalOffset { get; }
        public float Alpha { get; }
        public float Scale { get; }
    }

    public static class DamageNumberAnimation
    {
        public const float DurationSeconds = 0.5f;
        public const float RisePixels = 20f;

        const float FadeStartRatio = 0.58f;
        const float InitialScale = 1.28f;
        const float RestScale = 1f;

        public static DamageNumberAnimationSample Evaluate(Vector2 initialLocalOffset, float elapsedSeconds)
        {
            var ratio = Mathf.Clamp01(elapsedSeconds / DurationSeconds);
            var easedRise = 1f - Mathf.Pow(1f - ratio, 2f);
            var localOffset = initialLocalOffset + new Vector2(0f, RisePixels * easedRise);
            var scale = Mathf.Lerp(InitialScale, RestScale, Mathf.Clamp01(ratio * 2.5f));
            var alpha = ResolveAlpha(ratio);
            return new DamageNumberAnimationSample(localOffset, alpha, scale);
        }

        static float ResolveAlpha(float ratio)
        {
            if (ratio <= FadeStartRatio)
            {
                return 1f;
            }

            return 1f - Mathf.Clamp01((ratio - FadeStartRatio) / (1f - FadeStartRatio));
        }
    }
}
