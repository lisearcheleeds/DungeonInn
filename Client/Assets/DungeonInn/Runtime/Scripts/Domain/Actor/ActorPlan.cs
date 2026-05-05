using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorPlan
    {
        public ActorPlanType Type { get; }
        public int TargetId { get; }
        public int TargetCount { get; }

        public ActorPlan(ActorPlanType type, int targetId, int targetCount)
        {
            Type = type;
            TargetId = Math.Max(0, targetId);
            TargetCount = Math.Max(0, targetCount);
        }

        public static ActorPlan None()
        {
            return new ActorPlan(ActorPlanType.None, 0, 0);
        }
    }
}
