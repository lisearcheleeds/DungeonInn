# Milestone 8 統合レビュー

**日付**: 2026-05-24
**レビュー担当**: Claude Code（統合）
**統合元**:
- `docs/self-review/milestone8-after-review-2-codex.md`（Codex レビュー）
- `docs/self-review/milestone8-completion-review-2-claude.md`（Claude レビュー）

---

## 現行コード確認時の注意

このレビュー本文と末尾の「Milestone 9 キャリーオーバー」は、2026-05-24 時点の後続対応前の指摘一覧である。
その後、`docs/roadmap/milestone8-roadmap-fix-2.md` の追加対応で多くが解消済みになっている。

Milestone 9 着手前にこのファイルを読む場合は、本文の未対応表だけで判断せず、必ず以下を確認すること。

- `docs/roadmap/milestone8-roadmap-fix-2.md`
- このファイル末尾の「2026-05-25 現行実装確認ログ」
- 現行コード検索結果

特に `MainGameLifetimeScope` / `DungeonInn.MainGame` 系の指摘は、現行コードでは `GameSessionLifetimeScope` / `GameSessionLifecycle` / `DungeonInn.GameSession` へ置き換え済みである。

---

## 統合作業の記録

### 重複指摘の統合

| 元指摘 | 統合先 |
|---|---|
| Codex #4「Application→View 依存 (IWorldGameSettingsRepository)」 | Claude L-1 と統合 → **T-4** |
| Codex #8「RecoveryRemainingSeconds placeholder」（重大度: 中） | Claude AB-3（重大度: 低）と統合 → **T-9**（Codex の中を採用） |

### guideline 間の評価差調整

- **T-9 重大度**: Codex は「中」（production UI data として placeholder を出力することの問題を重視）、Claude は「低」（機能ギャップとして軽く評価）。「常に 0f の値を production UI として表示する」は実装品質の観点からも問題であり、Codex の「中」を採用する。
- **CR-1 suffix 規約**: Claude 単独指摘。`InnEconomyStatus` という既存型が同じく `Summary/Status` suffix を使っているため、リネームより `coding-rules.md` へのルール追記を推奨する。

---

## 総合判定: **完了不可（高重大度 5 件、中重大度 4 件が未対処）**

以下 T-1〜T-5（高）のうち、T-2〜T-5 は Milestone 9 着手前に修正必須。
T-1 は設計ドキュメントとの整合にユーザー判断が必要。

---

## 指摘一覧

### T-1 — MainGameLifetimeScope の配置と namespace が設計ドキュメントと不一致

重大度: 高（ユーザー判断待ち）

問題:

`docs/roadmap/milestone8-roadmap-fix.md` の FIX-4 では `MainGameLifetimeScope` を `Core/` 配下へ移動し namespace を `DungeonInn.Core` とする方針になっている。実装は `Runtime/Scripts/MainGame` 配下、namespace `DungeonInn.MainGame` のままであり、設計上の Product/Core 境界と一致していない。`ProductEntryPoint` も `DungeonInn.MainGame` に依存している。

原因:

スコープ分離は実装されたが FIX-4 で定義された責務移動（配置変更）が完了していない。

解決案:

- 案A（設計通り）: `MainGameLifetimeScope`・`MainGameLifetimeScopeController`・`MainGameAssetScopeHolder` を `Core/` へ移し namespace を `DungeonInn.Core` に変更する
- 案B（設計更新）: `DungeonInn.MainGame` を正式なゲームセッション層として残すと決め、`docs/roadmap/milestone8-roadmap-fix.md` を更新して Product/Core がそこへ依存してよい理由と境界を明記する

どちらを選ぶかはユーザー判断とする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/MainGame/MainGameLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/MainGame/MainGameLifetimeScopeController.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductEntryPoint.cs`
- `docs/roadmap/milestone8-roadmap-fix.md`

完了条件:

- [ ] `MainGameLifetimeScope.cs` が設計ドキュメント通りの namespace / path にある、またはドキュメント側に新しい所有方針が明記されている
- [ ] `ProductEntryPoint` が不明確な namespace に依存していない
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### T-2 — MainGameLifetimeScope が World 離脱時に破棄されない

重大度: 高

問題:

`TitlePresenter` は `CreateGameScope()` を呼んでゲームセッションスコープを生成するが、World → Title 戻り・別 MainScene への遷移・Reboot・遷移失敗の経路で `DisposeGameScope()` を呼ぶ実装が存在しない。`Dispose()` は Product singleton 破棄時のみ呼ばれるため、セッション単位の破棄としては遅すぎる。これは M8 完了条件「World 離脱後に `MainGameLifetimeScope` と scoped disposable が破棄される」に反する。

原因:

`MainGameLifetimeScopeController` が生成と破棄の両責務を持つが、Scene 遷移ライフサイクルに破棄処理が接続されていない。

解決案:

以下の経路で `DisposeGameScope()` を明示的に呼ぶ。
- 非ゲーム MainScene への遷移完了時
- `PreReboot`
- `CreateGameScope()` 後の遷移失敗キャッチブロック
- 将来の New Game 再開始時

また、World 離脱後に `ActiveGameScope` が null になり `IAssetScope` 等の scoped disposable が破棄されることを確認するテストを追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/MainGame/MainGameLifetimeScopeController.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/Title/TitlePresenter.cs`
- `docs/roadmap/milestone8-roadmap.md`
- `docs/roadmap/milestone8-roadmap-fix.md`

完了条件:

- [ ] ゲームセッション終了 / Reboot / 遷移失敗の経路で `DisposeGameScope()` が呼ばれる
- [ ] World から離脱した後 `ActiveGameScope` が null になる
- [ ] `IAssetScope` を含む MainGame scoped disposable がセッション終了時に破棄される
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### T-3 — GameRandom が Product scope に残り、設定 seed が適用されていない

重大度: 高

問題:

`GameRandom` は `ProductLifetimeScope` に登録されているため、乱数状態がゲームセッションをまたいで残る。`IGameRandom` は `Initialize` を公開しておらず、`InitialWorldSettings.GameRandomSeed` を適用している箇所も見当たらない。現在のゲームプレイ乱数はハードコードされた `12345` で開始され、設定 SO の seed と結びついていない。

原因:

Settings Repository 化で seed を含む設定は `IWorldGameSettingsRepository` へ移ったが、乱数サービスは Product scope に残ったままで、seed 初期化経路が再接続されていない。

解決案:

`GameRandom` の登録を `MainGameLifetimeScope` へ移し、World / session 初期化時に `worldGameSettingsRepository.GetInitialWorldSettings().GameRandomSeed` を適用する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GameRandom.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/IGameRandom.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldGameSettings.cs`
- `docs/roadmap/milestone8-roadmap-fix.md`

完了条件:

- [ ] `IGameRandom` / `GameRandom` が Product scope ではなくゲームセッション scope に所属している
- [ ] spawn / gameplay random 利用前に `GameRandomSeed` が適用されている
- [ ] New Game ごとに新しい乱数状態が生成される
- [ ] seed 指定後の deterministic behavior を確認するテストがある

---

### T-4 — `IWorldGameSettingsRepository` が View 層型 `WorldMapViewSettings` を返す

重大度: 高

問題:

`IWorldGameSettingsRepository`（`DungeonInn.Application.World`）が `using DungeonInn.View.Scene.MainScene.World` をインポートし、`GetWorldMapViewSettings()` で `WorldMapViewSettings`（View 層型）を返している。Application 層インターフェースが View 層の型に依存しており、Clean Architecture の依存方向規則（View → Application → Domain）に違反している。

`WorldMapViewSettings` の内容はすべて View 描画専用パラメータ（`ChunkTileSize`・`ChunkBuildsPerFrame`・`TileHeightMeters`）であり、Application 層ビジネスロジックでは使用されない。

原因:

Config Repository（P6）実装時に、WorldGameSettings SO から読み込む全設定を 1 つのリポジトリインターフェースにまとめた際に View 専用の `WorldMapViewSettings` も含めた。`WorldCameraSettings` / `LayerPositionViewSettings` は View 側 Repository に分離されたが `WorldMapViewSettings` だけが残っている。

解決案（推奨）:

インターフェースを分割する。
- `IWorldGameSettingsRepository`（Application 層）から `GetWorldMapViewSettings()` を削除
- `IWorldMapViewSettingsRepository`（View 層）を新設し `GetWorldMapViewSettings()` を置く
- `WorldGameSettingsRepository` は両インターフェースを実装し `MainGameLifetimeScope` / View 側スコープから解決する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/IWorldGameSettingsRepository.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapViewSettings.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/MainGame/Settings/WorldGameSettingsRepository.cs`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `IWorldGameSettingsRepository` に `using DungeonInn.View.*` が存在しない
- [ ] Application 層コードに `WorldMapViewSettings` への参照が残存しない（`rg "WorldMapViewSettings" Client/Assets/DungeonInn/Runtime/Scripts/Application` で 0 件）
- [ ] View 層から `WorldMapViewSettings` を取得するための別 IF が存在する
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### T-5 — GameHUD が World 固有の具象型・DTO 契約を直接消費している

重大度: 高

問題:

Bridge Proxy 分離は導入されているが、HUD 向けの screen position 契約が `LayerPosition`（Domain Map 型）を露出している。さらに `ActorDetailPopupPresenter` が `View.Scene.MainScene.World` 所有の具象 `ActorSelectionService` を直接注入している。GameHUD が World 固有の具象型と map position 概念へ依存し続けており、M8 fix 文書で定義された proxy/registry と `ActorScenePosition` DTO 方針に到達していない。

原因:

Provider Proxy は実装されたが、公開契約が HUD 向け DTO / interface に狭められていない。Actor 選択状態は共有スコープに移ったが、型の配置と依存先が World MainScene 所有の具象型のまま残っている。

解決案:

`LayerPosition` を露出しない bridge DTO（`ActorScenePosition` など）を導入する。Actor 選択については `IActorSelectionReader` / `IActorSelectionController` のような共有 interface を `MainGameLifetimeScope` に定義・登録し、World Input と GameHUD のどちらもその interface に依存して World 具象 `ActorSelectionService` へは直接依存しない。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/Bridge/IActorScreenPositionProvider.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ActorDetailPopupPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/WorldActorStatusPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSelectionService.cs`
- `docs/roadmap/milestone8-roadmap-fix.md`

完了条件:

- [ ] `GameHUD` 配下のコードに `DungeonInn.Domain.Map.LayerPosition` の import が存在しない
- [ ] `GameHUD` 配下のコードが `ActorSelectionService` 具象型を inject していない
- [ ] `IActorScreenPositionProvider` が HUD 向け DTO 契約（`LayerPosition` を露出しない）を使っている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### T-6 — `GetAwaiter().GetResult()` による UniTask 同期ブロッキング

重大度: 中

問題:

`WorldHudPresenter` と `InnStatusPanelPresenter` が UniTask を `.GetAwaiter().GetResult()` で同期解決している。

該当箇所:
- `WorldHudPresenter.UpdateHud()`: `getGameTimeStateUseCase.ExecuteAsync().GetAwaiter().GetResult()`、`getInnEconomyStatusUseCase.ExecuteAsync().GetAwaiter().GetResult()`
- `WorldHudPresenter.OnPauseClicked()` / `OnSpeedNormalClicked()` / `OnSpeedFastClicked()`: コマンド系 UseCase を同期ブロッキング
- `InnStatusPanelPresenter.UpdatePanelCore()`: `getInnEconomyStatusUseCase.ExecuteAsync().GetAwaiter().GetResult()`

クエリ系 UseCase（`GetGameTimeStateUseCase` など）の内部実装がインメモリ同期読み取りであれば、UniTask 戻り値シグネチャは不必要なオーバーヘッドを生んでいる。

原因:

View Presenter から async コンテキストなしで呼ぶ必要があるため `GetAwaiter().GetResult()` になっている。クエリ系 UseCase が `UniTask<T>` シグネチャを持ちながら内部は同期処理である。

解決案:

クエリ系 UseCase がインメモリ同期読み取りであれば `T Execute()` シグネチャに変更する。コマンド系 UseCase は UI コールバックから `UniTask.Void(async () => {...})` でラップするか同期シグネチャに変更する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/WorldHudPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/InnStatusPanelPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GetGameTimeStateUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/GetInnEconomyStatusUseCase.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `WorldHudPresenter.cs` に `GetAwaiter().GetResult()` が存在しない
- [ ] `InnStatusPanelPresenter.cs` に `GetAwaiter().GetResult()` が存在しない
- [ ] クエリ系 UseCase が同期シグネチャになっているか、async コンテキストで適切に await されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### T-7 — GameHUDDriver が VContainer EntryPoint ではなく MonoBehaviour で実装されている

重大度: 中

問題:

M8 fix 文書では `GameHUDEntryPoint : IAsyncStartable, ITickable, IDisposable` を定義し、HUD Presenter の Initialize / Update / Dispose を委譲する方針になっている。実装では `GameHUDDriver`（MonoBehaviour）を `RegisterComponentOnNewGameObject` で生成し、`Start()` / `Update()` / async loop で HUD 初期化・更新を行っている。VContainer の EntryPoint ライフサイクル所有を迂回しており、破棄責務も読み取りにくい。

原因:

ModuleScene の初期化と Tick を VContainer EntryPoint ではなく生成された scene object に置いた。

解決案:

`RegisterComponentOnNewGameObject<GameHUDDriver>` を `RegisterEntryPoint<GameHUDEntryPoint>()` に置き換え、`LoadAsync`・Presenter 初期化・Tick・Dispose を VContainer lifecycle interface（`IAsyncStartable`・`ITickable`・`IDisposable`）へ移す。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GameHUDLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GameHUDDriver.cs`
- `docs/roadmap/milestone8-roadmap-fix.md`
- `docs/guidelines/lighthouse-patterns.md`

完了条件:

- [ ] `GameHUDLifetimeScope` が `RegisterEntryPoint<GameHUDEntryPoint>()` を登録している
- [ ] `GameHUDLifetimeScope` が `RegisterComponentOnNewGameObject<GameHUDDriver>` を登録していない
- [ ] HUD Presenter の初期化・更新・破棄が VContainer lifecycle interface に所有されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### T-8 — `ActorDetailViewData` が再利用 mutable buffer を alias する

重大度: 中

問題:

`GetActorDetailQuery` は `equipmentNameBuffer` と `effectBuffer` を field として再利用し、clear 後に同じ list instance を `ActorDetailViewData` へ渡している。呼び出し側が過去の DTO インスタンスを保持していると、次回 `Query()` 実行後に過去 DTO の list 内容が変化する可能性がある（暗黙の aliasing）。

原因:

以前の `ToArray()` allocation 除去の際に、DTO の契約が snapshot data から borrowed mutable buffer へ変わったことが明示・制御されていない。

解決案:

返却 DTO を immutable snapshot にする（装備 3 スロットを固定 field として持たせるなど）、または query API を明示的な `CopyTo` / `Fill` 契約に変更する。連続して別 Actor を `Query()` したとき最初の DTO が変化しないことを確認する regression test を追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetActorDetailQuery.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/ActorDetailViewData.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] 最初の `ActorDetailViewData` を保持したまま別 Actor を Query しても最初の DTO の内容が変化しない
- [ ] aliasing しないことを確認する EditMode テストがある
- [ ] 修正によって frame loop の hidden allocation が再導入されていない

---

### T-9 — `InnGuestSummary.RecoveryRemainingSeconds` が常に `0f`（placeholder を production UI として公開）

重大度: 中

問題:

`GetInnGuestListUseCase.Execute()` で `RecoveryRemainingSeconds` に `0f` がハードコードされており、`InnStatusPanelView` がその placeholder 値を回復残時間として表示している。placeholder 値が production UI state として出力されている状態。

原因:

回復残時間の計算または契約が定義される前に、DTO field と UI 表示だけが追加されている。

解決案:

`AdventurerRecoveryStateService` 等から実際の残回復秒数を計算して設定する。まだ意味のある値を出せないなら、`RecoveryRemainingSeconds` field と UI 表示を削除し、後続タスクで再追加する（常に placeholder になる production ViewData field は残さない）。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetInnGuestListUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/InnGuestSummary.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/InnStatusPanelView.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `RecoveryRemainingSeconds` が実状態から計算されテストで確認されている、または field と表示が削除されている
- [ ] Inn status panel が常に 0 秒の placeholder 回復時間を表示しない
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している

---

### T-10 — Coding Rules: `>` 比較演算子の残存

重大度: 低

問題:

Codex スキャンで `RecoverAdventurerAtInnUseCase.cs`・`WorldCameraController.cs`・`MapLayerViewRegistry.cs` 等に `>` 比較演算子が残存している。`coding-rules.md` 11-1「比較演算子は `<` のみ使用する」に違反。

原因:

今回差分の既存ファイル修正時に比較方向の統一が徹底されなかった。

解決案:

該当ファイルの `a > b` を `b < a` に書き直す。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/RecoverAdventurerAtInnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldCameraController.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapLayerViewRegistry.cs`（要確認）
- `docs/guidelines/coding-rules.md`

完了条件:

- [ ] `rg "> [0-9a-zA-Z]" -g "*.cs" Client/Assets/DungeonInn/Runtime/Scripts` で比較目的の `>` が出ない
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### T-11 — `InnGuestSummary` の suffix 規約と Application 層 DTO ルール明確化

重大度: 低

問題:

`coding-rules.md` は View 向け表示 struct のsuffix として `ViewData` を定めているが、`InnGuestSummary` は Application 層に置かれた View 向け DTO で suffix が `Summary`。一方 `InnEconomyStatus` という既存型も同様に Application 層で `Status` suffix を使っており、完全一致がない。リネームよりルール明確化が先決。

原因:

`coding-rules.md` が「View 向け表示 struct は `ViewData` を使う」とだけ定めており、Application 層の業務結果 DTO（`Summary` / `Status`）との使い分けが明文化されていない。新規型追加時に命名基準が曖昧なため、型ごとに判断がばらつく。

解決案:

`coding-rules.md` に「Application 層の業務結果 DTO は `Summary` / `Status` を使い、View 層専用の表示 DTO は `ViewData` を使う」旨を追記してルールを明確化する。`InnGuestSummary` のリネームは不要。

根拠となるファイルリスト:

- `docs/guidelines/coding-rules.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/InnGuestSummary.cs`

完了条件:

- [ ] `coding-rules.md` に Application 層 DTO と View 層 DTO の suffix 使い分けが明記されている

---

### T-12 — `GetInnGuestListUseCase.Execute()` の毎呼び出しリスト生成

重大度: 低

問題:

`Execute()` が毎回 `new List<InnGuestSummary>()` を生成してアクター全件走査をする。`InnStatusPanelPresenter` から 0.5 秒ごとに呼ばれる。同プロジェクトの `CopySelectionCandidatesTo(List<T>)` パターンとの不統一。

原因:

`IReadOnlyList<T>` を返す UseCase の自然な実装として新規リストを生成した。`CopyTo` パターンを採用した `IActorSelectionCandidateProvider` との設計方針の統一が行われていない。

解決案（0.5 秒間隔なので緊急ではない）:

`Execute(List<InnGuestSummary> buffer)` パターンに変更し呼び出し元に再利用バッファを持たせる。または現状維持として別タスク化する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetInnGuestListUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/InnStatusPanelPresenter.cs`

完了条件:

- [ ] `InnStatusPanelPresenter` に `List<InnGuestSummary>` 再利用バッファがあり `Execute()` がバッファ受け取りシグネチャになっている、または別タスク化されて追跡されている

---

### T-13 — `MinimapPresenter.DrawActorDot()` の `SetPixel()` 個別呼び出し

重大度: 低

問題:

`DrawActorDot()` がネストループで `mapTexture.SetPixel()` を 1 アクターあたり最大 9 回呼ぶ。ベースレイヤーリセットには `SetPixels()` を使っているが、アクタードット描画だけ個別呼び出しになっており、将来アクター数が増えた際のスケーラビリティが低い。

原因:

小さなドット描画の実装として `SetPixel()` が直感的で手軽なため選択された。`SetPixels()` でまとめる実装より記述量が少なく、現行アクター数では性能差が出ていないため気づきにくい。

解決案:

ベースピクセルのワーキングコピー（`Color[]`）へ配列操作でドットを書き込み、最後に `SetPixels(workBuffer)` を 1 回だけ呼ぶ。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/MinimapPresenter.cs`

完了条件:

- [ ] `DrawActorDot()` 内の `mapTexture.SetPixel()` 呼び出しが存在しない
- [ ] ドット描画が `Color[]` バッファへの配列書き込み + `SetPixels()` 1 回パターンになっている

---

### T-14 — イベントアラートにリフレクション使用

重大度: 低

問題:

`WorldHudPresenter.OnGameEvent()` が `hudView.ShowAlert(gameEvent.GetType().Name)` でアラートテキストを設定している。C# 型名（例: `AdventurerArrivedAtInnEvent`）がユーザー向けアラートとして表示される。非ローカライズ・非人間可読のプレースホルダー実装。

原因:

アラート表示テキストの実装が後回しとなり、型名によるデバッグ表示が暫定として残っている。本来は View 側でイベント型を人間可読テキストに変換する責務が必要だが、その設計が未着手。

解決案:

`IGameEvent`（Application 層）に表示文言プロパティを追加することは `application-boundary-guidelines.md` の「イベント DTO に View 用文字列・表示用値を含めない」方針に反するため禁止。

代わりに View 側または HUD 専用の Formatter / Mapper を置く。
- `GameHUD` 内に `GameEventAlertFormatter` クラスを作成し、`IGameEvent` の型ごとに人間可読テキストを返す switch 式で変換する
- または `IGameEvent` 実装ごとに `IGameEventAlertProvider` のような View 専用 narrow interface を View 層で定義し、各イベント型が実装する

`IGameEvent` 本体は変更しない。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/WorldHudPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/IGameEvent.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `WorldHudPresenter` に `GetType().Name` を表示テキストとして使う呼び出しが存在しない
- [ ] `IGameEvent` に View 用文字列プロパティが追加されていない
- [ ] アラートテキストの変換が View 層（`GameHUD` 内 Formatter / Mapper）で行われている
- [ ] アラートテキストが型名ではなく人間可読なテキストを返している

---

## 優先度付き全指摘一覧

| ID | 内容 | 重大度 | 修正優先度 | ソース |
|---|---|---|---|---|
| T-1 | MainGameLifetimeScope 配置/namespace 不一致 | 高 | **ユーザー判断必須** | Codex |
| T-2 | MainGameLifetimeScope 破棄経路なし | 高 | **M9 前必須** | Codex |
| T-3 | GameRandom scope/seed 未接続 | 高 | **M9 前必須** | Codex |
| T-4 | IWorldGameSettingsRepository → View 型依存 | 高 | **M9 前必須** | Codex + Claude |
| T-5 | GameHUD が World 具象型を直接消費 | 高 | **M9 前必須** | Codex |
| T-6 | GetAwaiter().GetResult() 同期ブロッキング | 中 | M9 前推奨 | Claude |
| T-7 | GameHUDDriver → EntryPoint 未対応 | 中 | M9 前推奨 | Codex |
| T-8 | ActorDetailViewData mutable buffer alias | 中 | M9 前推奨 | Codex |
| T-9 | RecoveryRemainingSeconds 常時 0f | 中 | 別タスク化して追跡 | Codex + Claude |
| T-10 | `>` 比較演算子残存 | 低 | commit 前に修正 | Codex |
| T-11 | InnGuestSummary suffix / coding-rules 明確化 | 低 | M9 中に対応 | Claude |
| T-12 | GetInnGuestListUseCase リスト毎回生成 | 低 | 延期可 | Claude |
| T-13 | MinimapPresenter SetPixel バッチ化 | 低 | 延期可 | Claude |
| T-14 | イベントアラートにリフレクション使用 | 低 | 延期可 | Claude |

---

## Milestone 9 キャリーオーバー

| ID | 内容 | 優先度 |
|---|---|---|
| M8-T-1 | MainGameLifetimeScope 配置 — ユーザー判断後に実施 | 高 |
| M8-T-2 | MainGameLifetimeScope 破棄経路の実装 | 高 |
| M8-T-3 | GameRandom を MainGameLifetimeScope へ移動・seed 適用 | 高 |
| M8-T-4 | IWorldGameSettingsRepository から WorldMapViewSettings 分離 | 高 |
| M8-T-5 | GameHUD の World 具象型依存を bridge DTO / interface に切り替え | 高 |
| M8-T-6 | GetAwaiter().GetResult() 除去 / UseCase シグネチャ統一 | 中 |
| M8-T-7 | GameHUDDriver → GameHUDEntryPoint (VContainer EntryPoint) | 中 |
| M8-T-8 | ActorDetailViewData aliasing 修正・テスト追加 | 中 |
| M8-T-9 | RecoveryRemainingSeconds 実装または field 削除 | 中 |
| M8-T-10 | `>` 比較演算子の残存修正 | 低 |
| M8-T-11 | coding-rules.md Application 層 DTO suffix ルール明記 | 低 |
| M8-T-12 | GetInnGuestListUseCase リスト再利用バッファ化 | 低 |
| M8-T-13 | MinimapPresenter SetPixel バッチ化 | 低 |
| M8-T-14 | イベントアラートテキストのリフレクション除去 | 低 |

---

## 2026-05-25 現行実装確認ログ

Codex が Milestone 9 着手前に現行コードを確認した結果、上記キャリーオーバー一覧の多くは後続対応で解消済みだった。
以降の作業者は、この節を現在状態の入口として扱う。

### 確認コマンド

- `rg -n "MainGameLifetimeScope|MainGameLifetimeScopeController|MainGameAssetScopeHolder|DungeonInn\.MainGame|GameHUDDriver|GetAwaiter\(\)\.GetResult\(\)|GetType\(\)\.Name|mapTexture\.SetPixel\(" Client/Assets/DungeonInn/Runtime/Scripts`
- `rg -n "BeginSession|EndSession|CleanupBeforeReboot|Register<GameRandom>|gameRandom.Initialize|interface IWorldGameSettingsRepository|GetWorldMapViewSettings|interface IActorScreenPositionProvider|TryGetScreenPosition\(LayerPosition|RegisterEntryPoint<GameHUDEntryPoint>|GetRemainingSeconds|GameEventAlertFormatter.Format" Client/Assets/DungeonInn/Runtime/Scripts`

### 現行ステータス

| ID | 2026-05-25 現行コード確認結果 |
|---|---|
| M8-T-1 | 解消済み。`MainGame` 系は現行 Runtime コードに存在せず、`GameSession` 系へ置換済み。`docs/roadmap/milestone8-roadmap-fix-2.md` も参照。 |
| M8-T-2 | 解消済み。`GameSessionLifecycle.BeginSession()` / `EndSession()` が session scope を所有し、`TitlePresenter` 失敗時、`ProductSceneManager` の非 session 遷移時、`RebootService` cleanup 時に破棄経路がある。 |
| M8-T-3 | 解消済み。`GameRandom` は `GameSessionLifetimeScope` に登録され、`WorldSimulationOrchestrator.InitializeAsync()` で `InitialWorldSettings.GameRandomSeed` を適用している。 |
| M8-T-4 | 解消済み。`IWorldGameSettingsRepository` から `WorldMapViewSettings` は削除済み。World 表示設定は View 側 `IWorldMapViewSettingsRepository` / `WorldMapViewSettingsRepository` が所有している。 |
| M8-T-5 | 一部対応。GameHUD は World 具象 `ActorSelectionService` を直接 inject していない。一方、`IActorScreenPositionProvider` の契約はまだ `LayerPosition` を露出しているため、HUD 専用 DTO 化は未完了。 |
| M8-T-6 | 解消済み。`WorldHudPresenter` / `InnStatusPanelPresenter` に `GetAwaiter().GetResult()` は残っておらず、該当 UseCase は同期 `Execute()` に変更済み。 |
| M8-T-7 | 解消済み。`GameHUDLifetimeScope` は `RegisterEntryPoint<GameHUDEntryPoint>()` を登録し、`GameHUDDriver` は Runtime コードに残っていない。 |
| M8-T-8 | 要再確認。今回の現行確認では対象外。Milestone 9 作業前に必要なら `GetActorDetailQuery` / `ActorDetailViewData` と EditMode test を確認する。 |
| M8-T-9 | 解消済み。`GetInnGuestListUseCase.Execute(List<InnGuestSummary>)` は `AdventurerRecoveryStateService.GetRemainingSeconds()` で回復残秒数を計算している。 |
| M8-T-10 | 要再確認。今回の現行確認では対象外。commit 前に Coding Rules スキャンで確認する。 |
| M8-T-11 | 解消済み。`docs/guidelines/coding-rules.md` に Application 層 DTO は `Summary` / `Status` suffix を使ってよい旨が追記済み。 |
| M8-T-12 | 解消済み。`GetInnGuestListUseCase.Execute(List<InnGuestSummary> buffer)` と `InnStatusPanelPresenter.guestBuffer` による再利用バッファ化済み。 |
| M8-T-13 | 解消済み。Runtime コード検索で `mapTexture.SetPixel(` は検出されない。 |
| M8-T-14 | 解消済み。`WorldHudPresenter` は `GameEventAlertFormatter.Format(gameEvent)` を使用し、`GetType().Name` は Runtime コードに残っていない。 |

### 次に残す判断

Milestone 9 着手前に、上表の `要再確認` と `一部対応` だけを現行コードで再確認する。
古い「Milestone 9 キャリーオーバー」表をそのまま未対応一覧として扱わないこと。
