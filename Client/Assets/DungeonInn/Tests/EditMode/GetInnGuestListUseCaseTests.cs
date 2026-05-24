using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class GetInnGuestListUseCaseTests
    {
        [Test]
        public void ExecuteReturnsOnlyRecoveringAdventurers()
        {
            var recoveringActor = CreateAdventurer(AdventurerLifecycleState.Recovering, hp: 5);
            var waitingActor = CreateAdventurer(AdventurerLifecycleState.WaitingForInn, hp: 10);
            var reader = new TestWorldStateReader(true, new[] { recoveringActor, waitingActor });
            var repository = new HardcodedMasterRepository();
            using var recoveryStateService = new AdventurerRecoveryStateService(TestEventSubscriber.Instance);
            recoveryStateService.SetAccumulatedHp(recoveringActor.Id, 0.5f);
            var useCase = new GetInnGuestListUseCase(
                reader,
                repository,
                recoveryStateService,
                new FixedWorldGameSettingsRepository());
            var guests = new List<InnGuestSummary>();

            useCase.Execute(guests);

            Assert.That(guests.Count, Is.EqualTo(1));
            Assert.That(guests[0].ActorId, Is.EqualTo(recoveringActor.Id));
            Assert.That(guests[0].Name, Is.EqualTo(repository.GetActorArchetypeMaster(1).Name));
            Assert.That(guests[0].HpRatio, Is.EqualTo((float)recoveringActor.Hp / recoveringActor.Params.MaxHp).Within(0.001f));
            var expectedRemainingSeconds =
                (recoveringActor.Params.MaxHp - recoveringActor.Hp - 0.5f)
                / (recoveringActor.Params.MaxHp * InnBalanceSettings.CreateDefault().HpRecoveryPercentPerMinute / 60f);
            Assert.That(guests[0].RecoveryRemainingSeconds, Is.EqualTo(expectedRemainingSeconds).Within(0.001f));
        }

        [Test]
        public void ExecuteReturnsEmptyWhenWorldIsNotInitialized()
        {
            var reader = new TestWorldStateReader(false, Array.Empty<Actor>());
            using var recoveryStateService = new AdventurerRecoveryStateService(TestEventSubscriber.Instance);
            var useCase = new GetInnGuestListUseCase(
                reader,
                new HardcodedMasterRepository(),
                recoveryStateService,
                new FixedWorldGameSettingsRepository());
            var guests = new List<InnGuestSummary>();

            useCase.Execute(guests);

            Assert.That(guests.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteReturnsZeroRemainingSecondsWhenRecoveryRateIsZero()
        {
            var recoveringActor = CreateAdventurer(AdventurerLifecycleState.Recovering, hp: 5);
            var reader = new TestWorldStateReader(true, new[] { recoveringActor });
            using var recoveryStateService = new AdventurerRecoveryStateService(TestEventSubscriber.Instance);
            var settingsRepository = new FixedWorldGameSettingsRepository(
                innBalanceSettings: new InnBalanceSettings(0f, 10, 3, 2, -2, -1));
            var useCase = new GetInnGuestListUseCase(
                reader,
                new HardcodedMasterRepository(),
                recoveryStateService,
                settingsRepository);
            var guests = new List<InnGuestSummary>();

            useCase.Execute(guests);

            Assert.That(guests[0].RecoveryRemainingSeconds, Is.EqualTo(0f));
        }

        static Actor CreateAdventurer(AdventurerLifecycleState lifecycleState, int hp)
        {
            return new Actor(
                Guid.NewGuid(),
                1,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                hp,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.Ground, 0f, 0f),
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, lifecycleState),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        sealed class TestWorldStateReader : IGameWorldStateReader
        {
            readonly IReadOnlyList<Actor> actors;

            public TestWorldStateReader(bool isInitialized, IReadOnlyList<Actor> actors)
            {
                IsInitialized = isInitialized;
                this.actors = actors ?? throw new ArgumentNullException(nameof(actors));
            }

            public bool IsInitialized { get; }
            public IReadOnlyList<Actor> Actors => actors;
            public AdventurerGuild Guild => throw new NotSupportedException();
            public GroundMap GroundMap => throw new NotSupportedException();
            public Dungeon Dungeon => throw new NotSupportedException();
            public InnEconomyState InnEconomy => throw new NotSupportedException();
            public IReadOnlyList<ItemInstance> Items => throw new NotSupportedException();
            public IReadOnlyList<ProjectileInstance> Projectiles => throw new NotSupportedException();
            public IReadOnlyList<AreaEffectInstance> AreaEffects => throw new NotSupportedException();
            public SpawnScheduleState SpawnSchedule => throw new NotSupportedException();

            public Actor FindActor(Guid actorId)
            {
                throw new NotSupportedException();
            }
        }
    }
}

