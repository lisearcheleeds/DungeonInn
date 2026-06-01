using System;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class PlayerEventLogFormatterTests
    {
        [Test]
        public void InnRecoveryLogsOnlyStartAndEnd()
        {
            var actorId = Guid.NewGuid();
            var profileRegistry = new ActorProfileRegistry();
            profileRegistry.Register(actorId, "アリス", 1, 0, ActorBehaviorType.Adventurer, 0);
            var formatter = new PlayerEventLogFormatter(profileRegistry, new HardcodedMasterRepository());

            Assert.That(
                formatter.Format(new ActorReservedInn(actorId, Guid.NewGuid())),
                Is.EqualTo("アリス は宿屋で休み始めた"));
            Assert.That(
                formatter.Format(new ActorRecoveringAtInn(actorId, 7, 10)),
                Is.Empty);
            Assert.That(
                formatter.Format(new ActorFullyRecovered(actorId)),
                Is.EqualTo("アリス は宿屋での回復を終えた"));
        }

        [Test]
        public void PlayerEventLogStoreIgnoresPerTickInnRecoveryEvent()
        {
            var actorId = Guid.NewGuid();
            var clock = new GameClock();
            var eventBus = new GameEventBus(new GameEventHistoryService(clock));
            var profileRegistry = new ActorProfileRegistry();
            profileRegistry.Register(actorId, "アリス", 1, 0, ActorBehaviorType.Adventurer, 0);
            var store = new PlayerEventLogStore(
                eventBus,
                new PlayerEventLogFormatter(profileRegistry, new HardcodedMasterRepository()),
                clock);

            try
            {
                store.Initialize();

                eventBus.Publish(new ActorReservedInn(actorId, Guid.NewGuid()));
                eventBus.Publish(new ActorRecoveringAtInn(actorId, 7, 10));
                eventBus.Publish(new ActorFullyRecovered(actorId));

                var entries = store.GetRecentEntries(10);
                Assert.That(entries.Count, Is.EqualTo(2));
                Assert.That(entries[0].Text, Is.EqualTo("アリス は宿屋で休み始めた"));
                Assert.That(entries[1].Text, Is.EqualTo("アリス は宿屋での回復を終えた"));
            }
            finally
            {
                store.Dispose();
                eventBus.Dispose();
            }
        }
    }
}
