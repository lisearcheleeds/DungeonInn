using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ActorView : MonoBehaviour
    {
        [SerializeField] ActorSpriteAnimationClip idleAnimationClip;
        [SerializeField] ActorSpriteAnimationClip walkAnimationClip;

        SpriteRenderer spriteRenderer;
        ActorSpriteAnimator animator;
        float targetCanvasHeightMeters;
        Sprite currentSprite;
        float currentVisualScale = 1f;
        bool hasRotationY;
        bool visible = true;
        bool currentFlipX;
        float currentRotationY;

        public Vector2 Facing { get; private set; }
        public LayerPosition LastPosition { get; private set; }
        public bool HasLastPosition { get; private set; }
        public int CurrentFrameIndex => animator != null ? animator.CurrentFrameIndex : 0;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = new ActorSpriteAnimator();
            animator.Setup(idleAnimationClip, walkAnimationClip);
            Facing = Vector2.down;
        }

        public void Reset()
        {
            SetSprite(null);
            SetFlip(false);
            SetVisualCanvasHeight(0f);
            SetVisible(true);
            hasRotationY = false;
            Facing = Vector2.down;
            LastPosition = default;
            HasLastPosition = false;
            animator?.Reset();
        }

        public void SetupAnimation(ActorSpriteAnimationClip idle, ActorSpriteAnimationClip walk)
        {
            EnsureAnimator();
            animator.Setup(idle, walk);
        }

        public void SetAnimationState(ActorAnimationState state)
        {
            EnsureAnimator();
            animator.SetState(state);
        }

        public void Tick(float deltaTime)
        {
            EnsureAnimator();
            animator.Tick(deltaTime);
        }

        public void SetSprite(Sprite sprite)
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            if (currentSprite == sprite)
            {
                return;
            }

            currentSprite = sprite;
            spriteRenderer.sprite = sprite;
            ApplyVisualScale();
        }

        public void SetVisualCanvasHeight(float heightMeters)
        {
            if (Mathf.Approximately(targetCanvasHeightMeters, heightMeters))
            {
                return;
            }

            targetCanvasHeightMeters = heightMeters;
            ApplyVisualScale();
        }

        public void SetFlip(bool flipX)
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            if (currentFlipX == flipX)
            {
                return;
            }

            currentFlipX = flipX;
            spriteRenderer.flipX = flipX;
        }

        public void SetVisible(bool value)
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            if (visible == value && spriteRenderer.enabled == value)
            {
                return;
            }

            visible = value;
            spriteRenderer.enabled = value;
        }

        public void SetRotationY(float degrees)
        {
            if (hasRotationY && Mathf.Approximately(currentRotationY, degrees))
            {
                return;
            }

            currentRotationY = degrees;
            hasRotationY = true;
            transform.rotation = Quaternion.Euler(0f, degrees, 0f);
        }

        public void SetLocalPosition(Vector3 position)
        {
            transform.localPosition = position;
        }

        public bool UpdateFacing(LayerPosition position)
        {
            var previousFacing = Facing;
            if (HasLastPosition && LastPosition.LayerId.Equals(position.LayerId))
            {
                var movement = new Vector2(
                    position.X - LastPosition.X,
                    position.Z - LastPosition.Z);
                if (0.0001f < movement.sqrMagnitude)
                {
                    Facing = movement.normalized;
                }
            }

            LastPosition = position;
            HasLastPosition = true;
            return !previousFacing.Equals(Facing);
        }

        void EnsureAnimator()
        {
            if (animator != null)
            {
                return;
            }

            animator = new ActorSpriteAnimator();
            animator.Setup(idleAnimationClip, walkAnimationClip);
        }

        void ApplyVisualScale()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            var sprite = spriteRenderer.sprite;
            if (targetCanvasHeightMeters <= 0f || sprite == null || sprite.bounds.size.y <= 0f)
            {
                ApplyScale(1f);
                return;
            }

            var scale = targetCanvasHeightMeters / sprite.bounds.size.y;
            ApplyScale(scale);
        }

        void ApplyScale(float scale)
        {
            if (Mathf.Approximately(currentVisualScale, scale))
            {
                return;
            }

            currentVisualScale = scale;
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
