using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.SaveLoad;
using DungeonInn.Core;
using DungeonInn.View.Scene.MainScene.World;
using UnityEngine;
using VContainer;

namespace DungeonInn.GameSession
{
    public sealed class GameSessionStartCoordinator
    {
        readonly IProductSceneManager sceneManager;
        readonly IGameSessionLifecycle gameSessionLifecycle;
        readonly IGameSaveRepository gameSaveRepository;
        readonly GameSessionStartRequestStore startRequestStore;

        [Inject]
        public GameSessionStartCoordinator(
            IProductSceneManager sceneManager,
            IGameSessionLifecycle gameSessionLifecycle,
            IGameSaveRepository gameSaveRepository,
            GameSessionStartRequestStore startRequestStore)
        {
            this.sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            this.gameSessionLifecycle = gameSessionLifecycle
                ?? throw new ArgumentNullException(nameof(gameSessionLifecycle));
            this.gameSaveRepository = gameSaveRepository
                ?? throw new ArgumentNullException(nameof(gameSaveRepository));
            this.startRequestStore = startRequestStore
                ?? throw new ArgumentNullException(nameof(startRequestStore));
        }

        public UniTask StartNewGameAsync(int seed)
        {
            startRequestStore.Set(GameSessionStartRequest.NewGame(seed));
            return BeginSessionAndEnterWorldAsync();
        }

        public async UniTask<bool> TryLoadGameAsync(int slotId)
        {
            if (!gameSaveRepository.TryLoad(slotId, out var saveData))
            {
                return false;
            }

            startRequestStore.Set(GameSessionStartRequest.LoadGame(slotId, saveData));
            await BeginSessionAndEnterWorldAsync();
            return true;
        }

        public async UniTask<bool> TryReloadGameAsync(int slotId)
        {
            if (!gameSaveRepository.TryLoad(slotId, out var saveData))
            {
                return false;
            }

            gameSessionLifecycle.EndSession();
            startRequestStore.Set(GameSessionStartRequest.LoadGame(slotId, saveData));
            await BeginSessionAndEnterWorldAsync();
            return true;
        }

        public async UniTask<bool> TryContinueAsync()
        {
            if (!gameSaveRepository.TryGetLatestSlot(out var summary))
            {
                return false;
            }

            return await TryLoadGameAsync(summary.SlotId);
        }

        async UniTask BeginSessionAndEnterWorldAsync()
        {
            try
            {
                gameSessionLifecycle.BeginSession();
                await sceneManager.TransitionScene(new WorldScene.WorldTransitionData());
            }
            catch (OperationCanceledException)
            {
                gameSessionLifecycle.EndSession();
                throw;
            }
            catch (Exception exception)
            {
                gameSessionLifecycle.EndSession();
                Debug.LogError($"[GameSessionStart] Failed to enter World.\n{exception}");
                throw;
            }
        }
    }
}
