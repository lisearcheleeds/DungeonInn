using System;
using System.Collections.Generic;
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

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorAiTests
    {
        [Test]
        public void AdvanceActorAiEvaluatesHighestDirtyLayerOncePerFrame()
        {
            var actor = CreateAdventurer();
            var useCase = CreateUseCase();

            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 0f, 0, 0f).GetAwaiter().GetResult(),
                Is.True);
            Assert.That(actor.CurrentGoal.Type, Is.EqualTo(ActorGoalType.LevelUp));
            Assert.That(actor.CurrentPlan.Type, Is.EqualTo(ActorPlanType.None));
            Assert.That(actor.CurrentAction.Type, Is.EqualTo(ActorActionType.None));

            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 0.01f, 0, 0f).GetAwaiter().GetResult(),
                Is.False);

            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 0.01f, 1, 0f).GetAwaiter().GetResult(),
                Is.True);
            Assert.That(actor.CurrentPlan.Type, Is.EqualTo(ActorPlanType.Prepare));
            Assert.That(actor.CurrentAction.Type, Is.EqualTo(ActorActionType.None));

            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 0.02f, 2, 0f).GetAwaiter().GetResult(),
                Is.True);
            Assert.That(actor.CurrentAction.Type, Is.EqualTo(ActorActionType.Wait));
        }

        [Test]
        public void RuntimeStateRespectsCooldown()
        {
            var state = new ActorAiRuntimeState(Guid.NewGuid());

            Assert.That(state.CanEvaluate(10f, 1), Is.True);
            state.MarkEvaluated(10f, 1, 0.5f);

            Assert.That(state.CanEvaluate(10.1f, 1), Is.False);
            Assert.That(state.CanEvaluate(10.4f, 2), Is.False);
            Assert.That(state.CanEvaluate(10.5f, 2), Is.True);
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
            var useCase = new AdvanceActorAiOrchestrator(
                TestRuntimeServiceFactory.CreateActorDecisionScheduler(),
                new IActorAiPolicy[] { new ThrowingActorAiPolicy() },
                new ApplyActorAiDecisionUseCase());

            Assert.Throws<InvalidOperationException>(() =>
                useCase.ExecuteAsync(new[] { actor }, 1f, 1, 0.5f).GetAwaiter().GetResult());
            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 1.1f, 1, 0.5f).GetAwaiter().GetResult(),
                Is.False);
            Assert.That(
                useCase.ExecuteAsync(new[] { actor }, 1.4f, 2, 0.5f).GetAwaiter().GetResult(),
                Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                useCase.ExecuteAsync(new[] { actor }, 1.5f, 2, 0.5f).GetAwaiter().GetResult());
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
                50,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.Ground, 0, 0),
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        static AdvanceActorAiOrchestrator CreateUseCase()
        {
            return new AdvanceActorAiOrchestrator(
                TestRuntimeServiceFactory.CreateActorDecisionScheduler(),
                new IActorAiPolicy[]
                {
                    new AdventurerAiPolicy(),
                    new MonsterAiPolicy(),
                    new PetAiPolicy(),
                    new GuildStaffAiPolicy()
                },
                new ApplyActorAiDecisionUseCase());
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

