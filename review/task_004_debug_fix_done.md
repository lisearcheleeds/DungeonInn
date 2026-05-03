# task_004_debug fix done

## 修正内容

- `Launcher.TransitionNextScene()` を `sceneManager.TransitionScene(new WorldScene.WorldTransitionData())` に戻し、Lighthouse の遷移パイプラインを通すよう修正。
- `Launcher.LaunchProcess()` に 5 Config Repository の `UniTask.WhenAll` ロードを復元。
- `Launcher` コンストラクタに `IWorldConfigRepository` / `IInnConfigRepository` / `IAdventurerConfigRepository` / `IMonsterConfigRepository` / `IDungeonConfigRepository` の注入を復元。
- `AddressableAssetSettings.asset` の `m_ActivePlayerDataBuilderIndex` を `0` に変更。
- Unity Editor 上の Addressables Play Mode builder も `ActivePlayModeDataBuilderIndex = 0` に設定。
- Config Repository は Addressables を使用したロードに戻し、PlayMode で await が戻らない経路を避けるため `WaitForCompletion()` で取得して `ToData()` をキャッシュする実装に修正。
- Lighthouse の `AsyncOperation` await が MainScene load 完了後に戻らず ModuleScene load に進まないため、PackageCache は触らず `ProductMainSceneManager` / `ProductModuleSceneManager` を追加し、`AsyncOperation` 完了待ちを `ToUniTask()` にした実装を登録。
- `WorldLifetimeScope` で `WorldScene` を `RegisterComponentInHierarchy` し、`WorldScene` の `[Inject]` が実行されるよう修正。
- `ScreenStackBackgroundInputBlocker` を `ScreenStackScene` 配下に生成し、`ScreenStack` の root count が増えないよう修正。

## 確認

- `uloop.cmd compile --project-path Client`
  - `Success: true`
  - `ErrorCount: 0`
  - `WarningCount: 0`

## 残確認

- 最終 PlayMode シーン確認の直前に PowerShell プロセス自体が `-1073741502` で起動失敗する状態になったため、以下は未完了。
  - `uloop get-logs` Error 0 の最終再確認
  - `execute-dynamic-code` の `World[2] ScreenStack[1] HUD[1]` 最終再確認

直前の切り分けでは、Addressables Play Mode builder 設定後に `World[2]` までは確認済み。その後、Lighthouse MainSceneManager の `AsyncOperation` await 停止を修正するため `ProductMainSceneManager` / `ProductModuleSceneManager` を追加し、コンパイル 0 まで確認済み。
