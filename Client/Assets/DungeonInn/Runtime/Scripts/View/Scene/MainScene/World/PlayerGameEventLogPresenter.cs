using System;
using DungeonInn.Application.World;
using R3;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class PlayerGameEventLogPresenter : IDisposable
    {
        readonly PlayerEventLogStore logStore;
        readonly WorldAddressableViewFactory viewFactory;
        readonly WorldHudCanvasProvider hudCanvasProvider;
        PlayerEventLogView logView;
        DisposableBag bag;

        [Inject]
        public PlayerGameEventLogPresenter(
            PlayerEventLogStore logStore,
            WorldAddressableViewFactory viewFactory,
            WorldHudCanvasProvider hudCanvasProvider)
        {
            this.logStore = logStore ?? throw new ArgumentNullException(nameof(logStore));
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.hudCanvasProvider = hudCanvasProvider ?? throw new ArgumentNullException(nameof(hudCanvasProvider));
        }

        public void Initialize()
        {
            EnsureLogView();
            if (logView == null)
            {
                return;
            }

            var recent = logStore.GetRecentEntries(PlayerEventLogView.LineCountPublic);
            logView.SetEntries(ConvertToTextList(recent));

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

            var prefab = viewFactory.PlayerEventLogViewPrefab;
            if (prefab == null || hudCanvasProvider.HUDCanvas == null)
            {
                return;
            }

            logView = UnityEngine.Object.Instantiate(prefab, hudCanvasProvider.HUDCanvas.transform);
        }
    }
}
