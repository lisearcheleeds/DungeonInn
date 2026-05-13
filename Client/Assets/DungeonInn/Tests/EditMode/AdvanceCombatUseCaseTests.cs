using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Orchestration;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
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
            var useCase = CreateAdvanceCombatUseCase(combatService, clock, eventBus);
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
            var useCase = CreateAdvanceCombatUseCase(combatService, clock, eventBus);
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
            var useCase = CreateAdvanceCombatUseCase(combatService, clock, eventBus);
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

        [Test]
        public void AreaAttackCreatesAreaEffectWithoutImmediateDamage()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = new GameWorldState();
            var combatService = new ActorCombatService();
            var eventBus = new CollectingGameEventBus();
            var useCase = CreateAdvanceCombatUseCase(combatService, clock, eventBus);
            var attacker = CreateActor("Attacker", 1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var target = CreateActor("Target", 2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 50);
            attacker.ChangeNaturalWeaponType(WeaponTypeCombatMasterCatalog.Get(WeaponType.Scythe));
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.SetTarget(attacker.Id, target.Id);

            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            var createdAreas = eventBus.GetEvents<AreaEffectCreated>();
            Assert.That(worldState.AreaEffects.Count, Is.EqualTo(1));
            Assert.That(createdAreas.Count, Is.EqualTo(1));
            Assert.That(worldState.AreaEffects[0].AttackerActorId, Is.EqualTo(attacker.Id));
            Assert.That(worldState.AreaEffects[0].CenterPosition, Is.EqualTo(target.Position));
            Assert.That(worldState.AreaEffects[0].AreaSpec.Shape, Is.EqualTo(AttackAreaShape.Circle));
            Assert.That(target.Hp, Is.EqualTo(50));
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
            public IReadOnlyDictionary<int, AdventurerSpawnMaster> AdventurerSpawnMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, ActorEffectMaster> ActorEffectMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, SpeciesMaster> SpeciesMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, SpawnTableMaster> SpawnTableMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<int, LevelTable> LevelTables => throw new NotSupportedException();
            public IReadOnlyDictionary<int, DungeonFloorExplorationMaster> DungeonFloorExplorationMasters => throw new NotSupportedException();
            public ItemMaster GetItemMaster(int itemId) => throw new NotSupportedException();
            public EquipmentMaster GetEquipmentMaster(int itemId) => throw new NotSupportedException();
            public WeaponMaster GetWeaponMaster(int itemId) => throw new NotSupportedException();
            public WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType) => throw new NotSupportedException();
            public ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId) => throw new NotSupportedException();
            public AdventurerSpawnMaster GetAdventurerSpawnMaster(int adventurerSpawnId) => throw new NotSupportedException();
            public ActorEffectMaster GetActorEffectMaster(int actorEffectId) => throw new NotSupportedException();
            public SpeciesMaster GetSpeciesMaster(int speciesId) => throw new NotSupportedException();
            public SpawnTableMaster GetSpawnTableMaster(int spawnTableId) => throw new NotSupportedException();
            public LevelTable GetLevelTable(int levelTableId) => throw new NotSupportedException();
            public DungeonFloorExplorationMaster GetDungeonFloorExplorationMaster(int floorIndex) => throw new NotSupportedException();
            public int GetMaxStackCount(int itemId) => throw new NotSupportedException();
        }

        sealed class FakeGameClock : IGameClock
        {
            public int TotalScheduleTick => 0;
            public int CurrentScheduleTick => 0;
            public int CurrentDay => 0;
            public int CurrentTickOfDay => 0;
            public float ElapsedRealTimeSeconds => ElapsedGameTimeSeconds;
            public float ElapsedGameTimeSeconds { get; set; }
            public float TimeScale => 1f;
            public bool IsPaused => false;
            public void SetTimeScale(float timeScale) { }
            public void Pause() { }
            public void Resume() { }
            public GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds)
                => new GameClockAdvanceResult(0, Array.Empty<int>());
        }

        static GrantExperienceService CreateGrantExperienceService(IGameEventBus eventBus)
        {
            return new GrantExperienceService(new ThrowingMasterRepository(), eventBus);
        }

        static DropItemService CreateDropItemService(IGameEventBus eventBus)
        {
            return new DropItemService(new ZeroGameRandom(), eventBus);
        }

        static CombatEffectExecutor CreateCombatEffectExecutor(
            IActorCombatService combatService,
            IGameEventBus eventBus)
        {
            return new CombatEffectExecutor(
                eventBus,
                new CombatDamageResolver(combatService, eventBus));
        }

        static ActorDefeatOrchestrator CreateActorDefeatOrchestrator(
            IActorCombatService combatService,
            IGameEventBus eventBus)
        {
            return new ActorDefeatOrchestrator(
                new CombatDefeatResolver(combatService, eventBus),
                CreateGrantExperienceService(eventBus),
                CreateDropItemService(eventBus));
        }

        static AdvanceCombatUseCase CreateAdvanceCombatUseCase(
            IActorCombatService combatService,
            IGameClock clock,
            IGameEventBus eventBus)
        {
            return new AdvanceCombatUseCase(
                combatService,
                clock,
                CreateCombatEffectExecutor(combatService, eventBus),
                CreateActorDefeatOrchestrator(combatService, eventBus));
        }

        sealed class ZeroGameRandom : IGameRandom
        {
            public int Next() => 0;
            public int Next(int maxExclusive) => 0;
            public int Next(int minInclusive, int maxExclusive) => minInclusive;
        }

        static Actor CreateActor(string name, int factionId, LayerPosition position, int hp)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                0,
                hp,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(factionId, $"Faction {factionId}"),
                new AdventurerBehavior(0),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }
    }
}
