namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorSpriteAnimator
    {
        ActorSpriteAnimationClip idleClip;
        ActorSpriteAnimationClip walkClip;
        ActorAnimationState currentState = ActorAnimationState.Idle;
        float elapsed;
        int clipFrameIndex;

        public int CurrentFrameIndex { get; private set; }

        public void Setup(ActorSpriteAnimationClip idle, ActorSpriteAnimationClip walk)
        {
            idleClip = idle;
            walkClip = walk;
            clipFrameIndex = 0;
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
            var clip = currentState == ActorAnimationState.Walk ? walkClip : idleClip;
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

        int ResolveCurrentFrameIndex()
        {
            var clip = currentState == ActorAnimationState.Walk ? walkClip : idleClip;
            return clip != null ? clip.GetFrameIndex(clipFrameIndex) : 0;
        }
    }
}
