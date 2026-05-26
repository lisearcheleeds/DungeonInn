using LighthouseExtends.ScreenStack;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class GuildManagementWindowData : IScreenStackData
    {
        public GuildManagementWindowData(GuildManagementWindowViewData viewData)
        {
            ViewData = viewData;
        }

        public GuildManagementWindowViewData ViewData { get; }
        public bool IsSystem => false;
        public bool IsOverlayOpen => false;
    }
}
