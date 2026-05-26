using System;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class DungeonLayerListItemViewData
    {
        public DungeonLayerListItemViewData(
            string title,
            string status,
            string actorCount,
            DungeonLayerPopupViewData popup)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title is required.", nameof(title));
            }

            Title = title;
            Status = status ?? string.Empty;
            ActorCount = actorCount ?? string.Empty;
            Popup = popup ?? throw new ArgumentNullException(nameof(popup));
        }

        public string Title { get; }
        public string Status { get; }
        public string ActorCount { get; }
        public DungeonLayerPopupViewData Popup { get; }
    }
}
