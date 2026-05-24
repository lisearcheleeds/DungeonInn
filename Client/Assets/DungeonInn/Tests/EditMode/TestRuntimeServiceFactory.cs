using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;

namespace DungeonInn.Tests.EditMode
{
    internal static class TestRuntimeServiceFactory
    {
        public static ActorProcessingCandidateService CreateActorProcessingCandidateService()
        {
            return new ActorProcessingCandidateService(TestEventSubscriber.Instance);
        }

        public static ActorDecisionScheduler CreateActorDecisionScheduler()
        {
            return new ActorDecisionScheduler(TestEventSubscriber.Instance);
        }
    }
}
