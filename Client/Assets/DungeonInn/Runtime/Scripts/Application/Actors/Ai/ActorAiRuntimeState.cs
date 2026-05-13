using System;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class ActorAiRuntimeState
    {
        public Guid ActorId { get; }
        public ActorAiDirtyFlags DirtyFlags { get; private set; }
        public float LastEvaluatedTimeSeconds { get; private set; }
        public float CooldownUntilTimeSeconds { get; private set; }
        public int EvaluatedFrameId { get; private set; }

        public ActorAiRuntimeState(Guid actorId)
        {
            ActorId = actorId;
            DirtyFlags = ActorAiDirtyFlags.LongTerm | ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm;
            LastEvaluatedTimeSeconds = -1f;
            CooldownUntilTimeSeconds = 0f;
            EvaluatedFrameId = -1;
        }

        public bool HasDirty()
        {
            return DirtyFlags != ActorAiDirtyFlags.None;
        }

        public bool CanEvaluate(float currentTimeSeconds, int evaluationFrameId)
        {
            return HasDirty() && EvaluatedFrameId != evaluationFrameId && CooldownUntilTimeSeconds <= currentTimeSeconds;
        }

        public void MarkDirty(ActorAiDirtyFlags dirtyFlags)
        {
            DirtyFlags |= dirtyFlags;
        }

        public void ClearDirty(ActorAiDirtyFlags dirtyFlags)
        {
            DirtyFlags &= ~dirtyFlags;
        }

        public void MarkEvaluated(float currentTimeSeconds, int evaluationFrameId, float cooldownSeconds)
        {
            LastEvaluatedTimeSeconds = currentTimeSeconds;
            EvaluatedFrameId = evaluationFrameId;
            CooldownUntilTimeSeconds = currentTimeSeconds + Math.Max(0f, cooldownSeconds);
        }

        public ActorAiDirtyFlags GetHighestDirty()
        {
            if ((DirtyFlags & ActorAiDirtyFlags.LongTerm) != 0)
            {
                return ActorAiDirtyFlags.LongTerm;
            }

            if ((DirtyFlags & ActorAiDirtyFlags.MidTerm) != 0)
            {
                return ActorAiDirtyFlags.MidTerm;
            }

            if ((DirtyFlags & ActorAiDirtyFlags.ShortTerm) != 0)
            {
                return ActorAiDirtyFlags.ShortTerm;
            }

            return ActorAiDirtyFlags.None;
        }
    }
}
