using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorSpriteAnimator
    {
        ActorSpriteAnimationClip idleClip;
        ActorSpriteAnimationClip walkClip;
        ActorSpriteAnimationClip combatClip;
        ActorSpriteAnimationClip hitClip;
        ActorSpriteAnimationClip deadClip;
        ActorAnimationState currentState = ActorAnimationState.Idle;
        float elapsed;
        int clipFrameIndex;
        ActorAnimationState lastMissingClipWarningState;
        bool hasMissingClipWarningState;

        public int CurrentFrameIndex { get; private set; }
        public bool IsHitOneShotComplete =>
            currentState == ActorAnimationState.Hit &&
            hitClip != null &&
            !hitClip.Loop &&
            clipFrameIndex >= hitClip.FrameCount - 1;

        public void Setup(ActorSpriteAnimationClip idle, ActorSpriteAnimationClip walk)
        {
            idleClip = idle;
            walkClip = walk;
            clipFrameIndex = 0;
            CurrentFrameIndex = ResolveCurrentFrameIndex();
        }

        public void SetupCombatClips(
            ActorSpriteAnimationClip combat,
            ActorSpriteAnimationClip hit,
            ActorSpriteAnimationClip dead)
        {
            combatClip = combat;
            hitClip = hit;
            deadClip = dead;
            hasMissingClipWarningState = false;
            CurrentFrameIndex = ResolveCurrentFrameIndex();
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
            CurrentFrameIndex = ResolveCurrentFrameIndex();
        }

        public void Tick(float deltaTime)
        {
            var clip = ResolveClip(true);
            if (clip == null || clip.Fps <= 0f || clip.FrameCount <= 1)
            {
                clipFrameIndex = 0;
                CurrentFrameIndex = ResolveCurrentFrameIndex();
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

                CurrentFrameIndex = ResolveCurrentFrameIndex();
            }
        }

        public void Reset()
        {
            currentState = ActorAnimationState.Idle;
            elapsed = 0f;
            clipFrameIndex = 0;
            CurrentFrameIndex = ResolveCurrentFrameIndex();
        }

        ActorSpriteAnimationClip ResolveClip(bool logMissingClipWarning)
        {
            var clip = currentState switch
            {
                ActorAnimationState.Idle => idleClip,
                ActorAnimationState.Walk => walkClip,
                ActorAnimationState.Combat => combatClip,
                ActorAnimationState.Hit => hitClip,
                ActorAnimationState.Dead => deadClip,
                _ => idleClip
            };

            if (clip != null || currentState == ActorAnimationState.Idle)
            {
                return clip;
            }

            if (logMissingClipWarning &&
                (!hasMissingClipWarningState || lastMissingClipWarningState != currentState))
            {
                Debug.LogWarning(
                    $"[ActorSpriteAnimator] {currentState} animation clip is not assigned. Falling back to idle clip.");
                lastMissingClipWarningState = currentState;
                hasMissingClipWarningState = true;
            }

            return idleClip;
        }

        int ResolveCurrentFrameIndex()
        {
            var clip = ResolveClip(false);
            return clip != null ? clip.GetFrameIndex(clipFrameIndex) : 0;
        }
    }
}
