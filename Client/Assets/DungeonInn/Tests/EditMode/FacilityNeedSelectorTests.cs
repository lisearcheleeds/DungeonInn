using System;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class FacilityNeedSelectorTests
    {
        [Test]
        public void GeneralStoreNeedRequiresExecutablePurchaseWhenOnlyPotionIsNeeded()
        {
            var selector = CreateSelector();
            var actor = CreateAdventurer();
            var generalStore = CreateFacility(FacilityType.GeneralStore);
            var guild = CreateGuild(generalStore);

            Assert.That(selector.TrySelectFacility(guild, actor, out _), Is.False);

            actor.GainItem(new ItemStack(SpecialItemIds.Money, 999));

            Assert.That(selector.TrySelectFacility(guild, actor, out _), Is.False);

            generalStore = CreateFacility(FacilityType.GeneralStore, new ItemStack(SpecialItemIds.Potion, 1));
            guild = CreateGuild(generalStore);

            Assert.That(selector.TrySelectFacility(guild, actor, out var facility), Is.True);
            Assert.That(facility, Is.EqualTo(generalStore));
        }

        [Test]
        public void InnNeedIgnoresActorAlreadyQueuedForInn()
        {
            var selector = CreateSelector();
            var actor = CreateAdventurer();
            actor.ReceiveDamage(1);
            var inn = CreateFacility(FacilityType.Inn);
            var guild = CreateGuild(inn);
            guild.EnqueueInnReservation(actor, inn.Id);

            Assert.That(selector.TrySelectFacility(guild, actor, out _), Is.False);
        }

        [Test]
        public void SellableGeneralStoreItemTakesPriorityOverInnNeed()
        {
            var selector = CreateSelector();
            var actor = CreateAdventurer();
            actor.ReceiveDamage(1);
            actor.GainItem(new ItemStack(1003, 1));
            var generalStore = CreateFacility(
                FacilityType.GeneralStore,
                new ItemStack(SpecialItemIds.Money, 1000));
            var inn = CreateFacility(FacilityType.Inn);
            var guild = CreateGuild(inn, generalStore);

            Assert.That(selector.TrySelectFacility(guild, actor, out var facility), Is.True);
            Assert.That(facility, Is.EqualTo(generalStore));
        }

        static FacilityNeedSelector CreateSelector()
        {
            var masterRepository = new HardcodedMasterRepository();
            return new FacilityNeedSelector(
                masterRepository,
                new GetFacilityLineupUseCase(masterRepository, masterRepository));
        }

        static AdventurerGuild CreateGuild(params Facility[] facilities)
        {
            return new AdventurerGuild(
                Guid.NewGuid(),
                new Inventory(new FixedItemStackLimitResolver()),
                facilities);
        }

        static Facility CreateFacility(FacilityType facilityType, params ItemStack[] initialItems)
        {
            var inventory = new Inventory(new FixedItemStackLimitResolver());
            inventory.AddRange(initialItems);
            return new Facility(
                Guid.NewGuid(),
                facilityType,
                facilityType.ToString(),
                1,
                1,
                inventory);
        }

        static Actor CreateAdventurer()
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                10,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.Ground, 0f, 0f),
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, AdventurerLifecycleState.Recovering),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }
    }
}
