using System;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.GameLoop;

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

        public static AdventurerDeathRevivalService CreateAdventurerDeathRevivalService()
        {
            return new AdventurerDeathRevivalService(
                new FixedGameClock(),
                CreateActorProcessingCandidateService());
        }

        sealed class FixedGameClock : IGameClock
        {
            public int TotalScheduleTick => 0;
            public int CurrentScheduleTick => 0;
            public int CurrentDay => 0;
            public int CurrentTickOfDay => 0;
            public float ElapsedRealTimeSeconds => 0f;
            public float ElapsedGameTimeSeconds => 0f;
            public float TimeScale => 1f;
            public bool IsPaused => false;

            public void SetTimeScale(float timeScale)
            {
            }

            public void Pause()
            {
            }

            public void Resume()
            {
            }

            public GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds)
            {
                return new GameClockAdvanceResult(0, Array.Empty<int>());
            }
        }
    }
}
