using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.AI
{
    public sealed class ActorAiDecision
    {
        public ActorGoal NextGoal { get; }
        public ActorPlan NextPlan { get; }
        public ActorAction NextAction { get; }
        public ActorAiDirtyFlags AdditionalDirtyFlags { get; }

        public ActorAiDecision(ActorGoal nextGoal, ActorPlan nextPlan, ActorAction nextAction)
        {
            NextGoal = nextGoal;
            NextPlan = nextPlan;
            NextAction = nextAction;
            AdditionalDirtyFlags = new ActorAiDirtyRules().CalculateAdditionalDirtyFlags(nextGoal, nextPlan, nextAction);
        }

        public static ActorAiDecision None()
        {
            return new ActorAiDecision(null, null, null);
        }
    }
}
