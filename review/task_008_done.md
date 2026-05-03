# task_008 完了報告

## 対応内容

- `ProductLifetimeScope.cs` の登録を `ProductMainSceneManager` / `ProductModuleSceneManager` から Lighthouse 本体の `MainSceneManager` / `ModuleSceneManager` に戻しました。
- `ProductMainSceneManager.cs` と `ProductModuleSceneManager.cs` を削除しました。
- 対応する `.meta` ファイルを削除しました。

## 検証

- `uloop.cmd compile --project-path Client`
  - Success: true
  - ErrorCount: 0
  - WarningCount: 0

## 削除確認

- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductMainSceneManager.cs` は存在しません。
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductModuleSceneManager.cs` は存在しません。
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductMainSceneManager.cs.meta` は存在しません。
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductModuleSceneManager.cs.meta` は存在しません。
