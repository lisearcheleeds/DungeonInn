using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorSpriteAnimator
    {
        ActorVisualDefinition visualDefinition;
        ActorAnimationState currentState = ActorAnimationState.Idle;
        float elapsed;
        int clipFrameIndex;

        public ActorAnimationState CurrentState => currentState;
        public int ClipFrameIndex => clipFrameIndex;
        public bool IsDamageOneShotComplete(ActorAnimationDirection direction)
        {
            if (currentState != ActorAnimationState.Damage ||
                !TryGetClip(direction, out var clip) ||
                clip.Loop)
            {
                return false;
            }

            return clip.FrameCount <= clipFrameIndex + 1;
        }

        public void ApplyVisual(ActorVisualDefinition definition)
        {
            visualDefinition = definition;
            elapsed = 0f;
            clipFrameIndex = 0;
        }

        public void SetState(ActorAnimationState state)
        {
            if (currentState == state)
            {
                return;
            }

            currentState = state;
            elapsed = 0f;
            clipFrameIndex = 0;
        }

        public void Tick(float deltaTime, ActorAnimationDirection direction)
        {
            if (!TryGetClip(direction, out var clip))
            {
                clipFrameIndex = 0;
                return;
            }

            if (clip == null || clip.Fps <= 0f || clip.FrameCount <= 1)
            {
                clipFrameIndex = 0;
                return;
            }

            elapsed += deltaTime;
            var frameDuration = 1f / clip.Fps;
            if (frameDuration <= elapsed)
            {
                var framesToAdvance = (int)(elapsed / frameDuration);
                elapsed %= frameDuration;
                if (clip.Loop)
                {
                    clipFrameIndex = (clipFrameIndex + framesToAdvance) % clip.FrameCount;
                }
                else
                {
                    clipFrameIndex = System.Math.Min(clipFrameIndex + framesToAdvance, clip.FrameCount - 1);
                }
            }
        }

        public void Reset()
        {
            currentState = ActorAnimationState.Idle;
            visualDefinition = null;
            elapsed = 0f;
            clipFrameIndex = 0;
        }

        public Sprite GetCurrentSprite(ActorAnimationDirection direction)
        {
            if (!TryGetClip(direction, out var clip))
            {
                return null;
            }

            return clip.GetSprite(clipFrameIndex);
        }

        bool TryGetClip(ActorAnimationDirection direction, out ActorVisualAnimationClip clip)
        {
            clip = null;
            return visualDefinition != null &&
                visualDefinition.TryGetClip(ToAnimationKey(currentState), direction, out clip);
        }

        static ActorAnimationKey ToAnimationKey(ActorAnimationState state)
        {
            switch (state)
            {
                case ActorAnimationState.Idle:
                    return ActorAnimationKey.Idle;
                case ActorAnimationState.Walk:
                    return ActorAnimationKey.Walk;
                case ActorAnimationState.Work:
                    return ActorAnimationKey.Work;
                case ActorAnimationState.Casting:
                case ActorAnimationState.WindUp:
                    return ActorAnimationKey.Idle;
                case ActorAnimationState.Attack:
                    return ActorAnimationKey.Attack;
                case ActorAnimationState.Damage:
                    return ActorAnimationKey.Damage;
                case ActorAnimationState.Dead:
                    return ActorAnimationKey.Dead;
                default:
                    return ActorAnimationKey.Idle;
            }
        }
    }
}
