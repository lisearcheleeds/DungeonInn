using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Event;
using R3;

namespace DungeonInn.Tests.EditMode
{
    internal sealed class TestEventSubscriber : IEventSubscriber
    {
        public static readonly TestEventSubscriber Instance = new();

        TestEventSubscriber()
        {
        }

        public Observable<T> OnEvent<T>() where T : class, IGameEvent
        {
            return Observable.Empty<T>();
        }
    }

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
