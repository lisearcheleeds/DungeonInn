using System;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class InventoryTests
    {
        [Test]
        public void DefaultInventoryUsesConfiguredSlotCapacity()
        {
            var inventory = new Inventory(new FixedItemStackLimitResolver());

            Assert.That(inventory.MaxSlotCount, Is.EqualTo(GameConstants.DefaultInventorySlotCapacity));
        }

        [Test]
        public void AddThrowsWhenNewItemExceedsSlotCapacity()
        {
            var inventory = new Inventory(1, new FixedItemStackLimitResolver());
            inventory.Add(new ItemStack(1001, 1));

            Assert.Throws<InvalidOperationException>(() => inventory.Add(new ItemStack(1002, 1)));
        }

        [Test]
        public void AddStacksExistingItemWhenInventoryIsFull()
        {
            var inventory = new Inventory(1, new FixedItemStackLimitResolver());
            inventory.Add(new ItemStack(1001, 1));

            inventory.Add(new ItemStack(1001, 2));

            Assert.That(inventory.Has(new ItemStack(1001, 3)), Is.True);
            Assert.That(inventory.UsedSlotCount, Is.EqualTo(1));
        }

        [Test]
        public void CanAddAllChecksNewSlotsBeforeMutatingInventory()
        {
            var inventory = new Inventory(2, new FixedItemStackLimitResolver());
            inventory.Add(new ItemStack(1001, 1));

            Assert.That(
                inventory.CanAddAll(new[] { new ItemStack(1002, 1), new ItemStack(1003, 1) }),
                Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                inventory.AddRange(new[] { new ItemStack(1002, 1), new ItemStack(1003, 1) }));
            Assert.That(inventory.Has(new ItemStack(1002, 1)), Is.False);
            Assert.That(inventory.Has(new ItemStack(1003, 1)), Is.False);
        }

        [Test]
        public void AddSplitsItemIntoMultipleSlotsWhenStackLimitIsExceeded()
        {
            var inventory = new Inventory(10, new FixedItemStackLimitResolver(10));

            inventory.Add(new ItemStack(1001, 18));

            Assert.That(inventory.Slots.Count, Is.EqualTo(2));
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo(1001));
            Assert.That(inventory.Slots[0].Count, Is.EqualTo(10));
            Assert.That(inventory.Slots[1].ItemId, Is.EqualTo(1001));
            Assert.That(inventory.Slots[1].Count, Is.EqualTo(8));
            Assert.That(inventory.ItemCounts[1001], Is.EqualTo(18));
        }

        [Test]
        public void AddUsesPartialExistingStackBeforeCreatingNewSlot()
        {
            var inventory = new Inventory(10, new FixedItemStackLimitResolver(10));
            inventory.Add(new ItemStack(1001, 8));

            inventory.Add(new ItemStack(1001, 5));

            Assert.That(inventory.Slots.Count, Is.EqualTo(2));
            Assert.That(inventory.Slots[0].Count, Is.EqualTo(10));
            Assert.That(inventory.Slots[1].Count, Is.EqualTo(3));
            Assert.That(inventory.ItemCounts[1001], Is.EqualTo(13));
        }

        [Test]
        public void CanAddReturnsFalseWhenOverflowRequiresUnavailableSlot()
        {
            var inventory = new Inventory(1, new FixedItemStackLimitResolver(10));
            inventory.Add(new ItemStack(1001, 10));

            Assert.That(inventory.CanAdd(new ItemStack(1001, 1)), Is.False);
            Assert.Throws<InvalidOperationException>(() => inventory.Add(new ItemStack(1001, 1)));
        }

        [Test]
        public void RemoveConsumesSlotsAndUpdatesItemCountCache()
        {
            var inventory = new Inventory(10, new FixedItemStackLimitResolver(10));
            inventory.Add(new ItemStack(1001, 18));

            inventory.Remove(new ItemStack(1001, 12));

            Assert.That(inventory.Slots.Count, Is.EqualTo(1));
            Assert.That(inventory.Slots[0].Count, Is.EqualTo(6));
            Assert.That(inventory.ItemCounts[1001], Is.EqualTo(6));
        }

        [Test]
        public void GuildAndFacilityExposeReadOnlyInventory()
        {
            Assert.That(
                typeof(AdventurerGuild).GetProperty(nameof(AdventurerGuild.Inventory))?.PropertyType,
                Is.EqualTo(typeof(IReadOnlyInventory)));
            Assert.That(
                typeof(Facility).GetProperty(nameof(Facility.Inventory))?.PropertyType,
                Is.EqualTo(typeof(IReadOnlyInventory)));
        }
    }
}

