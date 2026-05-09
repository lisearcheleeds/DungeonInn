using System;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorGoalCompleted : IGameEvent
    {
        public Guid ActorId { get; }
        public ActorGoalType GoalType { get; }
        public int TargetId { get; }
        public int ProgressCount { get; }
        public int TargetCount { get; }

        public ActorGoalCompleted(
            Guid actorId,
            ActorGoalType goalType,
            int targetId,
            int progressCount,
            int targetCount)
        {
            ActorId = actorId;
            GoalType = goalType;
            TargetId = Math.Max(0, targetId);
            ProgressCount = Math.Max(0, progressCount);
            TargetCount = Math.Max(0, targetCount);
        }
    }
}
