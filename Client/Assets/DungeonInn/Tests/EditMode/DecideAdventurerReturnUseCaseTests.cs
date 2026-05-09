using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Profiles;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class DecideAdventurerReturnUseCaseTests
    {
        [Test]
        public void ReachFloorGoalStartsReturningWhenActorReachesTargetFloor()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = new GameWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(2), 0f, 0f));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.ReachFloor, 2, 1, 0));
            worldState.RegisterActor(actor);

            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
            Assert.That(eventBus.GetEvents<ActorGoalCompleted>().Count, Is.EqualTo(1));
            Assert.That(eventBus.GetEvents<ActorStartedReturning>().Count, Is.EqualTo(1));
        }

        [Test]
        public void CollectItemGoalStartsReturningWhenInventoryHasTargetCount()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = new GameWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            actor.Inventory.Add(new ItemStack(1002, 2));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.CollectItem, 1002, 2, 0));
            worldState.RegisterActor(actor);

            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.CurrentGoal.ProgressCount, Is.EqualTo(2));
            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
        }

        [Test]
        public void DefeatMonsterGoalStartsReturningWhenTargetMonsterWasDefeated()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            var profileRegistry = new ActorProfileRegistry();
            using var useCase = CreateUseCase(combatService, eventBus, profileRegistry);
            var worldState = new GameWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            var monsterId = Guid.NewGuid();
            profileRegistry.RegisterMonster(monsterId, "Goblin", 1);
            actor.ChangeGoal(new ActorGoal(ActorGoalType.DefeatMonster, 1, 1, 0));
            worldState.RegisterActor(actor);

            eventBus.Publish(new ActorDefeated(monsterId, actor.Id, DeathCause.Combat));
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.CurrentGoal.ProgressCount, Is.EqualTo(1));
            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
        }

        [Test]
        public void LevelUpGoalKeepsExistingReturnAfterCombatBehavior()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = new GameWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.LevelUp, 0, 1, 0));
            worldState.RegisterActor(actor);
            combatService.MarkCombatParticipation(actor.Id);

            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
        }

        [Test]
        public void IncompleteGoalDoesNotStartReturning()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = new GameWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.CollectItem, 1002, 2, 0));
            worldState.RegisterActor(actor);

            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Exploring));
            Assert.That(eventBus.GetEvents<ActorStartedReturning>().Count, Is.EqualTo(0));
        }

        static Actor CreateExploringAdventurer(LayerPosition position)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                50,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, AdventurerLifecycleState.Exploring));
        }

        static DecideAdventurerReturnUseCase CreateUseCase(
            IActorCombatService combatService,
            IGameEventBus eventBus)
        {
            return CreateUseCase(combatService, eventBus, new ActorProfileRegistry());
        }

        static DecideAdventurerReturnUseCase CreateUseCase(
            IActorCombatService combatService,
            IGameEventBus eventBus,
            IActorProfileRegistry profileRegistry)
        {
            return new DecideAdventurerReturnUseCase(combatService, eventBus, profileRegistry);
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
                return events.OfType<T>().ToList();
            }

            public void Dispose()
            {
                subject.Dispose();
            }
        }
    }
}
