using DungeonInn.Domain.Map;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ActorView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        ActorSpriteAnimator animator;
        readonly HashSet<MissingSpriteWarningKey> missingSpriteWarnings = new();
        Guid actorId;
        ActorVisualDefinition visualDefinition;
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
        public bool IsDamageOneShotComplete(ActorAnimationDirection direction)
        {
            return animator?.IsDamageOneShotComplete(direction) ?? false;
        }

        void Awake()
        {
            EnsureSpriteRenderer();
            animator = new ActorSpriteAnimator();
            Facing = Vector2.down;
        }

        public void BindActor(Guid value)
        {
            actorId = value;
        }

        public void Reset()
        {
            ClearVisual();
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

        public void ApplyVisual(ActorVisualDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (visualDefinition == definition)
            {
                return;
            }

            visualDefinition = definition;
            EnsureAnimator();
            animator.ApplyVisual(definition);
            missingSpriteWarnings.Clear();
            SetVisualCanvasHeight(ActorVisualSizeTierCatalog.GetCanvasHeightMeters(definition.VisualSizeTier));
        }

        public void ClearVisual()
        {
            visualDefinition = null;
            EnsureAnimator();
            animator.Reset();
            missingSpriteWarnings.Clear();
            SetSprite(null);
        }

        public void SetAnimationState(ActorAnimationState state)
        {
            EnsureAnimator();
            animator.SetState(state);
        }

        public void Tick(float deltaTime, ActorAnimationDirection direction)
        {
            EnsureAnimator();
            animator.Tick(deltaTime, direction);
            ApplyCurrentAnimationSprite(direction);
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

        void ApplyCurrentAnimationSprite(ActorAnimationDirection direction)
        {
            if (visualDefinition == null)
            {
                SetSprite(null);
                return;
            }

            var sprite = animator.GetCurrentSprite(direction);
            if (sprite == null)
            {
                WarnMissingSprite(direction);
            }

            SetSprite(sprite);
        }

        void WarnMissingSprite(ActorAnimationDirection direction)
        {
            var key = new MissingSpriteWarningKey(
                visualDefinition.VisualId,
                animator.CurrentState,
                direction,
                animator.ClipFrameIndex);
            if (!missingSpriteWarnings.Add(key))
            {
                return;
            }

            Debug.LogWarning(
                $"[ActorView] Actor visual sprite is null. ActorId={actorId} VisualId={visualDefinition.VisualId} State={animator.CurrentState} Direction={direction} Frame={animator.ClipFrameIndex}");
        }

        readonly struct MissingSpriteWarningKey : IEquatable<MissingSpriteWarningKey>
        {
            readonly string visualId;
            readonly ActorAnimationState state;
            readonly ActorAnimationDirection direction;
            readonly int frameIndex;

            public MissingSpriteWarningKey(
                string visualId,
                ActorAnimationState state,
                ActorAnimationDirection direction,
                int frameIndex)
            {
                this.visualId = visualId;
                this.state = state;
                this.direction = direction;
                this.frameIndex = frameIndex;
            }

            public bool Equals(MissingSpriteWarningKey other)
            {
                return visualId == other.visualId &&
                    state == other.state &&
                    direction == other.direction &&
                    frameIndex == other.frameIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is MissingSpriteWarningKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = visualId != null ? visualId.GetHashCode() : 0;
                hash = (hash * 397) ^ (int)state;
                hash = (hash * 397) ^ (int)direction;
                hash = (hash * 397) ^ frameIndex;
                return hash;
            }
        }
    }
}
