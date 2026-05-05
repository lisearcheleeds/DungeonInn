using Cysharp.Threading.Tasks;
using DungeonInn.Application.AI;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.UseCase
{
    public sealed class ApplyActorAiDecisionUseCase
    {
        public UniTask ExecuteAsync(Actor actor, ActorAiDecision decision)
        {
            if (decision.NextGoal != null)
            {
                actor.ChangeGoal(decision.NextGoal);
            }

            if (decision.NextPlan != null)
            {
                actor.ChangePlan(decision.NextPlan);
            }

            if (decision.NextAction != null)
            {
                actor.ChangeAction(decision.NextAction);
            }

            return UniTask.CompletedTask;
        }
    }
}
