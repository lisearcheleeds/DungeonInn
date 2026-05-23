using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ProjectileView : MonoBehaviour
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

        public void Reset()
        {
            transform.localPosition = Vector3.zero;
        }
    }
}
