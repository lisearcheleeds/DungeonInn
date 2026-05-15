using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;

using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;












using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class AdvanceAreaEffectUseCaseTests
    {
        [Test]
        public void InstantAreaHitsEnemiesInRadiusAndIgnoresAllies()
        {
            var spatialIndex = new ActorSpatialIndexService();
            var worldState = CreateWorldState(spatialIndex);
            var combatService = new ActorCombatService();
            var eventBus = new CollectingGameEventBus();
            var useCase = new AdvanceAreaEffectUseCase(
                new AttackAreaTargetResolver(spatialIndex),
                CreateCombatEffectExecutor(combatService, eventBus),
                CreateActorDefeatOrchestrator(combatService, eventBus),
                eventBus);
            var attacker = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var enemyA = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 50);
            var enemyB = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 7f, 5f), 50);
            var ally = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 6f), 50);
            var outsideEnemy = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 12f, 5f), 50);
            var areaEffect = new AreaEffectInstance(
                Guid.NewGuid(),
                attacker.Id,
                attacker.Faction.Id,
                new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f),
                new AttackAreaSpec(
                    AttackAreaShape.Circle,
                    AttackAreaDurationType.Instant,
                    AttackHitIntervalType.OncePerTarget,
                    0f,
                    0f,
                    3f,
                    0f,
                    0),
                8);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(enemyA);
            worldState.RegisterActor(enemyB);
            worldState.RegisterActor(ally);
            worldState.RegisterActor(outsideEnemy);
            worldState.AddAreaEffect(areaEffect);

            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            var hits = eventBus.GetEvents<AreaEffectHit>();
            var publishedEvents = eventBus.GetEvents();
            Assert.That(worldState.AreaEffects.Count, Is.EqualTo(0));
            Assert.That(enemyA.Hp, Is.EqualTo(42));
            Assert.That(enemyB.Hp, Is.EqualTo(42));
            Assert.That(ally.Hp, Is.EqualTo(50));
            Assert.That(outsideEnemy.Hp, Is.EqualTo(50));
            Assert.That(hits.Count, Is.EqualTo(2));
            Assert.That(publishedEvents.Select(gameEvent => gameEvent.GetType()).ToArray(), Is.EqualTo(new[]
            {
                typeof(AreaEffectHit),
                typeof(CombatAttackOccurred),
                typeof(AreaEffectHit),
                typeof(CombatAttackOccurred)
            }));
        }

        [Test]
        public void InstantAreaUsesSpatialIndexCandidates()
        {
            var spatialIndex = new ActorSpatialIndexService();
            var resolver = new AttackAreaTargetResolver(spatialIndex);
            var attacker = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var indexedEnemy = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 50);
            var unindexedEnemy = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 7f, 5f), 50);
            var areaEffect = new AreaEffectInstance(
                Guid.NewGuid(),
                attacker.Id,
                attacker.Faction.Id,
                new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f),
                new AttackAreaSpec(
                    AttackAreaShape.Circle,
                    AttackAreaDurationType.Instant,
                    AttackHitIntervalType.OncePerTarget,
                    0f,
                    0f,
                    3f,
                    0f,
                    0),
                8);
            spatialIndex.SyncActor(attacker);
            spatialIndex.SyncActor(indexedEnemy);

            var targets = resolver.ResolveTargets(areaEffect);

            Assert.That(targets.Any(actor => actor.Id.Equals(indexedEnemy.Id)), Is.True);
            Assert.That(targets.Any(actor => actor.Id.Equals(unindexedEnemy.Id)), Is.False);
        }

        sealed class CollectingGameEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();

            public void Publish(IGameEvent gameEvent) => events.Add(gameEvent);

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
                => throw new NotSupportedException();

            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent
                => events.OfType<T>().ToList();

            public IReadOnlyList<IGameEvent> GetEvents()
            {
                return events.ToArray();
            }
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

        sealed class ZeroGameRandom : IGameRandom
        {
            public int Next() => 0;
            public int Next(int maxExclusive) => 0;
            public int Next(int minInclusive, int maxExclusive) => minInclusive;
        }

        static GrantExperienceUseCase CreateGrantExperienceUseCase(IGameEventBus eventBus)
        {
            return new GrantExperienceUseCase(new HardcodedMasterRepository(), eventBus);
        }

        static DropItemUseCase CreateDropItemUseCase(IGameEventBus eventBus)
        {
            return new DropItemUseCase(new ZeroGameRandom(), eventBus);
        }

        static CombatEffectExecutor CreateCombatEffectExecutor(
            IActorCombatService combatService,
            IGameEventBus eventBus)
        {
            return new CombatEffectExecutor(new CombatDamageResolver(combatService));
        }

        static ActorDefeatOrchestrator CreateActorDefeatOrchestrator(
            IActorCombatService combatService,
            IGameEventBus eventBus)
        {
            return new ActorDefeatOrchestrator(
                new CombatDefeatResolver(combatService),
                CreateGrantExperienceUseCase(eventBus),
                CreateDropItemUseCase(eventBus));
        }

        static GameWorldState CreateWorldState(ActorSpatialIndexService actorSpatialIndexService)
        {
            return new GameWorldState(
                actorSpatialIndexService,
                new ItemSpatialIndexService(),
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                new ActorViewDataStore());
        }

        static Actor CreateActor(int factionId, LayerPosition position, int hp)
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
