using System;
using Cysharp.Threading.Tasks;
using VContainer;
using DungeonInn.Application.Actors.Phase;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class ApplyActorAiDecisionUseCase
    {
        readonly IActorActionPhaseStateStore phaseStateStore;

        [Inject]
        public ApplyActorAiDecisionUseCase(IActorActionPhaseStateStore phaseStateStore)
        {
            this.phaseStateStore = phaseStateStore ?? throw new ArgumentNullException(nameof(phaseStateStore));
        }

        public UniTask ExecuteAsync(Actor actor, ActorAiDecision decision, float currentTimeSeconds)
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
                phaseStateStore.TryStart(
                    actor.Id,
                    new ActorActionPhaseKey(decision.NextAction.Type, decision.NextAction.SubTypeId),
                    currentTimeSeconds);
            }

            return UniTask.CompletedTask;
        }
    }
}
