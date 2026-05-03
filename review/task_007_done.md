# task_007 完了報告

## 対応内容

- `Client/Assets/DungeonInn/Runtime/Scripts/Extensions/ButtonRxExtensions.cs` の `UnityEngine.UI.Button` 参照を削除
- `LHButton` の namespace が `LighthouseExtends.UIComponent.Button` であることを `Client/Library/PackageCache` 内の `LHButton.cs` で確認
- `SubscribeOnClick` の拡張対象を `Button` から `LHButton` に変更

## 確認結果

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0
