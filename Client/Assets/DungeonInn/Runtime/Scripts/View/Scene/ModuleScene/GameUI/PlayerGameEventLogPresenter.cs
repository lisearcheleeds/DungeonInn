using System;
using R3;
using VContainer;
using DungeonInn.Application.World;
using DungeonInn.View.Scene.MainScene.World;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    public sealed class PlayerGameEventLogPresenter : IDisposable
    {
        readonly PlayerEventLogStore logStore;
        readonly GameUIAddressableViewFactory viewFactory;
        readonly GameUIModuleScene gameUIModuleScene;
        PlayerEventLogView logView;
        DisposableBag bag;

        [Inject]
        public PlayerGameEventLogPresenter(
            PlayerEventLogStore logStore,
            GameUIAddressableViewFactory viewFactory,
            GameUIModuleScene gameUIModuleScene)
        {
            this.logStore = logStore ?? throw new ArgumentNullException(nameof(logStore));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.gameUIModuleScene = gameUIModuleScene ?? throw new ArgumentNullException(nameof(gameUIModuleScene));
        }

        public void Initialize()
        {
            EnsureLogView();

            if (logView != null)
            {
                var recent = logStore.GetRecentEntries(PlayerEventLogView.LineCountPublic);
                logView.SetEntries(ConvertToTextList(recent));
            }

            logStore.OnEntryAdded
                .Subscribe(OnEntryAdded)
                .AddTo(ref bag);
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        static string[] ConvertToTextList(System.Collections.Generic.IReadOnlyList<PlayerEventLogEntry> entries)
        {
            var result = new string[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                result[i] = entries[i].Text;
            }
            return result;
        }

        void OnEntryAdded(PlayerEventLogEntry entry)
        {
            EnsureLogView();
            logView?.AddEntry(entry.Text);
        }

        void EnsureLogView()
        {
            if (logView != null)
            {
                return;
            }

            if (gameUIModuleScene.UICanvas == null)
            {
                return;
            }

            logView = viewFactory.CreatePlayerEventLogView(gameUIModuleScene.UICanvas.transform);
        }
    }
}

