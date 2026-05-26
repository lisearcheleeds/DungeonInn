using System;
using LighthouseExtends.ScreenStack;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class MarketWindowData : IScreenStackData
    {
        public MarketWindowData(
            MarketWindowViewData viewData,
            Func<MarketWindowViewData> reload,
            Action<int> fulfillOffer)
        {
            ViewData = viewData;
            Reload = reload ?? throw new ArgumentNullException(nameof(reload));
            FulfillOffer = fulfillOffer ?? throw new ArgumentNullException(nameof(fulfillOffer));
        }

        public MarketWindowViewData ViewData { get; private set; }
        public Func<MarketWindowViewData> Reload { get; }
        public Action<int> FulfillOffer { get; }
        public bool IsSystem => false;
        public bool IsOverlayOpen => false;

        public void Refresh()
        {
            ViewData = Reload();
        }
    }
}
