using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;


using DungeonInn.Application.Actors.Movement;

using DungeonInn.Application.Actors.Spawn;

using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
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
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(2), 0f, 0f));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.ReachFloor, 2, 1, 0));
            worldState.RegisterActor(actor);

            useCase.RecordAdventureStart(actor);
            eventBus.Publish(new ActorEnteredDungeon(actor.Id, 2));
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
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            useCase.RecordAdventureStart(actor);
            actor.GainItem(new ItemStack(1002, 2));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.CollectItem, 1002, 2, 0));
            worldState.RegisterActor(actor);

            eventBus.Publish(new ItemPickedUp(actor.Id, CreateItemInstance(1002, actor.Position)));
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
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            var monsterId = Guid.NewGuid();
            profileRegistry.Register(monsterId, "Goblin", 2, 1, ActorBehaviorType.Monster, 0);
            actor.ChangeGoal(new ActorGoal(ActorGoalType.DefeatMonster, 1, 1, 0));
            worldState.RegisterActor(actor);

            useCase.RecordAdventureStart(actor);
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
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.LevelUp, 0, 1, 0));
            worldState.RegisterActor(actor);

            useCase.RecordAdventureStart(actor);
            actor.GainExperience(1);
            eventBus.Publish(new CombatEncounterEnded(actor.Id));
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
        }

        [Test]
        public void EarnMoneyGoalStartsReturningWhenSellableLootValueReachesTarget()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.EarnMoney, 0, 12, 0));
            worldState.RegisterActor(actor);

            useCase.RecordAdventureStart(actor);
            actor.GainItem(new ItemStack(1002, 1));
            eventBus.Publish(new ItemPickedUp(actor.Id, CreateItemInstance(1002, actor.Position)));
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.CurrentGoal.ProgressCount, Is.EqualTo(12));
            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
        }

        [Test]
        public void IncompleteGoalDoesNotStartReturning()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.CollectItem, 1002, 2, 0));
            worldState.RegisterActor(actor);

            useCase.RecordAdventureStart(actor);
            eventBus.Publish(new ActorEnteredDungeon(actor.Id, 1));
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Exploring));
            Assert.That(eventBus.GetEvents<ActorStartedReturning>().Count, Is.EqualTo(0));
        }

        [Test]
        public void DamagedAboveLowHpThresholdDoesNotStartReturning()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), 40);
            actor.ChangeGoal(new ActorGoal(ActorGoalType.CollectItem, 1002, 2, 0));
            worldState.RegisterActor(actor);

            useCase.RecordAdventureStart(actor);
            eventBus.Publish(new CombatEncounterEnded(actor.Id));
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Exploring));
            Assert.That(eventBus.GetEvents<ActorStartedReturning>().Count, Is.EqualTo(0));
        }

        [Test]
        public void LowHpWithoutRecoveryItemStartsReturning()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), 39);
            actor.ChangeGoal(new ActorGoal(ActorGoalType.CollectItem, 1002, 2, 0));
            worldState.RegisterActor(actor);

            useCase.RecordAdventureStart(actor);
            eventBus.Publish(new CombatEncounterEnded(actor.Id));
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
            Assert.That(eventBus.GetEvents<ActorGoalCompleted>().Count, Is.EqualTo(0));
            Assert.That(eventBus.GetEvents<ActorStartedReturning>().Count, Is.EqualTo(1));
            var aiEvents = eventBus.GetEvents<ActorAiDecisionRecorded>();
            Assert.That(aiEvents.Count, Is.EqualTo(1));
            Assert.That(aiEvents[0].DecisionType, Is.EqualTo(AiDecisionType.ReturnToInn));
            Assert.That(aiEvents[0].ReasonType, Is.EqualTo(AiDecisionReasonType.LowHpWithoutRecoveryItem));
        }

        [Test]
        public void LowHpWithRecoveryItemDoesNotStartReturning()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), 39);
            actor.GainItem(new ItemStack(2001, 1));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.CollectItem, 1002, 2, 0));
            worldState.RegisterActor(actor);

            useCase.RecordAdventureStart(actor);
            eventBus.Publish(new CombatEncounterEnded(actor.Id));
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Exploring));
            Assert.That(eventBus.GetEvents<ActorStartedReturning>().Count, Is.EqualTo(0));
        }

        [Test]
        public void CriticalHpStartsReturningEvenWithRecoveryItem()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), 19);
            actor.GainItem(new ItemStack(2001, 1));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.CollectItem, 1002, 2, 0));
            worldState.RegisterActor(actor);

            useCase.RecordAdventureStart(actor);
            eventBus.Publish(new CombatEncounterEnded(actor.Id));
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
            Assert.That(eventBus.GetEvents<ActorStartedReturning>().Count, Is.EqualTo(1));
            var aiEvents = eventBus.GetEvents<ActorAiDecisionRecorded>();
            Assert.That(aiEvents.Count, Is.EqualTo(1));
            Assert.That(aiEvents[0].ReasonType, Is.EqualTo(AiDecisionReasonType.CriticalHp));
        }

        [Test]
        public void CompletedGoalDoesNotStartReturningWithoutDirtyEvent()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(2), 0f, 0f));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.ReachFloor, 2, 1, 0));
            worldState.RegisterActor(actor);

            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Exploring));
            Assert.That(eventBus.GetEvents<ActorStartedReturning>().Count, Is.EqualTo(0));
        }

        [Test]
        public void DirtyReturnDecisionWaitsUntilCombatTargetIsCleared()
        {
            var combatService = new ActorCombatService();
            using var eventBus = new CollectingGameEventBus();
            using var useCase = CreateUseCase(combatService, eventBus);
            var worldState = CreateWorldState();
            var actor = CreateExploringAdventurer(new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f), 19);
            var monster = CreateMonster(new LayerPosition(MapLayerId.DungeonFloor(1), 1f, 0f));
            actor.ChangeGoal(new ActorGoal(ActorGoalType.CollectItem, 1002, 2, 0));
            worldState.RegisterActor(actor);
            worldState.RegisterActor(monster);
            combatService.SetTarget(actor.Id, monster.Id);

            useCase.RecordAdventureStart(actor);
            eventBus.Publish(new CombatEncounterEnded(actor.Id));
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Exploring));

            combatService.ClearTarget(actor.Id);
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.RequireBehavior<AdventurerBehavior>().LifecycleState, Is.EqualTo(AdventurerLifecycleState.Returning));
        }

        static Actor CreateExploringAdventurer(LayerPosition position, int hp = 50)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                hp,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, AdventurerLifecycleState.Exploring),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static Actor CreateMonster(LayerPosition position)
        {
            return new Actor(
                Guid.NewGuid(),
                2,
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
                new ActorFaction(2, "Monster"),
                new MonsterBehavior(1),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static DecideAdventurerReturnUseCaseFixture CreateUseCase(
            IActorCombatService combatService,
            IGameEventBus eventBus)
        {
            return CreateUseCase(combatService, eventBus, new ActorProfileRegistry());
        }

        static DecideAdventurerReturnUseCaseFixture CreateUseCase(
            IActorCombatService combatService,
            IGameEventBus eventBus,
            IActorProfileRegistry profileRegistry)
        {
            var achievementRegistry = new ActorExplorationAchievementRegistry(eventBus);
            var trackingService = new AdventurerReturnTrackingService(eventBus, profileRegistry, achievementRegistry);
            var masterRepository = new HardcodedMasterRepository();
            var goalProgressService = new AdventureGoalProgressService(
                combatService,
                achievementRegistry,
                masterRepository);
            var useCase = new DecideAdventurerReturnUseCase(
                combatService,
                eventBus,
                trackingService,
                goalProgressService,
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                new FixedWorldGameSettingsRepository(),
                new RecoveryItemCandidateQuery(masterRepository),
                new RecoveryEffectEstimator());
            return new DecideAdventurerReturnUseCaseFixture(useCase, trackingService, achievementRegistry);
        }

        static ItemInstance CreateItemInstance(int itemId, LayerPosition position)
        {
            return new ItemInstance(Guid.NewGuid(), new ItemStack(itemId, 1), position);
        }

        static GameWorldState CreateWorldState()
        {
            return new GameWorldState(
                new ActorSpatialIndexService(new FixedWorldGameSettingsRepository()),
                new ItemSpatialIndexService(new FixedWorldGameSettingsRepository()),
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                ActorViewDataStoreTestFactory.Create(),
                new FixedWorldGameSettingsRepository());
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

        sealed class DecideAdventurerReturnUseCaseFixture : IDisposable
        {
            readonly DecideAdventurerReturnUseCase useCase;
            readonly AdventurerReturnTrackingService trackingService;
            readonly ActorExplorationAchievementRegistry achievementRegistry;

            public DecideAdventurerReturnUseCaseFixture(
                DecideAdventurerReturnUseCase useCase,
                AdventurerReturnTrackingService trackingService,
                ActorExplorationAchievementRegistry achievementRegistry)
            {
                this.useCase = useCase;
                this.trackingService = trackingService;
                this.achievementRegistry = achievementRegistry;
            }

            public UniTask ExecuteAsync(IGameWorldState worldState)
            {
                return useCase.ExecuteAsync(worldState);
            }

            public void RecordAdventureStart(Actor actor)
            {
                achievementRegistry.RecordAdventureStart(actor);
            }

            public void Dispose()
            {
                trackingService.Dispose();
                achievementRegistry.Dispose();
            }
        }
    }
}


