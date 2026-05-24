# Milestone 8 Roadmap Fix 2

## 追加対応結果 2026-05-24

再レビューで残っていた設計上の指摘に対して、Codex が以下を追加対応した。

- `WorldGameSettingsRepository` から `IWorldMapViewSettingsRepository` 実装を削除し、Application 用設定のみを扱う責務へ戻した。
- `WorldMapViewSettingsRepository` を World MainScene 側に追加し、World 表示設定は `WorldLifetimeScope` が所有する形に分離した。
- `WorldGameLoopEntryPoint` から `IWorldGameSettingsRepository.LoadAsync()` 呼び出しを削除し、Application 設定ロードは `WorldSimulationOrchestrator.InitializeAsync()` 側に閉じた。
- `WorldGameSettingsSO` を `GameSession.Settings` 側の設定アセット型として配置し、GameSession から World View namespace への依存を削除した。
- `EntrySceneTransitionService` / `IEntrySceneTransitionService` を追加し、Launcher と RebootService に重複していた Title への入口遷移処理を共通化した。
- `RebootService` は Reboot cleanup と Launcher 再ロードを担当し、Title への入口遷移は `EntrySceneTransitionService` に委譲する形にした。

確認結果:

- `uloop.cmd compile --project-path Client`: 成功
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 301 passed
- Play 30 秒: `[World] GameWorldState initialized. Facilities=3 DungeonFloors=1 Actors=0` を確認
- Play 確認時の Error ログ: 0 件
## 目的

`git diff HEAD` に含まれる Milestone 8 後続修正について、クラス責務と依存方向を整理する。

主な対象は以下。

- `MainGame` 命名を `GameSession` へ置き換える
- GameSession scope の所有者と破棄責務を明確にする
- Core / Product 層から GameSession / World 具象依存を撤去する
- GameHUD の Presenter ライフサイクル所有を DI scope に寄せる
- GameSession scope / MainScene scope / ModuleScene scope の関係を読みやすくする

## 背景

現状の差分では `MainGameLifetimeScopeController` は `TitlePresenter` から作成され、`ProductSceneManager` や `RebootService` から破棄される構造になっている。
動作上は成立しているが、以下の設計問題が残っている。

1. `ProductSceneManager` が `MainGameLifetimeScopeController` と `DungeonInnMainSceneId.World` を直接知っている
2. `BackScene()` 成功時に無条件で GameSession scope を破棄している
3. `MainGameLifetimeScope` が World namespace の `ActorSelectionService` を session scope に登録している
4. `GameHUDEntryPoint` が scoped Presenter を手動 `Dispose()` しており、DI scope の所有責務と重なっている
5. `MainGame` という命名が「ゲーム本編画面」に見え、実際の責務である「1 回のプレイセッションの寿命」とずれている

## 方針

### 1. `MainGame` を `GameSession` へリネームする

`MainGame` はゲーム本編画面ではなく、New Game / Continue / Load で開始される寿命単位を表すため、`GameSession` に置き換える。

対象:

- `Runtime/Scripts/MainGame/` -> `Runtime/Scripts/GameSession/`
- namespace `DungeonInn.MainGame` -> `DungeonInn.GameSession`
- `MainGameLifetimeScope` -> `GameSessionLifetimeScope`
- `MainGameAssetScopeHolder` -> `GameSessionAssetScopeHolder`
- `MainGameLifetimeScopeController` -> `GameSessionLifecycle`
- `MainGame/Settings` -> `GameSession/Settings`

注意:

- Unity `.meta` は可能な限り GUID を維持する
- Lighthouse generated `.g.cs` は手動編集しない
- リネーム後に `rg -n "MainGame"` で不要な旧名が残っていないことを確認する

### 2. GameSession の所有者を `GameSessionLifecycle` にする

作成のきっかけは `TitlePresenter` でよいが、所有者は `TitlePresenter` ではなく `GameSessionLifecycle` とする。

導入する interface:

```csharp
public interface IGameSessionLifecycle
{
    LifetimeScope ActiveScope { get; }
    void BeginSession();
    void EndSession();
    bool IsSessionScene(MainSceneId sceneId);
}
```

実装方針:

- `GameSessionLifecycle` が `GameSessionLifetimeScope` を作成 / 破棄する
- `GameSessionLifecycle` は `IRebootCleanupTarget` を実装する
- session 作成時に `IRebootCleanupRegistry.Register(this)` する
- session 通常破棄時に `Unregister(this)` する
- `IsSessionScene(MainSceneId)` で session 内 scene を判定する

`TitlePresenter`:

- `BeginSession()` を呼ぶ
- World 遷移に失敗した場合だけ `EndSession()` で rollback する
- 成功後の session 寿命は所有しない

`ProductEntryPoint`:

- `mainSceneManager.SetEnqueueParentLifetimeScope` / `moduleSceneManager.SetEnqueueParentLifetimeScope` は `IGameSessionLifecycle.ActiveScope ?? productLifetimeScope` を見る
- 具象 `GameSessionLifecycle` ではなく `IGameSessionLifecycle` に依存する

### 3. `ProductSceneManager` から GameSession 具象依存を消す

`ProductSceneManager` は Core / Product の scene 遷移ラッパーであり、`GameSessionLifecycle` 具象クラスや `DungeonInnMainSceneId.World` を直接知るべきではない。

変更方針:

- `ProductSceneManager` は `IGameSessionLifecycle` にのみ依存する
- 遷移成功後は `if (!gameSessionLifecycle.IsSessionScene(nextTransitionData.MainSceneId)) gameSessionLifecycle.EndSession();`
- `BackScene()` 成功後は、現在の Lighthouse API から遷移先 MainSceneId を確定できない限り無条件破棄しない
- 例外時は `gameSessionLifecycle.EndSession()` してから `rebootService.Reboot()` する

禁止:

- `ProductSceneManager` が `GameSessionLifecycle` / `GameSessionLifetimeScope` / `DungeonInnMainSceneId.World` に直接依存すること
- `BackScene()` 成功時に無条件で session scope を破棄すること

### 4. `ActorSelectionService` を World namespace から移す

`ActorSelectionService` は World scene object ではなく、GameSession 内で共有される選択状態 service である。

変更方針:

- `View.Scene.MainScene.World.ActorSelectionService` から移動する
- 移動先は `Runtime/Scripts/View/Scene/Bridge/ActorSelectionService.cs`
- `IActorSelectionReader` は `View.Scene.Bridge` に残す
- World input と GameHUD は `IActorSelectionReader` または必要最小限の selection service に依存する

完了条件:

- `GameSessionLifetimeScope` が `View.Scene.MainScene.World` namespace を import しない
- `ActorSelectionService` が World MainScene の具象型に依存しない
- GameHUD が World 具象 `ActorSelectionService` を直接参照しない

### 5. GameHUD Presenter の Dispose 所有を DI scope に寄せる

現在 `GameHUDEntryPoint.Dispose()` が複数 Presenter を手動 dispose しているが、Presenter は VContainer scoped 登録されている。

変更方針:

- `GameHUDEntryPoint.Dispose()` から Presenter の手動 `Dispose()` 呼び出しを削除する
- Presenter / Factory / Pool の破棄は GameHUD LifetimeScope の disposal に任せる
- `GameHUDEntryPoint` は初期化順序と tick 呼び出しだけを担当する

注意:

- `GameHUDEntryPoint` 自身の `IDisposable` が不要になれば外す
- Presenter が `IDisposable` を実装していれば VContainer が scope dispose 時に破棄する
- 二重 dispose を避ける

## 実装順序

1. `MainGame` -> `GameSession` のファイル / namespace / class リネーム
2. `IGameSessionLifecycle` / `GameSessionLifecycle` を導入
3. `TitlePresenter` / `ProductEntryPoint` / `ProductSceneManager` の依存を `IGameSessionLifecycle` に置換
4. `ProductSceneManager.BackScene()` の無条件破棄を削除
5. `ActorSelectionService` を World namespace から移動
6. `GameHUDEntryPoint` の手動 dispose を削除
7. `rg` で旧名・禁止依存を確認
8. uLoop で compile / EditMode / Play 30 秒確認

## 完了条件

- `rg -n "MainGame" Client/Assets/DungeonInn/Runtime/Scripts -g "*.cs"` で不要な旧名が残っていない
- `ProductSceneManager` が GameSession 具象クラスと `DungeonInnMainSceneId.World` を参照していない
- `ProductSceneManager.BackScene()` が無条件で GameSession を破棄していない
- `GameSessionLifetimeScope` が `DungeonInn.View.Scene.MainScene.World` namespace を参照していない
- `GameHUDEntryPoint` が Presenter を手動 dispose していない
- `uloop.cmd compile --project-path Client` が成功している
- `uloop.cmd run-tests --project-path Client --test-mode EditMode` が全て pass している
- Play 30 秒で `[World] GameWorldState initialized` が出力され、Error ログがない

## 対応結果

2026-05-24 に Codex が以下を実施した。

- `MainGame` フォルダ / namespace / class 名を `GameSession` 系へ置換した
- `IGameSessionLifecycle` を追加し、`GameSessionLifecycle` が session scope の作成・破棄・Reboot cleanup 登録を所有する形にした
- `ProductSceneManager` / `ProductEntryPoint` / `TitlePresenter` の依存を `IGameSessionLifecycle` に置き換えた
- `ProductSceneManager.BackScene()` 成功時の無条件 session 破棄を削除した
- `ActorSelectionService` を `View.Scene.Bridge` namespace へ移動した
- `GameSessionLifetimeScope` から World view settings repository の登録を外し、World view settings は `WorldLifetimeScope` 側で登録した
- `WorldSimulationOrchestrator.InitializeAsync()` が Application 側で利用する `IWorldGameSettingsRepository` を自分で load するようにした
- `GameHUDEntryPoint` から Presenter の手動 dispose と `IDisposable` 実装を削除した

確認結果:

- `rg -n "MainGame" Client/Assets/DungeonInn/Runtime/Scripts -g "*.cs"`: 該当なし
- `ProductSceneManager` は `DungeonInnMainSceneId` / `GameSessionLifecycle` / `GameSessionLifetimeScope` を参照していない
- `GameSessionLifetimeScope` は `DungeonInn.View.Scene.MainScene.World` namespace を参照していない
- `GameHUDEntryPoint` は `IDisposable` / `Dispose()` を持っていない
- `uloop.cmd compile --project-path Client`: 成功
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 301 passed
- Play 30 秒: `[World] GameWorldState initialized. Facilities=3 DungeonFloors=1 Actors=0` を確認、Error ログ 0 件
