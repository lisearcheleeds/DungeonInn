using DungeonInn.Domain.Map;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ActorView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] ActorSpriteAnimationClip idleAnimationClip;
        [SerializeField] ActorSpriteAnimationClip walkAnimationClip;
        [SerializeField] ActorSpriteAnimationClip combatAnimationClip;
        [SerializeField] ActorSpriteAnimationClip hitAnimationClip;
        [SerializeField] ActorSpriteAnimationClip deadAnimationClip;

        ActorSpriteAnimator animator;
        float targetCanvasHeightMeters;
        Sprite currentSprite;
        float currentVisualScale = 1f;
        Quaternion currentBillboardRotation;
        bool hasBillboardRotation;
        bool visible = true;
        bool currentFlipX;

        public Vector2 Facing { get; private set; }
        public LayerPosition LastPosition { get; private set; }
        public bool HasLastPosition { get; private set; }
        public int CurrentFrameIndex => animator != null ? animator.CurrentFrameIndex : 0;
        public bool IsHitOneShotComplete => animator?.IsHitOneShotComplete ?? false;

        void Awake()
        {
            EnsureSpriteRenderer();
            animator = new ActorSpriteAnimator();
            animator.Setup(idleAnimationClip, walkAnimationClip);
            animator.SetupCombatClips(combatAnimationClip, hitAnimationClip, deadAnimationClip);
            Facing = Vector2.down;
        }

        public void Reset()
        {
            SetSprite(null);
            SetFlip(false);
            SetVisualCanvasHeight(0f);
            SetVisible(true);
            hasBillboardRotation = false;
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

        public void SetupCombatAnimation(
            ActorSpriteAnimationClip combat,
            ActorSpriteAnimationClip hit,
            ActorSpriteAnimationClip dead)
        {
            EnsureAnimator();
            animator.SetupCombatClips(combat, hit, dead);
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
            EnsureSpriteRenderer();
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
            EnsureSpriteRenderer();
            if (currentFlipX == flipX)
            {
                return;
            }

            currentFlipX = flipX;
            spriteRenderer.flipX = flipX;
        }

        public void SetVisible(bool value)
        {
            EnsureSpriteRenderer();
            if (visible == value && spriteRenderer.enabled == value)
            {
                return;
            }

            visible = value;
            spriteRenderer.enabled = value;
        }

        public void SetBillboardRotation(Quaternion rotation)
        {
            if (hasBillboardRotation && currentBillboardRotation == rotation)
            {
                return;
            }

            currentBillboardRotation = rotation;
            hasBillboardRotation = true;
            transform.rotation = rotation;
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
            animator.SetupCombatClips(combatAnimationClip, hitAnimationClip, deadAnimationClip);
        }

        void EnsureSpriteRenderer()
        {
            if (spriteRenderer != null)
            {
                return;
            }

            if (!TryGetComponent(out spriteRenderer))
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        void ApplyVisualScale()
        {
            EnsureSpriteRenderer();
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
