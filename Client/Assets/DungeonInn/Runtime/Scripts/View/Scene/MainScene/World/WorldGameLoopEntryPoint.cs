using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldGameLoopEntryPoint : MonoBehaviour
    {
        IGameLoopUseCase gameLoopUseCase;
        IGameWorldState gameWorldState;
        InitializeGameWorldUseCase initializeGameWorldUseCase;
        SpawnScheduledAdventurerUseCase spawnScheduledAdventurerUseCase;
        SpawnScheduledMonsterUseCase spawnScheduledMonsterUseCase;
        AdvanceActorSimpleLifecycleUseCase advanceActorSimpleLifecycleUseCase;
        DetectCombatEncounterUseCase detectCombatEncounterUseCase;
        WorldActorDebugVisualizer worldActorDebugVisualizer;

        bool isExecuting;
        bool isInitialized;

        [Inject]
        public void Construct(
            IGameLoopUseCase gameLoopUseCase,
            IGameWorldState gameWorldState,
            InitializeGameWorldUseCase initializeGameWorldUseCase,
            SpawnScheduledAdventurerUseCase spawnScheduledAdventurerUseCase,
            SpawnScheduledMonsterUseCase spawnScheduledMonsterUseCase,
            AdvanceActorSimpleLifecycleUseCase advanceActorSimpleLifecycleUseCase,
            DetectCombatEncounterUseCase detectCombatEncounterUseCase,
            WorldActorDebugVisualizer worldActorDebugVisualizer)
        {
            this.gameLoopUseCase = gameLoopUseCase ?? throw new ArgumentNullException(nameof(gameLoopUseCase));
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.initializeGameWorldUseCase = initializeGameWorldUseCase ?? throw new ArgumentNullException(nameof(initializeGameWorldUseCase));
            this.spawnScheduledAdventurerUseCase = spawnScheduledAdventurerUseCase ?? throw new ArgumentNullException(nameof(spawnScheduledAdventurerUseCase));
            this.spawnScheduledMonsterUseCase = spawnScheduledMonsterUseCase ?? throw new ArgumentNullException(nameof(spawnScheduledMonsterUseCase));
            this.advanceActorSimpleLifecycleUseCase = advanceActorSimpleLifecycleUseCase ?? throw new ArgumentNullException(nameof(advanceActorSimpleLifecycleUseCase));
            this.detectCombatEncounterUseCase = detectCombatEncounterUseCase ?? throw new ArgumentNullException(nameof(detectCombatEncounterUseCase));
            this.worldActorDebugVisualizer = worldActorDebugVisualizer ?? throw new ArgumentNullException(nameof(worldActorDebugVisualizer));
        }

        void Start()
        {
            Debug.Log("[WorldGameLoop] EntryPoint started.");
            InitializeAsync().Forget();
        }

        void Update()
        {
            if (!isInitialized || gameLoopUseCase == null)
            {
                return;
            }

            worldActorDebugVisualizer.UpdateVisuals();

            if (isExecuting)
            {
                return;
            }

            TickAsync().Forget();
        }

        async UniTask InitializeAsync()
        {
            await initializeGameWorldUseCase.ExecuteAsync(
                new InitializeGameWorldRequest(
                    GameConstants.InitialDungeonSeed,
                    Array.Empty<DungeonDepthBandConfig>()));
            isInitialized = true;
            Debug.Log(
                $"[World] GameWorldState initialized. " +
                $"Facilities={gameWorldState.Guild.Facilities.Count} " +
                $"DungeonFloors={gameWorldState.Dungeon.Floors.Count} " +
                $"Actors={gameWorldState.Actors.Count}");
        }

        async UniTask TickAsync()
        {
            isExecuting = true;
            try
            {
                var result = await gameLoopUseCase.ExecuteAsync(new GameLoopTickRequest(Time.unscaledDeltaTime));
                if (result.AdvancedScheduleTicks <= 0)
                {
                    return;
                }

                var spawnedAdventurer = await spawnScheduledAdventurerUseCase.ExecuteAsync(gameWorldState, result.CurrentScheduleTick);
                if (spawnedAdventurer != null)
                {
                    Debug.Log($"[Spawn] Adventurer {spawnedAdventurer.Name} spawned");
                }

                var spawnedMonster = await spawnScheduledMonsterUseCase.ExecuteAsync(gameWorldState, result.CurrentScheduleTick);
                if (spawnedMonster != null)
                {
                    Debug.Log($"[Spawn] Monster {spawnedMonster.Name} spawned at Floor 1");
                }

                var deltaGameSeconds = result.AdvancedScheduleTicks * result.TimeScale;
                await advanceActorSimpleLifecycleUseCase.ExecuteAsync(gameWorldState, deltaGameSeconds);
                await detectCombatEncounterUseCase.ExecuteAsync(gameWorldState);

                LogActorSummaries(result);
            }
            finally
            {
                isExecuting = false;
            }
        }

        void LogActorSummaries(GameLoopTickResult result)
        {
            var firstScheduleTick = result.CurrentScheduleTick - result.AdvancedScheduleTicks + 1;
            for (var i = 0; i < result.AdvancedScheduleTicks; i++)
            {
                LogActorSummary(result, firstScheduleTick + i);
            }
        }

        void LogActorSummary(GameLoopTickResult result, int scheduleTick)
        {
            var actors = gameWorldState.Actors;
            var adventurerCount = actors.Count(x => x.Behavior is AdventurerBehavior);
            var monsterCount = actors.Count(x => x.Behavior is MonsterBehavior);
            var petCount = actors.Count(x => x.Behavior is PetBehavior);
            var guildStaffCount = actors.Count(x => x.Behavior is GuildStaffBehavior);
            var knownCount = adventurerCount + monsterCount + petCount + guildStaffCount;
            var otherCount = actors.Count - knownCount;

            Debug.Log(
                $"[WorldGameLoop] ScheduleTick={scheduleTick} Day={result.CurrentDay} " +
                $"GameTime={result.ElapsedGameTimeSeconds:0.00}s " +
                $"Scale={result.TimeScale:0.##} Actors={actors.Count} " +
                $"Adventurers={adventurerCount} Monsters={monsterCount} Pets={petCount} " +
                $"GuildStaff={guildStaffCount} Others={otherCount}");
        }
    }
}
