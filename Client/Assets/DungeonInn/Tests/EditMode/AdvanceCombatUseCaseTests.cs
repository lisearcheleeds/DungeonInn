using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class AdvanceCombatUseCaseTests
    {
        [Test]
        public void DirectAttackDealsDamageWhenTargetIsInWeaponRange()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = new GameWorldState();
            var combatService = new ActorCombatService();
            var eventBus = new CollectingGameEventBus();
            var useCase = new AdvanceCombatUseCase(combatService, clock, eventBus, CreateGrantExperienceUseCase(eventBus));
            var attacker = CreateActor("Attacker", 1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var target = CreateActor("Target", 2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 50);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.SetTarget(attacker.Id, target.Id);

            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            var attacks = eventBus.GetEvents<CombatAttackOccurred>();
            Assert.That(attacks.Count, Is.EqualTo(1));
            Assert.That(attacks[0].Damage, Is.EqualTo(attacker.WeaponCombatParams.AttackSpec.Nodes[0].DamageSpec.Amount));
            Assert.That(target.Hp, Is.EqualTo(50 - attacks[0].Damage));
        }

        [Test]
        public void AttackCooldownPreventsRepeatedAttackUntilEnoughGameSecondsPass()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = new GameWorldState();
            var combatService = new ActorCombatService();
            var eventBus = new CollectingGameEventBus();
            var useCase = new AdvanceCombatUseCase(combatService, clock, eventBus, CreateGrantExperienceUseCase(eventBus));
            var attacker = CreateActor("Attacker", 1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var target = CreateActor("Target", 2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 50);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.SetTarget(attacker.Id, target.Id);

            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();
            var hpAfterFirstAttack = target.Hp;
            eventBus.Clear();

            clock.ElapsedGameTimeSeconds = 0.1f;
            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            var attacksAfterCooldown = eventBus.GetEvents<CombatAttackOccurred>();
            Assert.That(attacksAfterCooldown.Count, Is.EqualTo(0));
            Assert.That(target.Hp, Is.EqualTo(hpAfterFirstAttack));
        }

        [Test]
        public void DefeatedTargetIsRemovedFromWorldAndCombatTargetsAreCleared()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = new GameWorldState();
            var combatService = new ActorCombatService();
            var eventBus = new CollectingGameEventBus();
            var useCase = new AdvanceCombatUseCase(combatService, clock, eventBus, CreateGrantExperienceUseCase(eventBus));
            var attacker = CreateActor("Attacker", 1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var target = CreateActor("Target", 2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 1);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.SetTarget(attacker.Id, target.Id);
            combatService.SetTarget(target.Id, attacker.Id);

            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            var deaths = eventBus.GetEvents<ActorDefeated>();
            Assert.That(deaths.Count, Is.EqualTo(1));
            Assert.That(deaths[0].ActorId, Is.EqualTo(target.Id));
            Assert.That(deaths[0].KillerActorId, Is.EqualTo(attacker.Id));
            Assert.That(deaths[0].Cause, Is.EqualTo(DeathCause.Combat));
            Assert.That(worldState.Actors.Any(x => x.Id.Equals(target.Id)), Is.False);
            Assert.That(combatService.HasTarget(attacker.Id), Is.False);
        }

        sealed class CollectingGameEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();

            public void Publish(IGameEvent gameEvent) => events.Add(gameEvent);

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
                => throw new NotSupportedException();

            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent
                => events.OfType<T>().ToList();

            public void Clear() => events.Clear();
        }

        sealed class ThrowingMasterRepository : IMasterRepository
        {
            public IReadOnlyDictionary<int, ItemMaster> ItemMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, EquipmentMaster> EquipmentMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, WeaponMaster> WeaponMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> WeaponTypeCombatMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, ActorArchetypeMaster> ActorArchetypeMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, MonsterSpeciesMaster> MonsterSpeciesMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, LevelTable> LevelTables => throw new NotSupportedException();
            public ItemMaster GetItemMaster(int itemId) => throw new NotSupportedException();
            public EquipmentMaster GetEquipmentMaster(int itemId) => throw new NotSupportedException();
            public WeaponMaster GetWeaponMaster(int itemId) => throw new NotSupportedException();
            public WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType) => throw new NotSupportedException();
            public ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId) => throw new NotSupportedException();
            public MonsterSpeciesMaster GetMonsterSpeciesMaster(int speciesId) => throw new NotSupportedException();
            public SpawnTableMaster GetSpawnTableMaster(int spawnTableId) => throw new NotSupportedException();
            public LevelTable GetLevelTable(int levelTableId) => throw new NotSupportedException();
        }

        sealed class FakeGameClock : IGameClock
        {
            public int CurrentScheduleTick => 0;
            public int CurrentDay => 0;
            public float ElapsedRealTimeSeconds => ElapsedGameTimeSeconds;
            public float ElapsedGameTimeSeconds { get; set; }
            public float TimeScale => 1f;
            public void SetTimeScale(float timeScale) { }
            public GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds)
                => new GameClockAdvanceResult(0, false);
        }

        static GrantExperienceUseCase CreateGrantExperienceUseCase(IGameEventBus eventBus)
        {
            return new GrantExperienceUseCase(new ThrowingMasterRepository(), eventBus);
        }

        static Actor CreateActor(string name, int factionId, LayerPosition position, int hp)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(),
                1,
                0,
                hp,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(factionId, $"Faction {factionId}"),
                new AdventurerBehavior(0));
        }
    }
}
