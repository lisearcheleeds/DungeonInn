using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
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
    public sealed class SelectDungeonTargetFloorUseCaseTests
    {
        [Test]
        public void WeakActorTargetsFirstFloor()
        {
            var useCase = CreateUseCase();
            var actor = CreateActor(new ActorStats(5, 5, 5, 5, 5, 5));

            var targetFloor = useCase.ExecuteAsync(actor).GetAwaiter().GetResult();

            Assert.That(targetFloor, Is.EqualTo(1));
        }

        [Test]
        public void EquippedActorTargetsDeepDepthBand()
        {
            var masterRepository = new HardcodedMasterRepository();
            var useCase = CreateUseCase(masterRepository);
            var actor = CreateActor(new ActorStats(14, 14, 14, 14, 14, 14));
            actor.Equip(
                masterRepository.GetEquipmentMaster(3004),
                masterRepository.GetWeaponMaster(3004));
            actor.Equip(masterRepository.GetEquipmentMaster(3003));

            var targetFloor = useCase.ExecuteAsync(actor).GetAwaiter().GetResult();

            Assert.That(targetFloor, Is.EqualTo(8));
        }

        [Test]
        public void StrongActorTargetsEndlessDepthBand()
        {
            var useCase = CreateUseCase();
            var actor = CreateActor(new ActorStats(30, 30, 30, 30, 30, 30));

            var targetFloor = useCase.ExecuteAsync(actor).GetAwaiter().GetResult();

            Assert.That(targetFloor, Is.EqualTo(12));
        }

        [Test]
        public void SelectingTargetFloorPublishesAiReason()
        {
            var eventBus = new CollectingEventBus();
            var masterRepository = new HardcodedMasterRepository();
            var useCase = new SelectDungeonTargetFloorUseCase(
                masterRepository,
                new ActorCombatPowerCalculator(),
                eventBus);
            var actor = CreateActor(new ActorStats(30, 30, 30, 30, 30, 30));

            var targetFloor = useCase.ExecuteAsync(actor).GetAwaiter().GetResult();

            Assert.That(targetFloor, Is.EqualTo(12));
            var aiEvents = eventBus.GetEvents<ActorAiDecisionRecorded>();
            Assert.That(aiEvents.Count, Is.EqualTo(1));
            Assert.That(aiEvents[0].DecisionType, Is.EqualTo(AiDecisionType.SelectDungeonFloor));
            Assert.That(aiEvents[0].ReasonType, Is.EqualTo(AiDecisionReasonType.CombatPowerMatchesFloor));
            Assert.That(aiEvents[0].SelectedFloor, Is.EqualTo(12));
        }

        [Test]
        public void AlwaysSelectsAtLeastFirstFloor()
        {
            var useCase = CreateUseCase();
            var actor = CreateActor(new ActorStats(1, 1, 1, 1, 1, 1));

            var targetFloor = useCase.ExecuteAsync(actor).GetAwaiter().GetResult();

            Assert.That(targetFloor, Is.EqualTo(1));
        }

        static SelectDungeonTargetFloorUseCase CreateUseCase()
        {
            return CreateUseCase(new HardcodedMasterRepository());
        }

        static SelectDungeonTargetFloorUseCase CreateUseCase(HardcodedMasterRepository masterRepository)
        {
            return new SelectDungeonTargetFloorUseCase(
                masterRepository,
                new ActorCombatPowerCalculator(),
                new CollectingEventBus());
        }

        static Actor CreateActor(ActorStats stats)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                stats,
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                50,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.Ground, 0f, 0f),
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
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

