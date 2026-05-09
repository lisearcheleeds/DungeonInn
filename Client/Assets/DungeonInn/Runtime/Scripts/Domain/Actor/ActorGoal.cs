using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorGoal
    {
        public ActorGoalType Type { get; }
        public int TargetId { get; }
        public int TargetCount { get; }
        public int ProgressCount { get; private set; }

        public ActorGoal(ActorGoalType type, int targetId, int targetCount, int progressCount)
        {
            Type = type;
            TargetId = Math.Max(0, targetId);
            TargetCount = Math.Max(0, targetCount);
            ProgressCount = Math.Max(0, progressCount);
        }

        public static ActorGoal None()
        {
            return new ActorGoal(ActorGoalType.None, 0, 0, 0);
        }

        public void SetProgress(int progressCount)
        {
            ProgressCount = Math.Max(0, progressCount);
        }

        public void AddProgress(int amount)
        {
            ProgressCount = Math.Max(0, ProgressCount + Math.Max(0, amount));
        }

        public bool IsCompleted()
        {
            return Type == ActorGoalType.None || (0 < TargetCount && TargetCount <= ProgressCount);
        }
    }
}
