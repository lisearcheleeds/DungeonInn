using System;
using System.Collections.Generic;
using DungeonInn.Application.AI;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorAiTests
    {
        [Test]
        public void AdvanceActorAiEvaluatesHighestDirtyLayerOncePerTick()
        {
            var actor = CreateAdventurer();
            var useCase = new AdvanceActorAiUseCase();

            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 0, 0).GetAwaiter().GetResult(),
                Is.True);
            Assert.That(actor.CurrentGoal.Type, Is.EqualTo(ActorGoalType.LevelUp));
            Assert.That(actor.CurrentPlan.Type, Is.EqualTo(ActorPlanType.None));
            Assert.That(actor.CurrentAction.Type, Is.EqualTo(ActorActionType.None));

            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 0, 0).GetAwaiter().GetResult(),
                Is.False);

            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 1, 0).GetAwaiter().GetResult(),
                Is.True);
            Assert.That(actor.CurrentPlan.Type, Is.EqualTo(ActorPlanType.Prepare));
            Assert.That(actor.CurrentAction.Type, Is.EqualTo(ActorActionType.None));

            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 2, 0).GetAwaiter().GetResult(),
                Is.True);
            Assert.That(actor.CurrentAction.Type, Is.EqualTo(ActorActionType.Wait));
        }

        [Test]
        public void RuntimeStateRespectsCooldown()
        {
            var state = new ActorAiRuntimeState(Guid.NewGuid());

            Assert.That(state.CanEvaluate(10), Is.True);
            state.MarkEvaluated(10, 5);

            Assert.That(state.CanEvaluate(10), Is.False);
            Assert.That(state.CanEvaluate(14), Is.False);
            Assert.That(state.CanEvaluate(15), Is.True);
        }

        [Test]
        public void EventDirtyMapperMapsMeaningEventsToLayerDirty()
        {
            var mapper = new ActorAiEventDirtyMapper();

            Assert.That(
                mapper.Map(ActorAiEventType.EnemyEnteredRange),
                Is.EqualTo(ActorAiDirtyFlags.ShortTerm));
            Assert.That(
                mapper.Map(ActorAiEventType.ObjectiveItemCountChanged),
                Is.EqualTo(ActorAiDirtyFlags.LongTerm | ActorAiDirtyFlags.MidTerm));
        }

        [Test]
        public void AdvanceActorAiMarksEvaluatedWhenPolicyThrows()
        {
            var actor = CreateAdventurer();
            var useCase = new AdvanceActorAiUseCase(
                new ActorDecisionScheduler(),
                new IActorAiPolicy[] { new ThrowingActorAiPolicy() },
                new ApplyActorAiDecisionUseCase());

            Assert.Throws<InvalidOperationException>(() =>
                useCase.ExecuteAsync(new[] { actor }, 1, 5).GetAwaiter().GetResult());
            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 1, 5).GetAwaiter().GetResult(),
                Is.False);
            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 5, 5).GetAwaiter().GetResult(),
                Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                useCase.ExecuteAsync(new[] { actor }, 6, 5).GetAwaiter().GetResult());
        }

        static Actor CreateAdventurer()
        {
            return new Actor(
                Guid.NewGuid(),
                "Adventurer",
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(),
                1,
                0,
                50,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.Ground, 0, 0),
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0));
        }

        sealed class ThrowingActorAiPolicy : IActorAiPolicy
        {
            public bool CanHandle(Actor actor)
            {
                return true;
            }

            public ActorAiDecision EvaluateLongTerm(ActorAiContext context)
            {
                throw new InvalidOperationException("AI policy failed.");
            }

            public ActorAiDecision EvaluateMidTerm(ActorAiContext context)
            {
                throw new InvalidOperationException("AI policy failed.");
            }

            public ActorAiDecision EvaluateShortTerm(ActorAiContext context)
            {
                throw new InvalidOperationException("AI policy failed.");
            }
        }
    }
}
