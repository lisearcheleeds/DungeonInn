using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.NewGame;
using DungeonInn.Application.SaveLoad;
#if DEBUG
using DungeonInn.Debugging;
#endif
using DungeonInn.GameSession;
using UnityEngine;
using VContainer;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DungeonInn.View.Scene.MainScene.Title
{
    public sealed class TitlePresenter : ITitlePresenter
    {
        readonly GameSessionStartCoordinator gameSessionStartCoordinator;
        readonly GetSaveSlotSummariesUseCase getSaveSlotSummariesUseCase;
        readonly GetLatestSaveSlotUseCase getLatestSaveSlotUseCase;
        readonly TitleView titleView;

        bool isTransitioning;

        [Inject]
        public TitlePresenter(
            GameSessionStartCoordinator gameSessionStartCoordinator,
            GetSaveSlotSummariesUseCase getSaveSlotSummariesUseCase,
            GetLatestSaveSlotUseCase getLatestSaveSlotUseCase,
            TitleView titleView)
        {
            this.gameSessionStartCoordinator = gameSessionStartCoordinator
                ?? throw new ArgumentNullException(nameof(gameSessionStartCoordinator));
            this.getSaveSlotSummariesUseCase = getSaveSlotSummariesUseCase
                ?? throw new ArgumentNullException(nameof(getSaveSlotSummariesUseCase));
            this.getLatestSaveSlotUseCase = getLatestSaveSlotUseCase
                ?? throw new ArgumentNullException(nameof(getLatestSaveSlotUseCase));
            this.titleView = titleView ?? throw new ArgumentNullException(nameof(titleView));
        }

        void ITitlePresenter.Setup()
        {
            titleView.SetMenuListeners(
                ShowNewGameSeedPanel,
                Continue,
                ShowLoadSlots,
                Exit);
            titleView.SetSeedPanelListeners(StartNewGameFromSeed, titleView.HideSeedPanel);
            titleView.SetSlotCancelListener(titleView.HideSlotPanel);
        }

        void ITitlePresenter.OnEnter()
        {
            isTransitioning = false;
            titleView.SetMenuInteractable(true);
            titleView.SetContinueInteractable(getLatestSaveSlotUseCase.TryExecute(out _));
            titleView.HideSeedPanel();
            titleView.HideSlotPanel();
#if DEBUG
            PlayModeAutomation.RegisterTitleActions(ShowNewGameSeedPanel, StartNewGameFromSeed);
#endif
        }

        void ShowNewGameSeedPanel()
        {
            if (isTransitioning)
            {
                return;
            }

            titleView.ShowSeedPanel();
        }

        void StartNewGameFromSeed()
        {
            if (isTransitioning)
            {
                return;
            }

            var seed = NewGameSeedParser.ParseOrDefault(titleView.SeedText);
            StartTransitionAsync(() => gameSessionStartCoordinator.StartNewGameAsync(seed)).Forget();
        }

        void Continue()
        {
            if (isTransitioning)
            {
                return;
            }

            StartTransitionAsync(async () =>
            {
                await gameSessionStartCoordinator.TryContinueAsync();
            }).Forget();
        }

        void ShowLoadSlots()
        {
            if (isTransitioning)
            {
                return;
            }

            titleView.ShowSlotPanel(
                getSaveSlotSummariesUseCase.Execute(),
                null,
                LoadSlot);
        }

        void LoadSlot(int slotId)
        {
            if (isTransitioning)
            {
                return;
            }

            StartTransitionAsync(async () =>
            {
                await gameSessionStartCoordinator.TryLoadGameAsync(slotId);
            }).Forget();
        }

        async UniTask StartTransitionAsync(Func<UniTask> action)
        {
            try
            {
                isTransitioning = true;
                titleView.SetMenuInteractable(false);
                titleView.HideSeedPanel();
                titleView.HideSlotPanel();
                await action();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Title] Failed to transition from title.\n{exception}");
                isTransitioning = false;
                titleView.SetMenuInteractable(true);
                titleView.SetContinueInteractable(getLatestSaveSlotUseCase.TryExecute(out _));
            }
        }

        static void Exit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
