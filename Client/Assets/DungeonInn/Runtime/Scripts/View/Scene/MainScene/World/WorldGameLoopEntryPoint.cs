using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldGameLoopEntryPoint : MonoBehaviour
    {
        IGameLoopUseCase gameLoopUseCase;
        IGameWorldState gameWorldState;

        bool isExecuting;

        [Inject]
        public void Construct(
            IGameLoopUseCase gameLoopUseCase,
            IGameWorldState gameWorldState)
        {
            this.gameLoopUseCase = gameLoopUseCase ?? throw new ArgumentNullException(nameof(gameLoopUseCase));
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
        }

        void Start()
        {
            Debug.Log("[WorldGameLoop] EntryPoint started.");
        }

        void Update()
        {
            if (isExecuting || gameLoopUseCase == null)
            {
                return;
            }

            TickAsync().Forget();
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
