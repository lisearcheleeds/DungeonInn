using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class LevelTableTests
    {
        [Test]
        public void GetExperienceForLevelReturnsCorrectCumulativeXp()
        {
            var table = CreateAdventurerTable();

            Assert.That(table.GetExperienceForLevel(0), Is.EqualTo(0));
            Assert.That(table.GetExperienceForLevel(1), Is.EqualTo(10));
            Assert.That(table.GetExperienceForLevel(2), Is.EqualTo(30));
            Assert.That(table.GetExperienceForLevel(3), Is.EqualTo(60));
        }

        [Test]
        public void GetLevelReturnsCorrectLevelForXp()
        {
            var table = CreateAdventurerTable();

            Assert.That(table.GetLevel(0), Is.EqualTo(0));
            Assert.That(table.GetLevel(9), Is.EqualTo(0));
            Assert.That(table.GetLevel(10), Is.EqualTo(1));
            Assert.That(table.GetLevel(29), Is.EqualTo(1));
            Assert.That(table.GetLevel(30), Is.EqualTo(2));
            Assert.That(table.GetLevel(59), Is.EqualTo(2));
            Assert.That(table.GetLevel(60), Is.EqualTo(3));
        }

        [Test]
        public void RecalculateLevelUpdatesActorLevelFromExperience()
        {
            var table = CreateAdventurerTable();
            var actor = CreateActor(experience: 30);

            actor.RecalculateLevel(table);

            Assert.That(actor.Level, Is.EqualTo(2));
        }

        [Test]
        public void RecalculateLevelClampsToMinimumLevelOne()
        {
            var table = CreateAdventurerTable();
            var actor = CreateActor(experience: 0);

            actor.RecalculateLevel(table);

            Assert.That(actor.Level, Is.EqualTo(1));
        }

        [Test]
        public void GrantExperiencePublishesLevelUpEventWhenLevelIncreases()
        {
            var repository = new HardcodedMasterRepository();
            var eventBus = new CollectingGameEventBus();
            var useCase = new GrantExperienceUseCase(repository, eventBus);

            var killerArchetype = repository.GetActorArchetypeMaster(1);
            var levelTable = repository.GetLevelTable(killerArchetype.LevelTableId);
            var killerInitialXp = levelTable.GetExperienceForLevel(1);
            var killer = CreateActorWithArchetype(1, killerInitialXp);

            var defeatedArchetype = repository.GetActorArchetypeMaster(2);
            var monsterTable = repository.GetLevelTable(defeatedArchetype.LevelTableId);
            var defeatedInitialXp = monsterTable.GetExperienceForLevel(1);
            var defeated = CreateActorWithArchetype(2, defeatedInitialXp);

            useCase.Execute(killer, defeated);
            useCase.Execute(killer, defeated);

            var levelUps = eventBus.GetEvents<ActorLeveledUp>();
            Assert.That(levelUps.Count, Is.EqualTo(1));
            Assert.That(levelUps[0].PreviousLevel, Is.EqualTo(1));
            Assert.That(levelUps[0].NewLevel, Is.EqualTo(2));
        }

        [Test]
        public void GrantExperienceSkipsActorWithZeroArchetypeId()
        {
            var repository = new HardcodedMasterRepository();
            var eventBus = new CollectingGameEventBus();
            var useCase = new GrantExperienceUseCase(repository, eventBus);
            var killer = CreateActor(experience: 0);
            var defeated = CreateActorWithArchetype(2, 100);

            useCase.Execute(killer, defeated);

            Assert.That(eventBus.GetEvents<ExperienceGranted>(), Is.Empty);
        }

        [Test]
        public void GrantExperienceRewardIsMinimumOneWhenDefeatedHasZeroXp()
        {
            var repository = new HardcodedMasterRepository();
            var eventBus = new CollectingGameEventBus();
            var useCase = new GrantExperienceUseCase(repository, eventBus);
            var killerInitialXp = repository.GetLevelTable(repository.GetActorArchetypeMaster(1).LevelTableId).GetExperienceForLevel(1);
            var killer = CreateActorWithArchetype(1, killerInitialXp);
            var defeated = CreateActorWithArchetype(2, 0);

            useCase.Execute(killer, defeated);

            var granted = eventBus.GetEvents<ExperienceGranted>();
            Assert.That(granted.Count, Is.EqualTo(1));
            Assert.That(granted[0].GainedXp, Is.EqualTo(1));
        }

        static LevelTable CreateAdventurerTable()
        {
            var xp = new int[101];
            for (var n = 0; n <= 100; n++)
            {
                xp[n] = n * (n + 1) / 2 * 10;
            }

            return new LevelTable(1, xp);
        }

        static Actor CreateActor(int experience)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(),
                1,
                experience,
                50,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.Ground, 0, 0),
                new ActorFaction(1, "Test"),
                new AdventurerBehavior(0));
        }

        static Actor CreateActorWithArchetype(int archetypeId, int experience)
        {
            return new Actor(
                Guid.NewGuid(),
                archetypeId,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(),
                1,
                experience,
                50,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f),
                new ActorFaction(archetypeId, $"Faction {archetypeId}"),
                new AdventurerBehavior(0));
        }

        sealed class CollectingGameEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();

            public void Publish(IGameEvent gameEvent) => events.Add(gameEvent);

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
                => throw new NotSupportedException();

            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent
                => events.OfType<T>().ToList();
        }
    }
}
