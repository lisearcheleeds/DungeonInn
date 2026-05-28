using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Actors.Phase;
using DungeonInn.Domain.Actor;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorActionPhaseStateStoreTests
    {
        [Test]
        public void PhaseKeyCanBeUsedAsDictionaryKey()
        {
            var subTypeId = Guid.NewGuid();
            var key = new ActorActionPhaseKey(ActorActionType.Attack, subTypeId);
            var dictionary = new Dictionary<ActorActionPhaseKey, string>
            {
                { key, "attack" }
            };

            Assert.That(dictionary[new ActorActionPhaseKey(ActorActionType.Attack, subTypeId)], Is.EqualTo("attack"));
        }

        [Test]
        public void AttackActionStoresOptionalSubTypeId()
        {
            var subTypeId = Guid.NewGuid();
            var action = ActorAction.Attack(10, subTypeId);

            Assert.That(action.Type, Is.EqualTo(ActorActionType.Attack));
            Assert.That(action.TargetId, Is.EqualTo(10));
            Assert.That(action.SubTypeId, Is.EqualTo(subTypeId));
        }

        [Test]
        public void TryStartReturnsFalseWhenPhaseSequenceDoesNotExist()
        {
            var store = CreateStore();
            var actorId = Guid.NewGuid();
            var key = new ActorActionPhaseKey(ActorActionType.Move, null);

            var started = store.TryStart(actorId, key, 0f);

            Assert.That(started, Is.False);
            Assert.That(store.IsActive(actorId), Is.False);
        }

        [Test]
        public void TryStartActivatesAttackPhaseSequence()
        {
            var store = CreateStore();
            var actorId = Guid.NewGuid();

            var started = store.TryStart(actorId, AttackKey(), 0f);

            Assert.That(started, Is.True);
            Assert.That(store.IsActive(actorId), Is.True);
        }

        [Test]
        public void TickAllReportsEffectTransitionAfterWindUpDuration()
        {
            var store = CreateStore();
            var actorId = Guid.NewGuid();
            store.TryStart(actorId, AttackKey(), 0f);

            var result = store.TickAll(0.2f);
            var phaseNames = result.Transitions.Select(transition => transition.PhaseDef.PhaseName).ToArray();

            Assert.That(phaseNames, Does.Contain(ActorActionPhaseName.WindUp));
            Assert.That(phaseNames, Does.Contain(ActorActionPhaseName.Effect));
            Assert.That(phaseNames, Does.Contain(ActorActionPhaseName.Recovery));
            Assert.That(result.CompletedActorIds, Is.Empty);
            Assert.That(store.IsActive(actorId), Is.True);
        }

        [Test]
        public void TickAllCompletesAndRemovesActorAfterFinalPhaseDuration()
        {
            var store = CreateStore();
            var actorId = Guid.NewGuid();
            store.TryStart(actorId, AttackKey(), 0f);

            var result = store.TickAll(1f);

            Assert.That(result.CompletedActorIds, Is.EqualTo(new[] { actorId }));
            Assert.That(store.IsActive(actorId), Is.False);
        }

        [Test]
        public void InterruptRemovesActiveActor()
        {
            var store = CreateStore();
            var actorId = Guid.NewGuid();
            store.TryStart(actorId, AttackKey(), 0f);

            store.Interrupt(actorId);

            Assert.That(store.IsActive(actorId), Is.False);
            Assert.That(store.TickAll(1f).Transitions, Is.Empty);
        }

        static ActorActionPhaseStateStore CreateStore()
        {
            return new ActorActionPhaseStateStore(new HardcodedActorActionPhaseMasterRepository());
        }

        static ActorActionPhaseKey AttackKey()
        {
            return new ActorActionPhaseKey(ActorActionType.Attack, null);
        }
    }
}
