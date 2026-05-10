using System;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;

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
        public void EquippedActorTargetsSecondFloor()
        {
            var masterRepository = new HardcodedMasterRepository();
            var useCase = CreateUseCase(masterRepository);
            var actor = CreateActor(new ActorStats(14, 14, 14, 14, 14, 14));
            actor.Equip(
                masterRepository.GetEquipmentMaster(3004),
                masterRepository.GetWeaponMaster(3004));
            actor.Equip(masterRepository.GetEquipmentMaster(3003));

            var targetFloor = useCase.ExecuteAsync(actor).GetAwaiter().GetResult();

            Assert.That(targetFloor, Is.EqualTo(2));
        }

        [Test]
        public void StrongActorTargetsThirdFloor()
        {
            var useCase = CreateUseCase();
            var actor = CreateActor(new ActorStats(30, 30, 30, 30, 30, 30));

            var targetFloor = useCase.ExecuteAsync(actor).GetAwaiter().GetResult();

            Assert.That(targetFloor, Is.EqualTo(3));
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
                new ActorCombatPowerCalculator(masterRepository));
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
                new AdventurerBehavior(0));
        }
    }
}
