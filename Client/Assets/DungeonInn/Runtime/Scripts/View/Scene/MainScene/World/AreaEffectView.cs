using DungeonInn.Domain.Combat;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AreaEffectView : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetLocalPosition(Vector3 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public void SetBillboardRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetNormalizedProgress(float t)
        {
            var color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, t);
        }

        public void SetShape(AttackAreaShape shape, float radiusMeters)
        {
            var diameter = radiusMeters * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        public void TriggerHitPulse()
        {
            var color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 1f);
        }

        public void Reset()
        {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            TriggerHitPulse();
        }
    }
}
