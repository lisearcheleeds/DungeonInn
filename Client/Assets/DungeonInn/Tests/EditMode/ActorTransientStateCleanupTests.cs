using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorTransientStateCleanupTests
    {
        [Test]
        public void ActorDecisionSchedulerRemovesRuntimeStateWhenActorEnds()
        {
            using var eventBus = new CollectingGameEventBus();
            using var scheduler = new ActorDecisionScheduler(eventBus);
            var defeatedActorId = Guid.NewGuid();
            var departedActorId = Guid.NewGuid();
            var defeatedState = scheduler.GetOrCreateState(defeatedActorId);
            var departedState = scheduler.GetOrCreateState(departedActorId);

            eventBus.Publish(new ActorDefeated(defeatedActorId, null, DeathCause.Combat));
            eventBus.Publish(new ActorDeparted(departedActorId, 1));

            Assert.That(scheduler.GetOrCreateState(defeatedActorId), Is.Not.SameAs(defeatedState));
            Assert.That(scheduler.GetOrCreateState(departedActorId), Is.Not.SameAs(departedState));
        }

        [Test]
        public void ExplorationAndRecoveryStateServicesCleanupWhenActorEnds()
        {
            using var eventBus = new CollectingGameEventBus();
            using var explorationState = new AdventurerExplorationStateService(eventBus);
            using var recoveryState = new AdventurerRecoveryStateService(eventBus);
            var defeatedActorId = Guid.NewGuid();
            var departedActorId = Guid.NewGuid();
            var destination = new LayerPosition(MapLayerId.DungeonFloor(1), 1f, 2f);
            explorationState.SetDestination(defeatedActorId, destination);
            explorationState.SetDestination(departedActorId, destination);
            recoveryState.SetAccumulatedHp(defeatedActorId, 3f);
            recoveryState.SetAccumulatedHp(departedActorId, 4f);

            eventBus.Publish(new ActorDefeated(defeatedActorId, null, DeathCause.Combat));
            eventBus.Publish(new ActorDeparted(departedActorId, 1));

            Assert.That(explorationState.TryGetDestination(defeatedActorId, out _), Is.False);
            Assert.That(explorationState.TryGetDestination(departedActorId, out _), Is.False);
            Assert.That(recoveryState.GetAccumulatedHp(defeatedActorId), Is.EqualTo(0f));
            Assert.That(recoveryState.GetAccumulatedHp(departedActorId), Is.EqualTo(0f));
        }

        [Test]
        public void ExplorationAchievementRegistryResetsAndCleansUpWithoutRemovingProfile()
        {
            using var eventBus = new CollectingGameEventBus();
            var profileRegistry = new ActorProfileRegistry();
            using var achievementRegistry = new ActorExplorationAchievementRegistry(eventBus);
            using var returnTrackingService = new AdventurerReturnTrackingService(
                eventBus,
                profileRegistry,
                achievementRegistry);
            var adventurerId = Guid.NewGuid();
            var monsterId = Guid.NewGuid();
            profileRegistry.Register(adventurerId, "Adventurer", 1, 0, ActorBehaviorType.Adventurer);
            profileRegistry.Register(monsterId, "Slime", 2, 5, ActorBehaviorType.Monster);

            eventBus.Publish(new ActorDefeated(monsterId, adventurerId, DeathCause.Combat));

            Assert.That(returnTrackingService.GetDefeatedMonsterCount(adventurerId, 5), Is.EqualTo(1));

            eventBus.Publish(new ActorEnteredDungeon(adventurerId, 1));

            Assert.That(returnTrackingService.GetDefeatedMonsterCount(adventurerId, 5), Is.EqualTo(0));

            eventBus.Publish(new ActorDefeated(monsterId, adventurerId, DeathCause.Combat));
            eventBus.Publish(new ActorDeparted(adventurerId, 1));

            Assert.That(returnTrackingService.GetDefeatedMonsterCount(adventurerId, 5), Is.EqualTo(0));
            Assert.That(profileRegistry.TryGetProfile(adventurerId, out var profile), Is.True);
            Assert.That(profile.DisplayName, Is.EqualTo("Adventurer"));
        }

        sealed class CollectingGameEventBus : IGameEventBus, IDisposable
        {
            readonly Subject<IGameEvent> subject = new();
            readonly List<IGameEvent> events = new();

            public void Publish(IGameEvent gameEvent)
            {
                events.Add(gameEvent);
                subject.OnNext(gameEvent);
            }

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return subject.Where(gameEvent => gameEvent is T).Select(gameEvent => (T)(object)gameEvent);
            }

            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent
            {
                return events.OfType<T>().ToArray();
            }

            public void Dispose()
            {
                subject.Dispose();
            }
        }
    }
}
