using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using R3;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;

namespace DungeonInn.Tests.EditMode
{
    public sealed class AdvanceCombatUseCaseTests
    {
        [Test]
        public void DirectAttackDealsDamageWhenTargetIsInWeaponRange()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = CreateWorldState();
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
            var events = eventBus.GetEvents();
            Assert.That(attacks.Count, Is.EqualTo(1));
            Assert.That(attacks[0].Damage, Is.EqualTo(attacker.WeaponCombatParams.AttackSpec.Nodes[0].DamageSpec.Amount));
            Assert.That(target.Hp, Is.EqualTo(50 - attacks[0].Damage));
            Assert.That(events.Select(gameEvent => gameEvent.GetType()).ToArray(), Is.EqualTo(new[]
            {
                typeof(CombatAttackOccurred)
            }));
        }

        [Test]
        public void AttackCooldownPreventsRepeatedAttackUntilEnoughGameSecondsPass()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = CreateWorldState();
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
            var worldState = CreateWorldState();
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
        public void DefeatEventsArePublishedAfterDefeatTransactionCompletes()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = CreateWorldState();
            var combatService = new ActorCombatService();
            var eventBus = new CollectingGameEventBus();
            var useCase = CreateAdvanceCombatUseCase(combatService, clock, eventBus);
            var attacker = CreateActor("Attacker", 1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 50);
            var target = CreateActor("Target", 2, new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 1);
            var observedDefeatEvent = false;
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.SetTarget(attacker.Id, target.Id);
            combatService.SetTarget(target.Id, attacker.Id);
            eventBus.OnPublish = gameEvent =>
            {
                if (gameEvent is not ActorDefeated)
                {
                    return;
                }

                observedDefeatEvent = true;
                Assert.That(worldState.FindActor(target.Id), Is.Null);
                Assert.That(combatService.HasTarget(attacker.Id), Is.False);
                Assert.That(combatService.HasTarget(target.Id), Is.False);
            };

            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            Assert.That(observedDefeatEvent, Is.True);
        }

        [Test]
        public void DefeatDropAndRewardEventsArePublishedInOrder()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = CreateWorldState();
            var combatService = new ActorCombatService();
            var eventBus = new CollectingGameEventBus();
            var useCase = CreateAdvanceCombatUseCase(combatService, clock, eventBus);
            var attacker = CreateActor(
                "Attacker",
                1,
                new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f),
                50,
                1);
            var target = CreateDroppingMonster(new LayerPosition(MapLayerId.DungeonFloor(1), 6f, 5f), 1);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.SetTarget(attacker.Id, target.Id);
            combatService.SetTarget(target.Id, attacker.Id);

            useCase.ExecuteAsync(worldState, 0f).GetAwaiter().GetResult();

            var events = eventBus.GetEvents();
            Assert.That(events.Select(gameEvent => gameEvent.GetType()).ToArray(), Is.EqualTo(new[]
            {
                typeof(CombatAttackOccurred),
                typeof(CombatEncounterEnded),
                typeof(ActorDefeated),
                typeof(ItemDropped),
                typeof(ExperienceGranted)
            }));
        }

        [Test]
        public void AreaAttackCreatesAreaEffectWithoutImmediateDamage()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = CreateWorldState();
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

        [Test]
        public void CombatApproachUsesNavigationPathInsteadOfDirectWallCrossing()
        {
            var clock = new FakeGameClock { ElapsedGameTimeSeconds = 0f };
            var worldState = CreateWorldState(false);
            worldState.Initialize(
                CreateGuild(),
                CreateGroundMap(),
                CreateDungeonWithWall(new GridPosition(1, 1)));
            var combatService = new ActorCombatService();
            var eventBus = new CollectingGameEventBus();
            var useCase = CreateAdvanceCombatUseCase(combatService, clock, eventBus);
            var attacker = CreateActor(
                "Attacker",
                1,
                new LayerPosition(MapLayerId.DungeonFloor(1), 0.5f, 1.5f),
                50);
            var target = CreateActor(
                "Target",
                2,
                new LayerPosition(MapLayerId.DungeonFloor(1), 2.5f, 1.5f),
                50);
            worldState.RegisterActor(attacker);
            worldState.RegisterActor(target);
            combatService.SetTarget(attacker.Id, target.Id);

            useCase.ExecuteAsync(worldState, 1f).GetAwaiter().GetResult();

            Assert.That(attacker.Position.X, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(attacker.Position.Z, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(combatService.HasTarget(attacker.Id), Is.True);
            Assert.That(eventBus.GetEvents<CombatAttackOccurred>().Count, Is.EqualTo(0));
        }

        sealed class CollectingGameEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();
            public Action<IGameEvent> OnPublish { get; set; }

            public void Publish(IGameEvent gameEvent)
            {
                events.Add(gameEvent);
                OnPublish?.Invoke(gameEvent);
            }

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                throw new NotSupportedException();
            }

            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent
            {
                return events.OfType<T>().ToList();
            }

            public IReadOnlyList<IGameEvent> GetEvents()
            {
                return events.ToArray();
            }

            public void Clear()
            {
                events.Clear();
            }
        }

        sealed class NoOpGameEventBus : IGameEventBus
        {
            public void Publish(IGameEvent gameEvent)
            {
            }

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return Observable.Empty<T>();
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
            public IReadOnlyDictionary<int, DungeonDepthBandMaster> DungeonDepthBandMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<string, EnvironmentPropVisualMaster> EnvironmentPropVisualMasters => throw new NotSupportedException();
            public IReadOnlyDictionary<string, ActorVisualMaster> ActorVisualMasters => throw new NotSupportedException();
            public ItemMaster GetItemMaster(int itemId)
            {
                throw new NotSupportedException();
            }

            public EquipmentMaster GetEquipmentMaster(int itemId)
            {
                throw new NotSupportedException();
            }

            public WeaponMaster GetWeaponMaster(int itemId)
            {
                throw new NotSupportedException();
            }

            public WeaponTypeCombatMaster GetWeaponTypeCombatMaster(WeaponType weaponType)
            {
                throw new NotSupportedException();
            }

            public ActorArchetypeMaster GetActorArchetypeMaster(int archetypeId)
            {
                throw new NotSupportedException();
            }

            public AdventurerSpawnMaster GetAdventurerSpawnMaster(int adventurerSpawnId)
            {
                throw new NotSupportedException();
            }

            public ActorEffectMaster GetActorEffectMaster(int actorEffectId)
            {
                throw new NotSupportedException();
            }

            public SpeciesMaster GetSpeciesMaster(int speciesId)
            {
                throw new NotSupportedException();
            }

            public SpawnTableMaster GetSpawnTableMaster(int spawnTableId)
            {
                throw new NotSupportedException();
            }

            public LevelTable GetLevelTable(int levelTableId)
            {
                throw new NotSupportedException();
            }

            public DungeonDepthBandMaster GetDungeonDepthBandMasterForFloor(int floorIndex)
            {
                throw new NotSupportedException();
            }

            public EnvironmentPropVisualMaster GetEnvironmentPropVisualMaster(string key)
            {
                throw new NotSupportedException();
            }

            public ActorVisualMaster GetActorVisualMaster(string visualId, int skinId)
            {
                throw new NotSupportedException();
            }

            public int GetMaxStackCount(int itemId)
            {
                throw new NotSupportedException();
            }
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
            {
                return new GameClockAdvanceResult(0, Array.Empty<int>());
            }
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

        static AdvanceCombatUseCase CreateAdvanceCombatUseCase(
            IActorCombatService combatService,
            IGameClock clock,
            IGameEventBus eventBus)
        {
            var spatialIndex = new ActorSpatialIndexService(new FixedWorldGameSettingsRepository());
            var actorViewDataStore = ActorViewDataStoreTestFactory.Create();
            var navigationService = new ActorNavigationService(
                new NoOpGameEventBus(),
                new NoOpNavigationPathProvider());
            var settingsRepository = new FixedWorldGameSettingsRepository();
            return new AdvanceCombatUseCase(
                combatService,
                clock,
                new GameWorldFrameBuffer(),
                CreateCombatEffectExecutor(combatService, eventBus),
                CreateActorDefeatOrchestrator(combatService, eventBus),
                eventBus,
                new ActorMovementService(
                    navigationService,
                    spatialIndex,
                    actorViewDataStore),
                settingsRepository,
                new CombatEncounterTargetResolver(clock, spatialIndex, settingsRepository));
        }

        static GameWorldState CreateWorldState()
        {
            return CreateWorldState(true);
        }

        static GameWorldState CreateWorldState(bool initialize)
        {
            var worldState = new GameWorldState(
                new ActorSpatialIndexService(new FixedWorldGameSettingsRepository()),
                new ItemSpatialIndexService(new FixedWorldGameSettingsRepository()),
                TestRuntimeServiceFactory.CreateActorProcessingCandidateService(),
                ActorViewDataStoreTestFactory.Create(),
                new FixedWorldGameSettingsRepository());
            if (initialize)
            {
                worldState.Initialize(CreateGuild(), CreateGroundMap(), CreateDungeonWithWall(new GridPosition(-1, -1)));
            }

            return worldState;
        }

        static AdventurerGuild CreateGuild()
        {
            return new AdventurerGuild(
                Guid.NewGuid(),
                new Inventory(new FixedItemStackLimitResolver()),
                Array.Empty<Facility>());
        }

        static GroundMap CreateGroundMap()
        {
            var layer = new MapLayer(MapLayerId.Ground, 1, 1, 1f);
            return new GroundMap(
                layer,
                new GridPosition(0, 0),
                new[] { new GroundCell(new GridPosition(0, 0), GroundCellType.Open, MapCellBlockType.Walkable) });
        }

        static Dungeon CreateDungeonWithWall(GridPosition wall)
        {
            var dungeon = new Dungeon(1);
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 80, 20, 1f);
            var cells = new DungeonCell[layer.Width * layer.Depth];
            for (var z = 0; z < layer.Depth; z++)
            {
                for (var x = 0; x < layer.Width; x++)
                {
                    var position = new GridPosition(x, z);
                    cells[z * layer.Width + x] = new DungeonCell(
                        position,
                        position.Equals(wall) ? DungeonCellType.Wall : DungeonCellType.Room);
                }
            }

            dungeon.AddFloor(new DungeonFloor(
                1,
                layer,
                cells,
                new DungeonStair(DungeonStairType.Up, new GridPosition(0, 0)),
                new DungeonStair(DungeonStairType.Down, new GridPosition(79, 19)),
                Array.Empty<DungeonRoom>(),
                new DungeonFloorGenerationSettings(0)));
            return dungeon;
        }

        sealed class ZeroGameRandom : IGameRandom
        {
            public void Initialize(int seed)
            {
            }

            public int Next()
            {
                return 0;
            }

            public int Next(int maxExclusive)
            {
                return 0;
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                return minInclusive;
            }
        }

        static Actor CreateActor(string name, int factionId, LayerPosition position, int hp)
        {
            return CreateActor(name, factionId, position, hp, 0);
        }

        static Actor CreateActor(string name, int factionId, LayerPosition position, int hp, int archetypeId)
        {
            return new Actor(
                Guid.NewGuid(),
                archetypeId,
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

        static Actor CreateDroppingMonster(LayerPosition position, int hp)
        {
            return new Actor(
                Guid.NewGuid(),
                0,
                new ActorStats(5, 5, 5, 5, 5, 5),
                new Inventory(new FixedItemStackLimitResolver()),
                1,
                10,
                hp,
                10,
                0,
                0,
                1,
                position,
                new ActorFaction(2, "Faction 2"),
                new MonsterBehavior(1, new[] { new ActorDropEntry(1001, 1f, 1, 1) }),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }
    }
}
