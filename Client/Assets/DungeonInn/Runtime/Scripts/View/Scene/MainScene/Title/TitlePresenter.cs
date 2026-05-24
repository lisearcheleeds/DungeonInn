using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Core;
using DungeonInn.View.Scene.MainScene.World;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.Title
{
    public sealed class TitlePresenter : ITitlePresenter
    {
        readonly IProductSceneManager sceneManager;
        readonly IGameSessionLifecycle gameSessionLifecycle;
        readonly TitleView titleView;

        bool isTransitioning;

        [Inject]
        public TitlePresenter(
            IProductSceneManager sceneManager,
            IGameSessionLifecycle gameSessionLifecycle,
            TitleView titleView)
        {
            this.sceneManager = sceneManager;
            this.gameSessionLifecycle = gameSessionLifecycle;
            this.titleView = titleView;
        }

        void ITitlePresenter.Setup()
        {
            titleView.SetStartGameListener(StartNewGame);
        }

        void ITitlePresenter.OnEnter()
        {
            isTransitioning = false;
            titleView.SetStartGameInteractable(true);
        }

        void StartNewGame()
        {
            if (isTransitioning)
            {
                return;
            }

            isTransitioning = true;
            titleView.SetStartGameInteractable(false);
            UniTask.Void(StartNewGameAsync);
        }

        async UniTaskVoid StartNewGameAsync()
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
                isTransitioning = false;
                titleView.SetStartGameInteractable(true);
                Debug.LogError($"[Title] Failed to start new game.\n{exception}");
            }
        }
    }
}
