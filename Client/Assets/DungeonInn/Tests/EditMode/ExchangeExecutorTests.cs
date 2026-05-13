using System;
using System.Collections.Generic;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Item;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ExchangeExecutorTests
    {
        [Test]
        public void ExecuteTransfersBothSidesAndRecordsTransaction()
        {
            var executor = new ExchangeExecutor();
            var initiator = CreateParticipant(new Inventory(new FixedItemStackLimitResolver()));
            var counterparty = CreateParticipant(new Inventory(new FixedItemStackLimitResolver()));
            initiator.Inventory.Add(new ItemStack(1001, 2));
            counterparty.Inventory.Add(new ItemStack(1, 10));

            var transaction = executor.Execute(
                initiator,
                counterparty,
                new[] { new ItemStack(1001, 2) },
                new[] { new ItemStack(1, 10) },
                123);

            Assert.That(initiator.Inventory.Has(new ItemStack(1, 10)), Is.True);
            Assert.That(initiator.Inventory.Has(new ItemStack(1001, 1)), Is.False);
            Assert.That(counterparty.Inventory.Has(new ItemStack(1001, 2)), Is.True);
            Assert.That(counterparty.Inventory.Has(new ItemStack(1, 1)), Is.False);
            Assert.That(transaction.InitiatorId, Is.EqualTo(initiator.Id));
            Assert.That(transaction.CounterpartyId, Is.EqualTo(counterparty.Id));
            Assert.That(transaction.InitiatorItems[0].ItemId, Is.EqualTo(1001));
            Assert.That(transaction.CounterpartyItems[0].Count, Is.EqualTo(10));
            Assert.That(transaction.OccurredAtTick, Is.EqualTo(123));
        }

        [Test]
        public void ExecuteAllowsSwapWhenOutgoingItemFreesSlot()
        {
            var executor = new ExchangeExecutor();
            var initiator = CreateParticipant(new Inventory(1, new FixedItemStackLimitResolver()));
            var counterparty = CreateParticipant(new Inventory(1, new FixedItemStackLimitResolver()));
            initiator.Inventory.Add(new ItemStack(1001, 1));
            counterparty.Inventory.Add(new ItemStack(2001, 1));

            executor.Execute(
                initiator,
                counterparty,
                new[] { new ItemStack(1001, 1) },
                new[] { new ItemStack(2001, 1) },
                0);

            Assert.That(initiator.Inventory.Has(new ItemStack(2001, 1)), Is.True);
            Assert.That(counterparty.Inventory.Has(new ItemStack(1001, 1)), Is.True);
        }

        [Test]
        public void ExecuteThrowsBeforeMutatingWhenPartyLacksItems()
        {
            var executor = new ExchangeExecutor();
            var initiator = CreateParticipant(new Inventory(new FixedItemStackLimitResolver()));
            var counterparty = CreateParticipant(new Inventory(new FixedItemStackLimitResolver()));
            initiator.Inventory.Add(new ItemStack(1001, 1));
            counterparty.Inventory.Add(new ItemStack(1, 10));

            Assert.Throws<InvalidOperationException>(() => executor.Execute(
                initiator,
                counterparty,
                new[] { new ItemStack(1001, 2) },
                new[] { new ItemStack(1, 10) },
                0));

            Assert.That(initiator.Inventory.Has(new ItemStack(1001, 1)), Is.True);
            Assert.That(counterparty.Inventory.Has(new ItemStack(1, 10)), Is.True);
        }

        [Test]
        public void ExecuteThrowsBeforeMutatingWhenReceiverHasNoCapacity()
        {
            var executor = new ExchangeExecutor();
            var initiator = CreateParticipant(new Inventory(1, new FixedItemStackLimitResolver()));
            var counterparty = CreateParticipant(new Inventory(new FixedItemStackLimitResolver()));
            initiator.Inventory.Add(new ItemStack(1001, 1));
            counterparty.Inventory.Add(new ItemStack(2001, 1));

            Assert.Throws<InvalidOperationException>(() => executor.Execute(
                initiator,
                counterparty,
                Array.Empty<ItemStack>(),
                new[] { new ItemStack(2001, 1) },
                0));

            Assert.That(initiator.Inventory.Has(new ItemStack(1001, 1)), Is.True);
            Assert.That(initiator.Inventory.Has(new ItemStack(2001, 1)), Is.False);
            Assert.That(counterparty.Inventory.Has(new ItemStack(2001, 1)), Is.True);
        }

        static TestExchangeParticipant CreateParticipant(Inventory inventory)
        {
            return new TestExchangeParticipant(Guid.NewGuid(), inventory);
        }

        sealed class TestExchangeParticipant : IExchangeParticipant
        {
            public TestExchangeParticipant(Guid id, Inventory inventory)
            {
                Id = id;
                Inventory = inventory;
            }

            public Guid Id { get; }
            public Inventory Inventory { get; }

            public bool HasAll(IReadOnlyList<ItemStack> items)
            {
                return Inventory.HasAll(items);
            }

            public bool CanAddAfterRemoving(IReadOnlyList<ItemStack> toRemove, IReadOnlyList<ItemStack> toAdd)
            {
                return Inventory.CanAddAfterRemoving(toRemove, toAdd);
            }

            public void RemoveRange(IReadOnlyList<ItemStack> items)
            {
                Inventory.RemoveRange(items);
            }

            public void AddRange(IReadOnlyList<ItemStack> items)
            {
                Inventory.AddRange(items);
            }
        }
    }
}
