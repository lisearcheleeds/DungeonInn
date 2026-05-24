# Milestone 8 after review 2 - Codex

日付: 2026-05-24
レビュー担当: Codex
対象: `git diff HEAD`

## レビュー方法

- `docs/` 配下の全ファイル 80 件を確認した。
- 主対象は現在の実装差分 `git diff HEAD` とした。
- `docs/guidelines/self-review-guidelines.md` に従った。
- 専任レビュー軸として以下を分けて確認した。
  - Coding Rules
  - Lighthouse / DI / LifetimeScope / Addressable
  - Application Boundary / Domain / Implementation Quality
- 最後に統合レビューとして、重複指摘を統合し、現行コードで解消済みの旧指摘は再掲しなかった。

## 総合判定

今回の差分では、前回レビューで残っていた `WorldHudCanvasProvider` / `FindFirstObjectByType` 問題は解消され、HUD の所有も `GameHUD` 側へ移っている。
一方で、`MainGameLifetimeScope`、ゲームセッション寿命、Application から View への依存、HUD 境界契約に完了を止める問題が残っている。

以下の高重大度項目を修正するまで、Milestone 8 を完全完了として扱うことは推奨しない。

---

### 1. MainGameLifetimeScope の配置と namespace が設計ドキュメントと一致していない

重大度: 高

問題:

`docs/roadmap/milestone8-roadmap-fix.md` の FIX-4 では、`MainGameLifetimeScope` は `Core/` 配下へ移動し、namespace を `DungeonInn.Core` に変更することになっている。
しかし実装は `Runtime/Scripts/MainGame` 配下、namespace `DungeonInn.MainGame` のままである。
そのため、ゲームセッションの Composition Root の所有者が設計上の Product/Core 境界と一致しておらず、`ProductEntryPoint` も `DungeonInn.MainGame` に依存している。

原因:

スコープ分離は実装されたが、FIX-4 で定義された責務移動が完了していない。
機能上は動く形になっているが、設計ドキュメント上の所有境界と実装の配置がずれている。

解決案:

`MainGameLifetimeScope`、`MainGameLifetimeScopeController`、`MainGameAssetScopeHolder` を設計通り Core 側へ移す。
もし `DungeonInn.MainGame` を正式なゲームセッション層として残すなら、`docs/roadmap/milestone8-roadmap-fix.md` または設計ドキュメントを更新し、Product/Core がそこへ依存してよい理由と境界を明記する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/MainGame/MainGameLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/MainGame/MainGameLifetimeScopeController.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductEntryPoint.cs`
- `docs/roadmap/milestone8-roadmap-fix.md`

完了条件:

- [ ] `MainGameLifetimeScope.cs` が設計ドキュメント通りの所有 namespace / path にある、またはドキュメント側に新しい所有方針が明記されている
- [ ] `ProductEntryPoint` が未定義のセッションスコープ namespace に依存していない
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 2. MainGameLifetimeScope が World 離脱時に破棄されない

重大度: 高

問題:

`TitlePresenter` は World 遷移前に `MainGameLifetimeScope` を生成し、`ProductEntryPoint` は active scope を MainScene / ModuleScene の親 scope として使っている。
しかし、World から Title へ戻る、別 MainScene へ遷移する、Reboot する、または scope 生成後に遷移失敗する、といった経路で `DisposeGameScope()` を呼ぶ実装が見当たらない。
これは Milestone 8 の完了条件「World から離脱した後、`MainGameLifetimeScope` と scoped disposable が破棄される」に反する。

原因:

`MainGameLifetimeScopeController` が生成と破棄の責務を持っているが、Scene 遷移ライフサイクルに破棄処理が接続されていない。
`Dispose()` は Product singleton 自体の破棄時にしか呼ばれないため、ゲームセッション単位の破棄としては遅すぎる。

解決案:

以下のような明示的なセッション終了経路で `DisposeGameScope()` を呼ぶ。

- 非ゲーム MainScene への遷移完了時
- `PreReboot`
- `CreateGameScope()` 後の遷移失敗時
- 将来の New Game 再開始時

さらに、World 離脱時に active scope が null になり、`IAssetScope` などの scoped disposable が破棄されることを確認するテストを追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/MainGame/MainGameLifetimeScopeController.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/Title/TitlePresenter.cs`
- `docs/roadmap/milestone8-roadmap.md`
- `docs/roadmap/milestone8-roadmap-fix.md`

完了条件:

- [ ] ゲームセッション終了 / Reboot / 遷移失敗の経路で `MainGameLifetimeScopeController.DisposeGameScope()` が呼ばれる
- [ ] World から離脱した後、`ActiveGameScope` が null になる
- [ ] `IAssetScope` を含む MainGame scoped disposable がセッション終了時に破棄される
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 3. GameRandom が Product scope に残り、設定 seed も適用されていない

重大度: 高

問題:

`GameRandom` は `ProductLifetimeScope` に登録されているため、ゲームプレイ用乱数状態がゲームセッションをまたいで残る。
また、旧 `GameRandom(int seed)` constructor は `Initialize(int seed)` に置き換えられたが、`IGameRandom` は `Initialize` を公開しておらず、production 側で `InitialWorldSettings.GameRandomSeed` を適用している箇所も見当たらない。
そのため現在のゲームプレイ乱数はハードコードされた `12345` で開始され、設定 SO の seed と結びついていない。

原因:

Settings Repository 化によって seed を含む設定は `IWorldGameSettingsRepository` へ移ったが、乱数サービスは Product scope に残り、seed 初期化経路が再接続されていない。

解決案:

`GameRandom` の登録を `MainGameLifetimeScope` へ移し、World / session 初期化時に `worldGameSettingsRepository.GetInitialWorldSettings().GameRandomSeed` を適用する。
単に具象 `GameRandom.Initialize()` を外から呼ぶより、セッション初期化契約として seed を渡す形にした方が責務が読みやすい。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GameRandom.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/IGameRandom.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldGameSettings.cs`
- `docs/roadmap/milestone8-roadmap-fix.md`

完了条件:

- [ ] `IGameRandom` / `GameRandom` が Product scope ではなくゲームセッション scope に所属している
- [ ] spawn / drop / gameplay random の利用前に `GameRandomSeed` が適用されている
- [ ] New Game ごとに新しい乱数状態が生成される
- [ ] seed 指定後の deterministic behavior を確認するテストがある

---

### 4. Application 層が IWorldGameSettingsRepository 経由で View 型に依存している

重大度: 高

問題:

`Application.World` にある `IWorldGameSettingsRepository` が `DungeonInn.View.Scene.MainScene.World` を import し、`WorldMapViewSettings` を公開している。
これにより Application 層から View 層への依存が発生し、ゲーム設定と描画設定が同じ Repository 契約に混在している。

原因:

`WorldCameraSettings` と `LayerPositionViewSettings` は View 側 Repository に分離されたが、`WorldMapViewSettings` だけが Application の game settings repository に残っている。

解決案:

`WorldMapViewSettings` を View 側の narrow repository に分離する。
例として `IWorldMapViewSettingsRepository` を View 側に定義し、`WorldGameSettingsRepository` が MainGame / Infrastructure 側で Application 用 interface と View 用 interface の両方を実装する形にする。
Application 側 interface からは View 型を公開しない。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/IWorldGameSettingsRepository.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/MainGame/Settings/WorldGameSettingsRepository.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `rg -n "DungeonInn\\.View|WorldMapViewSettings" Client/Assets/DungeonInn/Runtime/Scripts/Application` で Application から View 設定への依存が出ない
- [ ] `WorldMapViewSettings` は View 側 Repository から解決されている、または本当に Application 所有の設定である理由が明記されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 5. GameHUD がまだ World 固有の具象型・DTO 契約を消費している

重大度: 高

問題:

proxy 分離自体は導入されているが、HUD 向けの screen position 契約がまだ `LayerPosition` を公開している。
さらに `ActorDetailPopupPresenter` は `View.Scene.MainScene.World` の具象 `ActorSelectionService` を直接注入している。
そのため GameHUD が World 固有の具象型と map position 概念へ依存し続けており、M8 fix 文書で定義された proxy/registry と `ActorScenePosition` DTO 方針に到達していない。

原因:

provider proxy は実装されたが、公開契約が HUD 向け DTO / interface に狭められていない。
Actor 選択状態は共有 scope に移ったが、型の配置と依存先が World MainScene 所有の具象型のまま残っている。

解決案:

`ActorScenePosition` のような `LayerPosition` を露出しない bridge DTO を導入する。
Actor 選択については `IActorSelectionReader` / `IActorSelectionController` のような共有 interface を定義し、`MainGameLifetimeScope` に登録する。
World input と GameHUD はどちらもその interface に依存し、World 具象 `ActorSelectionService` へ直接依存しない。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/Bridge/IActorScreenPositionProvider.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ActorDetailPopupPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/WorldActorStatusPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSelectionService.cs`
- `docs/roadmap/milestone8-roadmap-fix.md`

完了条件:

- [ ] `GameHUD` module code が `DungeonInn.Domain.Map.LayerPosition` を import していない
- [ ] `GameHUD` module code が `ActorSelectionService` 具象型を inject していない
- [ ] `IActorScreenPositionProvider` が HUD / scene 向け DTO 契約を使っている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 6. GameHUD のライフサイクルが設計上の VContainer EntryPoint 所有になっていない

重大度: 中

問題:

M8 fix 文書では `GameHUDEntryPoint : IAsyncStartable, ITickable, IDisposable` を定義し、HUD Presenter の Initialize / Update / Dispose を委譲する方針になっている。
しかし実装では `GameHUDDriver` という `MonoBehaviour` を新規 GameObject として生成し、`Start()` / `Update()` / async loop で HUD の初期化と更新を行っている。
これは VContainer の EntryPoint ライフサイクル所有を迂回しており、破棄責務も読み取りにくい。

原因:

ModuleScene の初期化と ticking を VContainer EntryPoint ではなく、生成された scene object に置いたため。

解決案:

`RegisterComponentOnNewGameObject<GameHUDDriver>` をやめ、`RegisterEntryPoint<GameHUDEntryPoint>()` に置き換える。
`LoadAsync`、Presenter 初期化、Tick、Dispose を VContainer の lifecycle interface へ移す。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GameHUDLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GameHUDDriver.cs`
- `docs/roadmap/milestone8-roadmap-fix.md`

完了条件:

- [ ] `GameHUDLifetimeScope` が `GameHUDEntryPoint` を登録している
- [ ] `GameHUDLifetimeScope` が `GameHUDDriver` を新規 GameObject として生成していない
- [ ] HUD Presenter の初期化・更新・破棄が VContainer lifecycle interface に所有されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 7. ActorDetailViewData が再利用 mutable buffer を alias する

重大度: 中

問題:

`GetActorDetailQuery` は `equipmentNameBuffer` と `effectBuffer` を field として再利用し、clear 後に同じ list instance を `ActorDetailViewData` へ渡している。
そのため、呼び出し側が過去の DTO を保持していると、次回 `Query()` 実行後に過去 DTO の list 内容が変化する可能性がある。

原因:

以前の `ToArray()` allocation を除去したが、DTO の契約が snapshot data から borrowed mutable buffer へ変わったことを明示・制御していない。

解決案:

返却 DTO を immutable snapshot にする、装備 3 スロットを固定 field として持たせる、または query API を明示的な `CopyTo` / `Fill` 契約に変更する。
連続して別 Actor を `Query()` したとき、最初の DTO が変化しないことを確認する regression test を追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetActorDetailQuery.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/ActorDetailViewData.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] 最初の `ActorDetailViewData` を保持したまま別 Actor を Query しても、最初の DTO の内容が変化しない
- [ ] aliasing しないことを確認するテストがある
- [ ] 修正によって frame loop の hidden allocation が再導入されていない

---

### 8. InnGuestSummary が placeholder の回復残り時間を production UI data として公開している

重大度: 中

問題:

`GetInnGuestListUseCase` は `RecoveryRemainingSeconds` に常に `0f` を入れており、`InnStatusPanelView` はそれを回復時間として表示している。
placeholder 値が production UI state として表示される状態になっている。

原因:

回復残り時間の計算または契約が定義される前に、DTO field と UI 表示だけが追加されている。

解決案:

実際の recovery state / settings から残り時間を計算する。
まだ意味のある値を出せないなら、`RecoveryRemainingSeconds` と UI 表示を削除し、後続タスクで再追加する。
常に placeholder になる production ViewData / Summary field は残さない。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetInnGuestListUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/InnGuestSummary.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/InnStatusPanelView.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `RecoveryRemainingSeconds` が実状態から計算され、テストで確認されている、または field と表示が削除されている
- [ ] Inn status panel が常に 0 秒の placeholder 回復時間を表示しない
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している

---

## Coding Rules 補足

Coding Rules 専任レビューでは、変更ファイルに以下の低重大度違反候補が見つかった。

- `MainGameLifetimeScope.cs`、`ActorDetailPopup.cs`、`InnStatusPanelView.cs`、`TitleView.cs` などで using 順序が不統一。
- `RecoverAdventurerAtInnUseCase.cs`、`WorldCameraController.cs`、`MapLayerViewRegistry.cs` などに、プロジェクト規約上 `<` 方向へ直すべき `>` 比較が残っている。
- `MainGameLifetimeScope.cs` などに、WHY ではなく WHAT を説明する section comment が残っている。

これらは今回の主要な完了ブロッカーではないが、strict guideline conformance を求めるなら commit 前に修正すること。

## 検証

- `docs/` 配下の全ファイル 80 件を確認した。
- `git diff HEAD --stat`、`git diff HEAD --name-only`、および対象 runtime architecture の差分を確認した。
- `rg` で Coding Rules の scan pattern 相当を実行した。
- guideline ごとの専任レビュー結果を統合し、この文書に反映した。
- production code は編集していない。
- このレビューでは `uloop.cmd compile`、EditMode test、Play mode 確認は実行していない。
