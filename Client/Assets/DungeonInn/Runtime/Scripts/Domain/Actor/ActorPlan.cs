using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorPlan
    {
        public ActorPlanType Type { get; }
        public int TargetId { get; }
        public int TargetCount { get; }
        public Guid? TargetGuid { get; }

        public ActorPlan(ActorPlanType type, int targetId, int targetCount)
            : this(type, targetId, targetCount, null)
        {
        }

        public ActorPlan(ActorPlanType type, int targetId, int targetCount, Guid? targetGuid)
        {
            Type = type;
            TargetId = Math.Max(0, targetId);
            TargetCount = Math.Max(0, targetCount);
            TargetGuid = targetGuid;
        }

        public static ActorPlan None()
        {
            return new ActorPlan(ActorPlanType.None, 0, 0);
        }

        public static ActorPlan UseFacility(Guid facilityId)
        {
            if (facilityId == Guid.Empty)
            {
                throw new ArgumentException("Facility id is required.", nameof(facilityId));
            }

            return new ActorPlan(ActorPlanType.UseFacility, 0, 0, facilityId);
        }
    }
}
