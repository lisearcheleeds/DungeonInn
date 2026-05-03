# task_007: ButtonRxExtensions を LHButton 対応に変更

## 問題

`ButtonRxExtensions.cs` が `UnityEngine.UI.Button` を直接使用している（CLAUDE.md 違反）。

```csharp
// NG
using UnityEngine.UI;
public static IDisposable SubscribeOnClick(this Button button, Action onClick)
```

## 調査済み情報

`LHButton` は `UnityEngine.UI.Button` を継承しているため、
`Button` を `LHButton` に変えるだけで動作は変わらない。
`OnClickAsObservable()` / `isActiveAndEnabled` / `interactable` はすべて使用可能。

## 修正対象

```
Client/Assets/DungeonInn/Runtime/Scripts/Extensions/ButtonRxExtensions.cs
```

## 修正内容

```csharp
using R3;
using LighthouseExtends.UIComponent.Button; // LHButton の namespace（プロジェクトを確認して正しいものを使う）

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
```

**注意**: `LHButton` の正確な using namespace はコードを検索して確認すること。
`Library/PackageCache` 内の `LHButton.cs` を読んで確認する。

## 完了条件

- [ ] `uloop.cmd compile --project-path Client` エラーゼロ
- [ ] `review/task_007_done.md` に完了報告
