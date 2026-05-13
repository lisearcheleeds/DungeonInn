using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class AdventurerAiPolicy : IActorAiPolicy
    {
        public bool CanHandle(Actor actor)
        {
            return actor.Behavior is AdventurerBehavior;
        }

        public ActorAiDecision EvaluateLongTerm(ActorAiContext context)
        {
            if (context.Actor.CurrentGoal.Type != ActorGoalType.None && !context.Actor.CurrentGoal.IsCompleted())
            {
                return ActorAiDecision.None();
            }

            return new ActorAiDecision(
                new ActorGoal(ActorGoalType.LevelUp, 0, 0, 0),
                null,
                null);
        }

        public ActorAiDecision EvaluateMidTerm(ActorAiContext context)
        {
            if (context.Actor.CurrentPlan.Type != ActorPlanType.None)
            {
                return ActorAiDecision.None();
            }

            return new ActorAiDecision(
                null,
                new ActorPlan(ActorPlanType.Prepare, 0, 0),
                null);
        }

        public ActorAiDecision EvaluateShortTerm(ActorAiContext context)
        {
            if (context.Actor.CurrentAction.State == ActorActionState.Running)
            {
                return ActorAiDecision.None();
            }

            return new ActorAiDecision(null, null, ActorAction.Wait());
        }
    }
}
