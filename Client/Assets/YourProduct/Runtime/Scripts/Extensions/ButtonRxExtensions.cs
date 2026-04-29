using System;
using R3;
using UnityEngine.UI;

namespace YourProduct.Extensions
{
    public static class ButtonRxExtensions
    {
        static readonly TimeSpan DefaultClickInterval = TimeSpan.FromMilliseconds(300);

        public static IDisposable SubscribeOnClick(this Button button, Action onClick)
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