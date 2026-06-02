using LighthouseExtends.ScreenStack;

namespace DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack
{
    public sealed class DungeonInfoWindowData : IScreenStackData
    {
        public DungeonInfoWindowData(DungeonInfoWindowViewData viewData)
        {
            ViewData = viewData;
        }

        public DungeonInfoWindowViewData ViewData { get; }
        public bool IsSystem => false;
        public bool IsOverlayOpen => false;
    }
}

