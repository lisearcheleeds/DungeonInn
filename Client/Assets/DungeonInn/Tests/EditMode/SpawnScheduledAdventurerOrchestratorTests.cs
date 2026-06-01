using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Items;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class SpawnScheduledAdventurerOrchestratorTests
    {
        [Test]
        public void SpawnSkipsActiveAdventurerWithSameSpawnMasterId()
        {
            var masterRepository = new HardcodedMasterRepository();
            var settingsRepository = new FixedWorldGameSettingsRepository();
            var eventBus = new CollectingEventBus();
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var profileRegistry = new ActorProfileRegistry();
            var worldState = CreateWorldState(masterRepository, settingsRepository, eventBus, candidateService);
            var blockedSpawnMaster = masterRepository.GetAdventurerSpawnMaster(9);
            var blockedArchetype = masterRepository.GetActorArchetypeMaster(blockedSpawnMaster.ActorArchetypeId);
            var existingActor = CreateAdventurer();
            profileRegistry.Register(
                existingActor.Id,
                blockedSpawnMaster.DisplayName,
                blockedSpawnMaster.ActorArchetypeId,
                blockedArchetype.SpeciesId,
                ActorBehaviorType.Adventurer,
                blockedSpawnMaster.Id);
            worldState.RegisterActor(existingActor);
            worldState.SpawnSchedule.MarkAdventurerSpawned(1);
            worldState.SpawnSchedule.MarkAdventurerSpawned(6);
            worldState.SpawnSchedule.MarkAdventurerSpawned(7);
            var orchestrator = new SpawnScheduledAdventurerOrchestrator(
                new SpawnAdventurerUseCase(
                    new ActorFactory(masterRepository),
                    masterRepository,
                    new CompleteActorSpawnUseCase(profileRegistry, eventBus)),
                masterRepository,
                new FixedGameRandom(0),
                settingsRepository,
                new SpawnTableResolver(masterRepository),
                new StubGameClock(),
                profileRegistry);

            var spawnedActor = orchestrator.ExecuteAsync(worldState, 1000).GetAwaiter().GetResult();

            Assert.That(spawnedActor, Is.Not.Null);
            Assert.That(profileRegistry.TryGetProfile(spawnedActor.Id, out var profile), Is.True);
            Assert.That(profile.AdventurerSpawnMasterId, Is.Not.EqualTo(blockedSpawnMaster.Id));
        }

        static GameWorldState CreateWorldState(
            IMasterRepository masterRepository,
            FixedWorldGameSettingsRepository settingsRepository,
            IGameEventBus eventBus,
            ActorProcessingCandidateService candidateService)
        {
            var worldState = new GameWorldState(
                new ActorSpatialIndexService(settingsRepository),
                new ItemSpatialIndexService(settingsRepository),
                candidateService,
                ActorViewDataStoreTestFactory.Create(),
                settingsRepository);
            var buildingRegistry = new FacilityBuildingRegistry();
            var initialize = new InitializeGameWorldOrchestrator(
                worldState,
                new InitializeWorldMapUseCase(settingsRepository, settingsRepository),
                new InitializeDungeonOrchestrator(new GenerateDungeonFloorUseCase(
                    settingsRepository,
                    masterRepository,
                    new AssignDungeonRoomRolesUseCase(masterRepository))),
                masterRepository,
                eventBus,
                settingsRepository,
                settingsRepository,
                buildingRegistry);
            initialize.ExecuteAsync(new InitializeGameWorldRequest(InitialWorldSettings.CreateDefault().DungeonSeed))
                .GetAwaiter()
                .GetResult();
            return worldState;
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
                10,
                10,
                0,
                0,
                1,
                new LayerPosition(MapLayerId.Ground, 0f, 0f),
                new ActorFaction(1, "Adventurer"),
                new AdventurerBehavior(0, AdventurerLifecycleState.Arrived),
                WeaponTypeCombatMasterCatalog.Get(WeaponType.Fist));
        }

        sealed class FixedGameRandom : IGameRandom
        {
            readonly int value;

            public FixedGameRandom(int value)
            {
                this.value = value;
            }

            public void Initialize(int seed)
            {
            }

            public int Next()
            {
                return value;
            }

            public int Next(int maxExclusive)
            {
                return Math.Min(value, maxExclusive - 1);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                return Math.Clamp(value, minInclusive, maxExclusive - 1);
            }
        }

        sealed class StubGameClock : IGameClock
        {
            public int TotalScheduleTick => 0;
            public int CurrentScheduleTick => 0;
            public int CurrentDay => 0;
            public int CurrentTickOfDay => 0;
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
                return new GameClockAdvanceResult(0, Array.Empty<int>());
            }
        }

        sealed class CollectingEventBus : IGameEventBus
        {
            readonly Subject<IGameEvent> subject = new();

            public void Publish(IGameEvent gameEvent)
            {
                subject.OnNext(gameEvent);
            }

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return subject.Where(gameEvent => gameEvent is T).Select(gameEvent => (T)(object)gameEvent);
            }
        }
    }
}
