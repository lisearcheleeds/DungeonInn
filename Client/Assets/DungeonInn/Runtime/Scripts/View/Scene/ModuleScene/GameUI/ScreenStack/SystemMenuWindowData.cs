using System;
using LighthouseExtends.ScreenStack;

namespace DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack
{
    public sealed class SystemMenuWindowData : IScreenStackData
    {
        public SystemMenuWindowData(
            Action save,
            Action load,
            Action title)
        {
            Save = save ?? throw new ArgumentNullException(nameof(save));
            Load = load ?? throw new ArgumentNullException(nameof(load));
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        public Action Save { get; }
        public Action Load { get; }
        public Action Title { get; }
        public bool IsSystem => false;
        public bool IsOverlayOpen => false;
    }
}

