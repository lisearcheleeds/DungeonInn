using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class GameEventHistoryServiceTests
    {
        [Test]
        public void GameEventBusRecordsRecentEventsWithClockSnapshot()
        {
            var clock = new StubGameClock
            {
                CurrentScheduleTickValue = GameTimeUtility.GetDayStartTick(2) + 30
            };
            var history = new GameEventHistoryService(clock);
            using var eventBus = new GameEventBus(history);
            var firstActorId = Guid.NewGuid();
            var secondActorId = Guid.NewGuid();

            eventBus.Publish(new ActorStartedReturning(firstActorId));
            clock.CurrentScheduleTickValue = GameTimeUtility.GetDayStartTick(2) + 31;
            eventBus.Publish(new ActorStartedReturning(secondActorId));

            var entries = history.GetRecent(1);
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(GameTimeUtility.GetDay(entries[0].OccurredAtTick), Is.EqualTo(2));
            Assert.That(entries[0].OccurredAtTick, Is.EqualTo(GameTimeUtility.GetDayStartTick(2) + 31));
            Assert.That(((ActorStartedReturning)entries[0].Event).ActorId, Is.EqualTo(secondActorId));
        }

        [Test]
        public void GetGameEventHistoryUseCaseReturnsEventsByDay()
        {
            var clock = new StubGameClock { CurrentDayValue = 1 };
            var history = new GameEventHistoryService(clock);
            using var eventBus = new GameEventBus(history);
            var useCase = new GetGameEventHistoryUseCase(history);

            eventBus.Publish(new ActorStartedReturning(Guid.NewGuid()));
            clock.CurrentDayValue = 2;
            eventBus.Publish(new ActorStartedReturning(Guid.NewGuid()));

            var entries = useCase.GetByDayAsync(1).GetAwaiter().GetResult();
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(GameTimeUtility.GetDay(entries[0].OccurredAtTick), Is.EqualTo(1));
        }

        sealed class StubGameClock : IGameClock
        {
            int totalScheduleTickValue;

            public int CurrentScheduleTickValue
            {
                get => totalScheduleTickValue;
                set => totalScheduleTickValue = value;
            }

            public int CurrentDayValue
            {
                get => CurrentDay;
                set => totalScheduleTickValue = GameTimeUtility.GetDayStartTick(value);
            }

            public int TotalScheduleTick => totalScheduleTickValue;
            public int CurrentScheduleTick => totalScheduleTickValue;
            public int CurrentDay => GameTimeUtility.GetDay(totalScheduleTickValue);
            public int CurrentTickOfDay => GameTimeUtility.GetTickOfDay(totalScheduleTickValue);
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

