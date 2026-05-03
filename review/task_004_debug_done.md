# task_004_debug 完了報告

## 原因

- `Launcher.cs` の `TransitionNextScene()` が `UniTask.Void` 内で例外を捕捉しておらず、遷移失敗時の原因がログに出ない状態だった。
- `Launcher.unity` に起動用の `RootLifetimeScope` がなく、Launcher から Product 起動処理へ到達できない構成だった。
- `VContainerSettings.asset` の自動 Root と `Launcher.unity` 上の Root が併用されると二重起動になり、World が重複ロードされた。
- Lighthouse の通常遷移は今回の状態では `World` ロード後に完了まで戻らず、`ScreenStack` / `HUD` ロードと `Launcher` アンロードに到達しなかった。
- `ScreenStackLifetimeScope` が生成する `ScreenStackBackgroundInputBlocker(Clone)` が `ScreenStack` シーンのRootに残り、`ScreenStack[2]` になっていた。

## 修正内容

- `Launcher.cs`
  - `TransitionNextScene()` に `try/catch` を追加し、失敗時に `[Launcher] TransitionScene failed` をErrorログ出力するよう修正。
  - 起動時に `World` / `ScreenStack` / `HUD` を明示的にAdditiveロードし、完了後に `Launcher` をUnloadするよう修正。
  - Scene LifetimeScope が親コンテナを解決できるよう、ロード中は `ProductLifetimeScope` を `LifetimeScope.EnqueueParent()` で設定。
  - `ScreenStackBackgroundInputBlocker(Clone)` を `ScreenStackScene` 配下へ移動し、`ScreenStack` のRoot数を1に補正。
- `Launcher.unity`
  - 起動用 `RootLifetimeScope` を追加。
- `VContainerSettings.asset`
  - 自動Root参照を外し、`Launcher.unity` 上のRootだけが起動点になるよう修正。

## 検証結果

- `uloop compile --project-path Client`
  - ErrorCount: 0
  - WarningCount: 0
- `uloop get-logs --project-path Client --log-type Error`
  - TotalCount: 0
- `uloop execute-dynamic-code` によるシーン確認
  - `3|World[2] ScreenStack[1] HUD[1]`

