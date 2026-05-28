using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class ActorAiDecision
    {
        static readonly ActorAiDirtyRules DirtyRules = new();

        public ActorGoal NextGoal { get; }
        public ActorPlan NextPlan { get; }
        public ActorAction NextAction { get; }
        public ActorAiDirtyFlags AdditionalDirtyFlags { get; }
        public float CooldownSeconds { get; }

        public ActorAiDecision(ActorGoal nextGoal, ActorPlan nextPlan, ActorAction nextAction)
            : this(nextGoal, nextPlan, nextAction, 0f)
        {
        }

        public ActorAiDecision(
            ActorGoal nextGoal,
            ActorPlan nextPlan,
            ActorAction nextAction,
            float cooldownSeconds)
        {
            NextGoal = nextGoal;
            NextPlan = nextPlan;
            NextAction = nextAction;
            CooldownSeconds = cooldownSeconds;
            AdditionalDirtyFlags = DirtyRules.CalculateAdditionalDirtyFlags(nextGoal, nextPlan, nextAction);
        }

        public static ActorAiDecision None()
        {
            return new ActorAiDecision(null, null, null);
        }
    }
}
