using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
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
    public sealed class ActorEffectUseCaseTests
    {
        [Test]
        public void UsePotionAppliesHealOverTimeActorEffectAndConsumesItem()
        {
            var repository = new HardcodedMasterRepository();
            var useCase = new UseConsumableItemUseCase(
                repository,
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService());
            var actor = CreateAdventurer(20);
            actor.GainItem(new ItemStack(2001, 1));

            var used = useCase.ExecuteAsync(actor, 2001).GetAwaiter().GetResult();

            Assert.That(used, Is.True);
            Assert.That(actor.Inventory.ItemCounts.ContainsKey(2001), Is.False);
            Assert.That(actor.ActorEffects.Count, Is.EqualTo(1));
            Assert.That(actor.ActorEffects[0].ActorEffectMasterId, Is.EqualTo(1));
        }

        [Test]
        public void PotionHealsThirtyHpOverTenSeconds()
        {
            var repository = new HardcodedMasterRepository();
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var useItemUseCase = new UseConsumableItemUseCase(repository, candidateService);
            var advanceUseCase = new AdvanceActorEffectsUseCase(candidateService);
            var worldState = CreateWorldState(candidateService);
            var actor = CreateAdventurer(20);
            actor.GainItem(new ItemStack(2001, 1));
            worldState.RegisterActor(actor);

            useItemUseCase.ExecuteAsync(actor, 2001).GetAwaiter().GetResult();
            advanceUseCase.ExecuteAsync(worldState, 5f).GetAwaiter().GetResult();

            Assert.That(actor.Hp, Is.EqualTo(35));

            advanceUseCase.ExecuteAsync(worldState, 5f).GetAwaiter().GetResult();

            Assert.That(actor.Hp, Is.EqualTo(50));
            Assert.That(actor.ActorEffects.Count, Is.EqualTo(0));
        }

        [Test]
        public void ReusingPotionAppendsDurationAndHealAmount()
        {
            var repository = new HardcodedMasterRepository();
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var useItemUseCase = new UseConsumableItemUseCase(repository, candidateService);
            var advanceUseCase = new AdvanceActorEffectsUseCase(candidateService);
            var worldState = CreateWorldState(candidateService);
            var actor = CreateAdventurer(0);
            actor.GainItem(new ItemStack(2001, 2));
            worldState.RegisterActor(actor);

            useItemUseCase.ExecuteAsync(actor, 2001).GetAwaiter().GetResult();
            advanceUseCase.ExecuteAsync(worldState, 1f).GetAwaiter().GetResult();
            useItemUseCase.ExecuteAsync(actor, 2001).GetAwaiter().GetResult();
            advanceUseCase.ExecuteAsync(worldState, 19f).GetAwaiter().GetResult();

            Assert.That(actor.Hp, Is.EqualTo(60));
            Assert.That(actor.ActorEffects.Count, Is.EqualTo(0));
        }

        [Test]
        public void HighPotionHealsSixtyHpOverTenSeconds()
        {
            var repository = new HardcodedMasterRepository();
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var useItemUseCase = new UseConsumableItemUseCase(repository, candidateService);
            var advanceUseCase = new AdvanceActorEffectsUseCase(candidateService);
            var worldState = CreateWorldState(candidateService);
            var actor = CreateAdventurer(0);
            actor.GainItem(new ItemStack(2002, 1));
            worldState.RegisterActor(actor);

            useItemUseCase.ExecuteAsync(actor, 2002).GetAwaiter().GetResult();
            advanceUseCase.ExecuteAsync(worldState, 10f).GetAwaiter().GetResult();

            Assert.That(actor.Hp, Is.EqualTo(60));
            Assert.That(actor.ActorEffects.Count, Is.EqualTo(0));
        }

        [Test]
        public void LowHpExploringAdventurerUsesRecoveryItemAutomatically()
        {
            var repository = new HardcodedMasterRepository();
            var eventBus = new CollectingEventBus();
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var useConsumableItemUseCase = new UseConsumableItemUseCase(repository, candidateService);
            var useRecoveryItemUseCase = new UseRecoveryItemOrchestrator(
                useConsumableItemUseCase,
                eventBus,
                candidateService,
                new FixedWorldGameSettingsRepository(),
                new RecoveryItemSelectionPolicy(repository));
            var worldState = CreateWorldState(candidateService);
            var actor = CreateAdventurer(30);
            actor.GainItem(new ItemStack(2001, 1));
            worldState.RegisterActor(actor);

            useRecoveryItemUseCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(actor.Inventory.ItemCounts.ContainsKey(2001), Is.False);
            Assert.That(actor.ActorEffects.Count, Is.EqualTo(1));
            var aiEvents = eventBus.GetEvents<ActorAiDecisionRecorded>();
            Assert.That(aiEvents.Count, Is.EqualTo(1));
            Assert.That(aiEvents[0].DecisionType, Is.EqualTo(AiDecisionType.UseRecoveryItem));
            Assert.That(aiEvents[0].ReasonType, Is.EqualTo(AiDecisionReasonType.LowHpWithRecoveryItem));
        }

        [Test]
        public void RecoveryItemSelectionChoosesLeastWasteRecoveryItem()
        {
            var repository = new HardcodedMasterRepository();
            var policy = new RecoveryItemSelectionPolicy(repository);
            var actor = CreateAdventurer(40);
            actor.GainItem(new ItemStack(2001, 1));
            actor.GainItem(new ItemStack(2002, 1));

            var selectedItemId = policy.SelectItemId(actor);

            Assert.That(selectedItemId, Is.EqualTo(2001));
        }

        [Test]
        public void RecoveryItemSelectionChoosesHighPotionWhenOnlyHighPotionExists()
        {
            var repository = new HardcodedMasterRepository();
            var policy = new RecoveryItemSelectionPolicy(repository);
            var actor = CreateAdventurer(5);
            actor.GainItem(new ItemStack(2002, 1));

            var selectedItemId = policy.SelectItemId(actor);

            Assert.That(selectedItemId, Is.EqualTo(2002));
        }

        static Actor CreateAdventurer(int hp)
        {
            return new Actor(
                Guid.NewGuid(),
                1,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new HardcodedMasterRepository()),
                1,
                0,
                hp,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.DungeonFloor(1), 0f, 0f),
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, AdventurerLifecycleState.Exploring),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static GameWorldState CreateWorldState(ActorProcessingCandidateService candidateService)
        {
            return new GameWorldState(
                new ActorSpatialIndexService(new FixedWorldGameSettingsRepository()),
                new ItemSpatialIndexService(new FixedWorldGameSettingsRepository()),
                candidateService,
                ActorViewDataStoreTestFactory.Create(),
                new FixedWorldGameSettingsRepository());
        }

        sealed class CollectingEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();

            public void Publish(IGameEvent gameEvent)
            {
                events.Add(gameEvent);
            }

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return Observable.Empty<T>();
            }

            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent
            {
                return events.OfType<T>().ToArray();
            }
        }
    }
}

