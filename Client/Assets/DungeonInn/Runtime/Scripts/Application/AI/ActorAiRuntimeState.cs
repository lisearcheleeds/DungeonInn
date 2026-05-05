using System;

namespace DungeonInn.Application.AI
{
    public sealed class ActorAiRuntimeState
    {
        public Guid ActorId { get; }
        public ActorAiDirtyFlags DirtyFlags { get; private set; }
        public int LastEvaluatedTick { get; private set; }
        public int CooldownUntilTick { get; private set; }
        public int EvaluatedTick { get; private set; }

        public ActorAiRuntimeState(Guid actorId)
        {
            ActorId = actorId;
            DirtyFlags = ActorAiDirtyFlags.LongTerm | ActorAiDirtyFlags.MidTerm | ActorAiDirtyFlags.ShortTerm;
            LastEvaluatedTick = -1;
            CooldownUntilTick = 0;
            EvaluatedTick = -1;
        }

        public bool HasDirty()
        {
            return DirtyFlags != ActorAiDirtyFlags.None;
        }

        public bool CanEvaluate(int currentTick)
        {
            return HasDirty() && EvaluatedTick != currentTick && CooldownUntilTick <= currentTick;
        }

        public void MarkDirty(ActorAiDirtyFlags dirtyFlags)
        {
            DirtyFlags |= dirtyFlags;
        }

        public void ClearDirty(ActorAiDirtyFlags dirtyFlags)
        {
            DirtyFlags &= ~dirtyFlags;
        }

        public void MarkEvaluated(int currentTick, int cooldownTicks)
        {
            LastEvaluatedTick = currentTick;
            EvaluatedTick = currentTick;
            CooldownUntilTick = currentTick + Math.Max(0, cooldownTicks);
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
