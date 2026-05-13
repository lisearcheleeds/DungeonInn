using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorSpatialIndexServiceTests
    {
        [Test]
        public void SyncActorAndRemoveActorMaintainNearbyCandidates()
        {
            var spatialIndex = new ActorSpatialIndexService();
            var actor = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f));
            var results = new List<Actor>();

            spatialIndex.SyncActor(actor);
            spatialIndex.CollectNearbyActors(actor.Position, 1, results);

            Assert.That(results.Select(x => x.Id), Does.Contain(actor.Id));

            results.Clear();
            var previousPosition = actor.Position;
            actor.MoveTo(new LayerPosition(MapLayerId.DungeonFloor(1), 65f, 5f));
            spatialIndex.SyncActor(actor);

            spatialIndex.CollectNearbyActors(previousPosition, 1, results);
            Assert.That(results.Any(x => x.Id.Equals(actor.Id)), Is.False);

            results.Clear();
            spatialIndex.CollectNearbyActors(actor.Position, 1, results);
            Assert.That(results.Select(x => x.Id), Does.Contain(actor.Id));

            results.Clear();
            spatialIndex.RemoveActor(actor.Id);
            spatialIndex.CollectNearbyActors(actor.Position, 1, results);
            Assert.That(results.Any(x => x.Id.Equals(actor.Id)), Is.False);
        }

        [Test]
        public void GroundActorIsMarkedDirtyAndRemovedFromIndex()
        {
            var spatialIndex = new ActorSpatialIndexService();
            var actor = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f));
            var results = new List<Actor>();
            var dirtyActorIds = new List<Guid>();

            spatialIndex.SyncActor(actor);
            spatialIndex.ConsumeDirtyActorIds(dirtyActorIds);

            actor.MoveTo(new LayerPosition(MapLayerId.Ground, 5f, 5f));
            spatialIndex.SyncActor(actor);
            spatialIndex.CollectNearbyActors(new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f), 1, results);
            spatialIndex.ConsumeDirtyActorIds(dirtyActorIds);

            Assert.That(results.Any(x => x.Id.Equals(actor.Id)), Is.False);
            Assert.That(dirtyActorIds, Does.Contain(actor.Id));
        }

        [Test]
        public void DetectCombatEncounterProcessesDirtyActorsOnly()
        {
            var spatialIndex = new ActorSpatialIndexService();
            var eventBus = new CollectingEventBus();
            var combatService = new ActorCombatService();
            var worldState = new TestWorldState(CreateDungeon());
            var useCase = new DetectCombatEncounterUseCase(
                combatService,
                spatialIndex,
                new CombatEncounterTargetResolver(new StubGameClock(), spatialIndex),
                eventBus);

            var actor = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f));
            var enemy = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 7f, 5f));
            worldState.RegisterActor(actor);
            worldState.RegisterActor(enemy);
            spatialIndex.SyncActor(actor);
            spatialIndex.SyncActor(enemy);

            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(eventBus.GetEvents<CombatEncounterStarted>().Count, Is.EqualTo(2));

            enemy.MoveTo(new LayerPosition(MapLayerId.DungeonFloor(1), 8f, 5f));
            spatialIndex.SyncActor(enemy);
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(eventBus.GetEvents<CombatEncounterStarted>().Count, Is.EqualTo(2));
        }

        [Test]
        public void DetectCombatEncounterClearsAttackersWhenTargetMovesToGround()
        {
            var spatialIndex = new ActorSpatialIndexService();
            var eventBus = new CollectingEventBus();
            var combatService = new ActorCombatService();
            var worldState = new TestWorldState(CreateDungeon());
            var useCase = new DetectCombatEncounterUseCase(
                combatService,
                spatialIndex,
                new CombatEncounterTargetResolver(new StubGameClock(), spatialIndex),
                eventBus);

            var actor = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f));
            var enemy = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 7f, 5f));
            worldState.RegisterActor(actor);
            worldState.RegisterActor(enemy);
            spatialIndex.SyncActor(actor);
            spatialIndex.SyncActor(enemy);
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            enemy.MoveTo(new LayerPosition(MapLayerId.Ground, 5f, 5f));
            spatialIndex.SyncActor(enemy);
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(combatService.HasTarget(actor.Id), Is.False);
            Assert.That(eventBus.GetEvents<CombatEncounterEnded>().Any(x => x.ActorId.Equals(actor.Id)), Is.True);
        }

        [Test]
        public void DetectCombatEncounterReevaluatesAttackersWhenTargetMovesOutOfRange()
        {
            var spatialIndex = new ActorSpatialIndexService();
            var eventBus = new CollectingEventBus();
            var combatService = new ActorCombatService();
            var worldState = new TestWorldState(CreateDungeon());
            var useCase = new DetectCombatEncounterUseCase(
                combatService,
                spatialIndex,
                new CombatEncounterTargetResolver(new StubGameClock(), spatialIndex),
                eventBus);

            var actor = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f));
            var enemy = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 7f, 5f));
            worldState.RegisterActor(actor);
            worldState.RegisterActor(enemy);
            spatialIndex.SyncActor(actor);
            spatialIndex.SyncActor(enemy);
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            enemy.MoveTo(new LayerPosition(MapLayerId.DungeonFloor(1), 50f, 5f));
            spatialIndex.SyncActor(enemy);
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(combatService.HasTarget(actor.Id), Is.False);
            Assert.That(eventBus.GetEvents<CombatEncounterEnded>().Any(x => x.ActorId.Equals(actor.Id)), Is.True);
        }

        [Test]
        public void DetectCombatEncounterInvalidatesLineOfSightCacheWhenTargetMoves()
        {
            var spatialIndex = new ActorSpatialIndexService();
            var eventBus = new CollectingEventBus();
            var combatService = new ActorCombatService();
            var worldState = new TestWorldState(CreateDungeonWithWall(new GridPosition(6, 5)));
            var useCase = new DetectCombatEncounterUseCase(
                combatService,
                spatialIndex,
                new CombatEncounterTargetResolver(new StubGameClock(), spatialIndex),
                eventBus);

            var actor = CreateActor(1, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 5f));
            var enemy = CreateActor(2, new LayerPosition(MapLayerId.DungeonFloor(1), 5f, 7f));
            worldState.RegisterActor(actor);
            worldState.RegisterActor(enemy);
            spatialIndex.SyncActor(actor);
            spatialIndex.SyncActor(enemy);
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            enemy.MoveTo(new LayerPosition(MapLayerId.DungeonFloor(1), 7f, 5f));
            spatialIndex.SyncActor(enemy);
            useCase.ExecuteAsync(worldState).GetAwaiter().GetResult();

            Assert.That(combatService.HasTarget(actor.Id), Is.False);
            Assert.That(eventBus.GetEvents<CombatEncounterEnded>().Any(x => x.ActorId.Equals(actor.Id)), Is.True);
        }

        static Dungeon CreateDungeon()
        {
            var layer = new MapLayer(MapLayerId.DungeonFloor(1), 80, 20, 1f);
            var cells = new DungeonCell[layer.Width * layer.Depth];
            for (var z = 0; z < layer.Depth; z++)
            {
                for (var x = 0; x < layer.Width; x++)
                {
                    cells[z * layer.Width + x] = new DungeonCell(new GridPosition(x, z), DungeonCellType.Room);
                }
            }

            var floor = new DungeonFloor(
                1,
                layer,
                cells,
                new DungeonStair(DungeonStairType.Up, new GridPosition(1, 1)),
                new DungeonStair(DungeonStairType.Down, new GridPosition(18, 18)),
                Array.Empty<DungeonRoom>(),
                new DungeonFloorGenerationSettings(0));
            var dungeon = new Dungeon(1);
            dungeon.AddFloor(floor);
            return dungeon;
        }

        static Dungeon CreateDungeonWithWall(GridPosition wallPosition)
        {
            var dungeon = CreateDungeon();
            var floor = dungeon.GetFloor(1);
            floor.GetCell(wallPosition).ChangeType(DungeonCellType.Wall);
            return dungeon;
        }

        static Actor CreateActor(int factionId, LayerPosition position)
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
                position,
                new ActorFaction(factionId, $"Faction {factionId}"),
                new AdventurerBehavior(0),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        sealed class StubGameClock : IGameClock
        {
            public int TotalScheduleTick => 1;
            public int CurrentScheduleTick => 1;
            public int CurrentDay => 0;
            public int CurrentTickOfDay => 1;
            public float ElapsedRealTimeSeconds => 0f;
            public float ElapsedGameTimeSeconds => 0f;
            public float TimeScale => 1f;
            public bool IsPaused => false;

            public void SetTimeScale(float timeScale)
            {
            }

            public void Pause()
            {
            }

            public void Resume()
            {
            }

            public GameClockAdvanceResult Advance(float unscaledDeltaTimeSeconds)
            {
                throw new NotSupportedException();
            }
        }

        sealed class CollectingEventBus : IGameEventBus
        {
            readonly List<IGameEvent> events = new();
            public void Publish(IGameEvent gameEvent) => events.Add(gameEvent);
            public Observable<T> OnEvent<T>() where T : class, IGameEvent => throw new NotSupportedException();
            public IReadOnlyList<T> GetEvents<T>() where T : class, IGameEvent => events.OfType<T>().ToList();
        }

        sealed class TestWorldState : IGameWorldStateReader
        {
            readonly List<Actor> actors = new();

            public TestWorldState(Dungeon dungeon)
            {
                Dungeon = dungeon;
            }

            public bool IsInitialized => true;
            public AdventurerGuild Guild => null;
            public GroundMap GroundMap => null;
            public Dungeon Dungeon { get; }
            public InnEconomyState InnEconomy { get; } = new();
            public IReadOnlyList<Actor> Actors => actors;
            public IReadOnlyList<ItemInstance> Items => Array.Empty<ItemInstance>();
            public IReadOnlyList<ProjectileInstance> Projectiles => Array.Empty<ProjectileInstance>();
            public IReadOnlyList<AreaEffectInstance> AreaEffects => Array.Empty<AreaEffectInstance>();
            public SpawnScheduleState SpawnSchedule { get; } = new();

            public void RegisterActor(Actor actor)
            {
                actors.Add(actor);
            }

            public Actor FindActor(Guid actorId)
            {
                return actors.FirstOrDefault(actor => actor.Id.Equals(actorId));
            }
        }
    }
}
