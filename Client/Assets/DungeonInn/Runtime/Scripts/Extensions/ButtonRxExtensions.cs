using System;
using LighthouseExtends.UIComponent.Button;
using R3;

namespace DungeonInn.Runtime.Scripts.Extensions
{
    public static class ButtonRxExtensions
    {
        static readonly TimeSpan DefaultClickInterval = TimeSpan.FromMilliseconds(300);

        public static IDisposable SubscribeOnClick(this LHButton button, Action onClick)
        {
            return button.OnClickAsObservable()
                .Where(_ => button != null)
                .Where(_ => button.isActiveAndEnabled)
                .Where(_ => button.interactable)
                .ThrottleFirst(DefaultClickInterval)
                .Subscribe(_ => onClick())
                .AddTo(button);
        }
    }
}
