using System;
using System.Collections.Generic;
using DungeonInn.Application.SaveLoad;
using LighthouseExtends.ScreenStack;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class SaveSlotSelectionWindowData : IScreenStackData
    {
        public SaveSlotSelectionWindowData(
            string title,
            IReadOnlyList<GameSaveSlotSummary> summaries,
            int? activeSlotId,
            bool allowEmpty,
            Action<int> selectSlot)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Summaries = summaries ?? throw new ArgumentNullException(nameof(summaries));
            ActiveSlotId = activeSlotId;
            AllowEmpty = allowEmpty;
            SelectSlot = selectSlot ?? throw new ArgumentNullException(nameof(selectSlot));
        }

        public string Title { get; }
        public IReadOnlyList<GameSaveSlotSummary> Summaries { get; }
        public int? ActiveSlotId { get; }
        public bool AllowEmpty { get; }
        public Action<int> SelectSlot { get; }
        public bool IsSystem => false;
        public bool IsOverlayOpen => false;
    }
}
