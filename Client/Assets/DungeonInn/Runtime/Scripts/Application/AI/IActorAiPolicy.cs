using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.AI
{
    public interface IActorAiPolicy
    {
        bool CanHandle(Actor actor);
        ActorAiDecision EvaluateLongTerm(ActorAiContext context);
        ActorAiDecision EvaluateMidTerm(ActorAiContext context);
        ActorAiDecision EvaluateShortTerm(ActorAiContext context);
    }
}
