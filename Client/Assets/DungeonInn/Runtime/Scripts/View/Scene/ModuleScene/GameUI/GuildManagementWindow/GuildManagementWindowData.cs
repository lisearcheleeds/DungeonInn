using System;
using LighthouseExtends.ScreenStack;

namespace DungeonInn.View.Scene.ModuleScene.GameUI.ScreenStack
{
    public sealed class GuildManagementWindowData : IScreenStackData
    {
        readonly Func<GuildManagementWindowViewData> reload;
        readonly Action<Guid> upgradeFacility;

        public GuildManagementWindowData(
            GuildManagementWindowViewData viewData,
            Func<GuildManagementWindowViewData> reload,
            Action<Guid> upgradeFacility)
        {
            ViewData = viewData ?? throw new ArgumentNullException(nameof(viewData));
            this.reload = reload ?? throw new ArgumentNullException(nameof(reload));
            this.upgradeFacility = upgradeFacility ?? throw new ArgumentNullException(nameof(upgradeFacility));
        }

        public GuildManagementWindowViewData ViewData { get; private set; }
        public bool IsSystem => false;
        public bool IsOverlayOpen => false;

        public void UpgradeFacility(Guid facilityId)
        {
            upgradeFacility(facilityId);
        }

        public void Refresh()
        {
            ViewData = reload();
        }
    }
}

