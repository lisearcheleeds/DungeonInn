# Milestone 8 完了レビュー（2回目）

**日付**: 2026-05-24
**レビュー担当**: Claude Code
**対象**: git diff HEAD（milestone8-completion-review-claude.md より後の差分）
**レビューフォーマット**: self-review-guidelines.md 準拠（専任エージェント分担 → 統合レビュー）

---

## 前回キャリーオーバー確認

| ID | 内容 | 状態 |
|---|---|---|
| M8-S5-1 | WorldHudCanvasProvider.FindFirstObjectByType 除去 | ✅ 解消 — `WorldHudCanvasProvider.cs` が削除されている |
| M8-LS-1 | WorldLifetimeScope の UI Presenter を WorldUILifetimeScope へ移動 | ✅ 解消 — UI Presenter 群が `GameHUDLifetimeScope` へ移動済み |

両キャリーオーバーは現行コードで解消済み。以降は今回差分での新規指摘を記録する。

---

## 新規概念追加ゲート記録

追加した主要な型:

| 型名 | 既存類似概念 | 意味差分 | 代替しなかった理由 | 削除・統合条件 |
|---|---|---|---|---|
| `InnGuestSummary` | `ActorViewData` | 宿屋滞在中冒険者に特化した表示 DTO。HP比率・回復残時間・名前のみ。ActorViewData は空間座標・行動タイプ含む | 宿屋パネル専用の薄い DTO。ActorViewData を流用すると空間座標が不要になり所有者が曖昧になる | Inn ゲスト表示が統合 Actor DTO に組み込まれる場合 |
| `MainGameAssetScopeHolder` | `IAssetScope` (ProductLifetimeScope 直接生成) | ゲームセッション寿命に対応する AssetScope の所有者として単独クラス化 | ProductLifetimeScope で直接生成すると寿命がゲームセッションでなく Product になる | MainGameLifetimeScope 自体が AssetScope ライフサイクルを管理できる場合 |
| `GameHUDLifetimeScope` | `WorldUILifetimeScope` | GameHUD ModuleScene 専用スコープ。WorldUI とは別 ModuleScene | WorldUI は World 固有 UI 用。HUD は ModuleScene として独立しているため別スコープが必要 | ModuleScene 統合が行われた場合 |
| `ActiveLayerProviderProxy` / `ActorScreenPositionProviderProxy` / `NavigationPathProviderProxy` | 直接クロスシーン参照 | 登録・取得を分離する Bridge Proxy。MainGameLifetimeScope に登録し、WorldScene 所有実装を後から差し込む | 直接参照は Lighthouse 禁止パターン（クロスシーン Canvas/Presenter 参照） | ModuleScene 間通信の共通 Proxy 基盤が導入された場合 |
| `IActorSelectionCandidateProvider` | `IActorViewDataProvider` | `CopyTo` パターンで MinimapPresenter 向けにバッファコピーを提供する狭い IF | MinimapPresenter が全アクター読み取り権を持つ必要はなく、候補集合コピーだけでよい | Minimap が ActorViewDataProvider に直接アクセスできる設計変更が行われた場合 |

---

## [先行フェーズ] Coding Rules 専任レビュー

`coding-rules.md` 記載のスキャンパターンを差分ファイルに対して実行した。

スキャン結果:

- `private ` prefix: 新規ファイルに `private` 明示なし ✅
- `_` prefix fields: 新規ファイルに `_` prefix フィールドなし ✅
- `>` 比較演算子: 新規ファイルに `>` 使用なし ✅ （`<=` / `<` のみ使用）
- expression-body (`=>`): メソッドへの式ボディなし ✅
- `[Inject]` 付与: DI コンストラクタに `[Inject]` あり ✅
- namespace 命名: 各ファイルのフォルダ構成と namespace が一致 ✅
- `ViewData` suffix: View 表示用 struct `InnGuestSummary` は `Summary` suffix — **後述 CR-1 参照**

**指摘: CR-1 — `InnGuestSummary` の suffix**

重大度: 低

問題:

`coding-rules.md` は View 向け表示 struct のsuffix として `ViewData` を定めている。`InnGuestSummary` は Application → View へ渡す表示 DTO だが suffix が `Summary` である。他の View 表示用 struct（`ActorViewData`, `GameTimeState` など）と命名規約が統一されていない。

原因:

`InnGuestSummary` は Application 層（`DungeonInn.Application.World`）に置かれており、Domain / Application / View 境界を意識して `Summary` という業務的名称を選択した。ただし `ViewData` suffix は View に特化した型を示すシグナルであり、Application 層の型が `ViewData` を持つことで混乱が生じる可能性もある。

解決案:

- 暫定: `InnGuestSummary` の名称を `InnGuestViewData` に変更し規約を揃える
- または: Application 層の業務結果型には `Summary` / `Status` を使い、View 用に別の `ViewData` 変換型を置くルールを coding-rules.md に追記してルールを明確化する
- 後者を採用する場合は `InnEconomyStatus`（既存）も同様の扱いになるため、統合レビューでの判断待ち

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/InnGuestSummary.cs`
- `docs/guidelines/coding-rules.md`

完了条件:

- [ ] `InnGuestSummary` を `InnGuestViewData` にリネームするか、`coding-rules.md` に Application 層 DTO の suffix 例外ルールを追記している
- [ ] リネームした場合、`GetInnGuestListUseCase`・`InnStatusPanelPresenter` の参照箇所を更新し `uloop compile` が成功している

---

## Lighthouse 専任レビュー

### L-1 — `IWorldGameSettingsRepository` が View 層型 `WorldMapViewSettings` を返す

重大度: 高

問題:

`IWorldGameSettingsRepository`（`DungeonInn.Application.World`）が `GetWorldMapViewSettings()` メソッドで `WorldMapViewSettings`（`DungeonInn.View.Scene.MainScene.World`）を返す。Application 層のインターフェースが View 層の型を使用しており、Clean Architecture の依存方向規則（View → Application → Domain）に違反している。

`WorldMapViewSettings` の内容:
- `ChunkTileSize` — チャンクの描画タイルサイズ（View 描画専用）
- `ChunkBuildsPerFrame` — 1フレームに何チャンク構築するか（View 描画専用）
- `TileHeightMeters` — タイル高さメートル（View 3D 空間専用）

これらはすべて View 描画に特化したパラメータであり、Application 層ビジネスロジックからは使用されない。

原因:

Config Repository（P6 パターン）実装時に、WorldGameSettings SO から読み込む全設定を 1 つのリポジトリインターフェースにまとめた際に、View 専用の `WorldMapViewSettings` も含めてしまった。

解決案:

案A（推奨）: インターフェースを分割する
- `IWorldGameSettingsRepository`（Application 層）: `GetWorldMapViewSettings()` を削除
- `IWorldMapViewSettingsRepository`（View 層）: `GetWorldMapViewSettings()` のみを持つ View 専用 IF を新設
- `WorldGameSettingsRepository` は両インターフェースを実装し `MainGameLifetimeScope` に登録

案B: `WorldMapViewSettings` を `DungeonInn.Application.World` に移動
- ただし `ChunkTileSize` / `ChunkBuildsPerFrame` は View 概念であり Application に持ち込むべきではないため非推奨

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/IWorldGameSettingsRepository.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapViewSettings.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/MainGame/Settings/WorldGameSettingsRepository.cs`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/domain-design-guidelines.md`

完了条件:

- [ ] `IWorldGameSettingsRepository` に `using DungeonInn.View.*` が存在しない
- [ ] `WorldMapViewSettings` への参照が Application 層ファイルに残存しない
- [ ] View 層（または `WorldGameSettingsRepository` 実装側）で `WorldMapViewSettings` を取得するための別 IF が存在する
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### L-2 — キャリーオーバー確認（解消済み）

M8-S5-1（`FindFirstObjectByType`）および M8-LS-1（`WorldLifetimeScope` UI Presenter 残存）は現行差分で解消済み。`WorldHudCanvasProvider.cs` の削除を確認。`GameHUDLifetimeScope` への Presenter 移動を確認。再掲なし。

---

## Domain Design 専任レビュー

差分ファイルにおける Domain 設計の検査結果:

- `InnGuestSummary` は Domain Entity を直接公開せず、Application 層で DTO 変換している ✅
- `GetInnGuestListUseCase` は Domain の `AdventurerBehavior.LifecycleState` を読むが、Domain 型の外部公開はない ✅
- Bridge Proxy（`ActiveLayerProviderProxy` など）は View 層インターフェースに閉じており Domain への依存なし ✅
- `MainGameLifetimeScope` が `using DungeonInn.View.Scene.MainScene.World` をインポートしているが、これは LifetimeScope（Composition Root）であり依存方向違反には当たらない

**Domain Design 専任レビュー: 指摘なし**

---

## Application Boundary 専任レビュー

### AB-1 — `GetAwaiter().GetResult()` による UniTask 同期ブロッキング

重大度: 中

問題:

`WorldHudPresenter` および `InnStatusPanelPresenter` で UniTask を `.GetAwaiter().GetResult()` により同期的に解決している。

該当箇所:
- `WorldHudPresenter.UpdateHud()` L82: `getGameTimeStateUseCase.ExecuteAsync().GetAwaiter().GetResult()`
- `WorldHudPresenter.UpdateHud()` L88: `getInnEconomyStatusUseCase.ExecuteAsync().GetAwaiter().GetResult()`
- `WorldHudPresenter.OnPauseClicked()` L111: `toggleGamePauseUseCase.ExecuteAsync().GetAwaiter().GetResult()`
- `WorldHudPresenter.OnSpeedNormalClicked()` L116: `setGameTimeScaleUseCase.ExecuteAsync(1f).GetAwaiter().GetResult()`
- `WorldHudPresenter.OnSpeedFastClicked()` L119: `setGameTimeScaleUseCase.ExecuteAsync(2f).GetAwaiter().GetResult()`
- `InnStatusPanelPresenter.UpdatePanelCore()` L81: `getInnEconomyStatusUseCase.ExecuteAsync().GetAwaiter().GetResult()`

`application-boundary-guidelines.md` では UseCase を UniTask で定義することを標準としているが、「純粋なインメモリ同期読み取り」の UseCase に `ExecuteAsync()` シグネチャを持たせると、呼び出し元が同期ブロッキングせざるを得ない。

原因:

`GetGameTimeStateUseCase` / `GetInnEconomyStatusUseCase` などのクエリ系 UseCase が `UniTask<T>` を返すシグネチャになっているが、内部実装は同期インメモリ読み取りの可能性が高い。View Presenter から async コンテキストなしで呼ぶ必要があるため `GetAwaiter().GetResult()` になっている。

解決案:

- クエリ系 UseCase（`GetGameTimeStateUseCase` など）が実際にインメモリ同期読み取りであれば、`UniTask<T>` ではなく `T Execute()` シグネチャに変更する
- コマンド系 UseCase（`ToggleGamePauseUseCase`, `SetGameTimeScaleUseCase`）はUI ボタンコールバックから呼ばれるため、`UniTask.Void(async () => { ... })` でラップするか、同期シグネチャに変更する
- 将来これらが非同期になる場合は、Presenter のコールバックを async に変える

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/WorldHudPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/InnStatusPanelPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GetGameTimeStateUseCase.cs`（要確認）
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/GetInnEconomyStatusUseCase.cs`（要確認）
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `WorldHudPresenter.cs` に `GetAwaiter().GetResult()` が存在しない
- [ ] `InnStatusPanelPresenter.cs` に `GetAwaiter().GetResult()` が存在しない
- [ ] クエリ系 UseCase が同期シグネチャに変更されているか、または async コンテキストで適切に await されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### AB-2 — `GetInnGuestListUseCase.Execute()` の毎呼び出しリスト生成

重大度: 低

問題:

`GetInnGuestListUseCase.Execute()` が毎回 `new List<InnGuestSummary>()` を生成してアクター全件走査をする。`InnStatusPanelPresenter.UpdatePanel()` から 0.5 秒ごとに呼ばれる。

原因:

`IReadOnlyList<T>` を返す UseCase の自然な実装として新規リストを生成している。同プロジェクト内の `IActorSelectionCandidateProvider.CopySelectionCandidatesTo(List<T>)` が採用している再利用バッファパターンとの不統一。

解決案（0.5 秒間隔なので緊急ではない）:

- 暫定: 現状維持（GC 圧は低い）
- 根治: `Execute(List<InnGuestSummary> buffer)` パターンに変更し呼び出し元に再利用バッファを持たせる

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetInnGuestListUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/InnStatusPanelPresenter.cs`

完了条件:

- [ ] リスト再利用を採用する場合: `InnStatusPanelPresenter` に `List<InnGuestSummary>` フィールドがあり `Execute()` がバッファ受け取りシグネチャになっている
- [ ] または: 暫定維持として別タスクに積み上げられている

---

### AB-3 — `InnGuestSummary.RecoveryRemainingSeconds` が常に `0f`

重大度: 低

問題:

`GetInnGuestListUseCase.Execute()` で `InnGuestSummary` を生成する際、`RecoveryRemainingSeconds` に `0f` がハードコードされている。マイルストーン 8 ロードマップでは Inn ステータスパネルに「残滞在時間」表示が含まれており、フィールドは宣言されているが未計算のまま。

原因:

回復残時間の計算実装が残タスクとして後回しになっている（実装プレースホルダー）。

解決案:

`AdventurerRecoveryStateService` または対応するサービスから残回復秒数を読み取り、`RecoveryRemainingSeconds` に実際の値を設定する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetInnGuestListUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/InnGuestSummary.cs`

完了条件:

- [ ] `GetInnGuestListUseCase.Execute()` で `RecoveryRemainingSeconds` に `0f` 以外の計算値が設定されている
- [ ] InnStatusPanelView に残回復時間が表示されることを PlayMode で確認している

---

## Implementation Quality 専任レビュー

### IQ-1 — `MinimapPresenter.DrawActorDot()` の `SetPixel()` 個別呼び出し

重大度: 低

問題:

`MinimapPresenter.DrawActorDot()` がネストループで `mapTexture.SetPixel(x, z, color)` を呼んでいる（アクター1体あたり最大 3×3 = 9 回）。`UpdateActorDots()` は `SetPixels(basePixels)` でベースレイヤーをリセットした後、各アクターのドット描画で `SetPixel()` を複数回呼んでいる。

`implementation-quality-guidelines.md` のフレームループ内オブジェクト生成・高頻度呼び出しの観点から、`SetPixel()` のAPIコールをなるべくバッチにまとめる方が望ましい。

原因:

小さなドット描画の便宜上 `SetPixel()` を使用。`SetPixels()` でまとめる実装より直感的。

解決案:

アクターループ前に `basePixels` の内容をワーキングバッファ（`Color[]`）にコピーし、ドット描画をそのバッファへ配列操作で書き込み、最後に `SetPixels(workBuffer)` を 1 回だけ呼ぶ。現状では `basePixels` に直接 `SetPixels` した後 `SetPixel` で上書きしているため、ベースと差分が混在している。

現状のアクター数規模では性能上の問題は発生しないが、アクター増加時のスケーラビリティのために改善する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/MinimapPresenter.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `DrawActorDot()` 内の `mapTexture.SetPixel()` 呼び出しが存在しない
- [ ] ドット描画に `Color[]` バッファへの配列書き込み + `SetPixels()` 1回のパターンを使用している

---

### IQ-2 — `WorldHudPresenter.OnGameEvent()` のリフレクション使用

重大度: 低

問題:

`WorldHudPresenter.OnGameEvent()` が `hudView.ShowAlert(gameEvent.GetType().Name)` でイベントアラートテキストを設定している。C# 型名（例: `AdventurerArrivedAtInnEvent`）がそのままユーザー向けアラートとして表示される。ローカライズ不可、人間に読みやすい表現でない、内部命名規約に依存するプレースホルダー実装。

原因:

アラートテキストの実装が後回しとなっており、型名でのデバッグ表示が暫定として残っている。

解決案:

- `IGameEvent` インターフェースに `string AlertText { get; }` を追加し、各イベント実装が日本語テキストを返す（多言語対応する場合は `ITextTableService` 経由のキー参照）
- または: `GameEventAlertTextResolver` のような専用マッパーを置き、イベント型→テキストのマッピングを一元管理する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/WorldHudPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/IGameEvent.cs`（要確認）
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `WorldHudPresenter` に `GetType().Name` を表示テキストとして使う呼び出しが存在しない
- [ ] アラートテキストが型名ではなく人間可読なテキストを返している

---

## 統合レビュー

### 重複排除・重大度調整

| ID | 内容 | 重大度 | 判定 |
|---|---|---|---|
| L-1 | `IWorldGameSettingsRepository` が View 型 `WorldMapViewSettings` を返す | 高 | **Milestone 9 着手前に修正必須** |
| AB-1 | `GetAwaiter().GetResult()` 同期ブロッキング | 中 | **Milestone 9 着手前に修正必須** |
| CR-1 | `InnGuestSummary` の suffix 規約 | 低 | Milestone 9 で対応またはルール明確化 |
| AB-2 | `GetInnGuestListUseCase` 毎呼び出しリスト生成 | 低 | 次マイルストーンへ延期可 |
| AB-3 | `RecoveryRemainingSeconds` が常に 0f | 低 | 未実装。別タスク化して追跡 |
| IQ-1 | `DrawActorDot()` の SetPixel 個別呼び出し | 低 | 次マイルストーンへ延期可 |
| IQ-2 | イベントアラートにリフレクション使用 | 低 | 次マイルストーンへ延期可 |

### guideline 優先順位判断

- **L-1 と CR-1 の関係**: `InnGuestSummary` は Application 層 DTO であり、`ViewData` suffix は View 層表示型に使うシグナル。Application 層 DTO は `Summary` / `Status` suffix を許容するルールを `coding-rules.md` に追記して明確化する方が望ましい（`InnEconomyStatus` という既存型との整合性あり）。`InnGuestSummary` のリネームは不要と判断するが、ルール明確化は推奨。
- **AB-1 と AB-3 の関係**: `GetAwaiter().GetResult()` の解消（AB-1）とクエリ UseCase のシグネチャ変更は連動する。AB-1 対応時に AB-3 の `RecoveryRemainingSeconds` 実装も同時に行うと工数効率が良い。

### 設計横断確認（統合レビュー観点）

- **Clean Architecture 依存方向**: L-1 の違反以外は依存方向が守られている ✅（要修正）
- **LifetimeScope 登録漏れ**: `GameHUDLifetimeScope` の Presenter 群は全登録済み、`MainGameLifetimeScope` の Application サービスも全登録済み ✅
- **フレームループ内の重い処理**: ミニマップの `SetPixel` ループ（IQ-1）と `InnStatusPanelPresenter` の 0.5 秒間隔更新のみ。ActorHUDViewPool や WorldActorStatusPresenter のフレーム更新は positional update のため許容範囲 ✅
- **Bridge Proxy の登録安全性**: `ActiveLayerProviderProxy` / `ActorScreenPositionProviderProxy` は `Register()` に重複呼び出しで例外を投げる。World シーン複数ロードの場合は要注意だが、現状シングルシーン構成では問題なし ✅

### 総合判定: **条件付き完了（L-1, AB-1 対処後）**

L-1（Application→View 依存方向違反）と AB-1（`GetAwaiter().GetResult()` 同期ブロッキング）の 2 件は Milestone 9 着手前に修正を要する。その他は次マイルストーンへの延期または別タスク化で進行可。

---

## キャリーオーバー一覧（Milestone 9 へ）

| ID | 内容 | 優先度 |
|---|---|---|
| M8-L-1 | `IWorldGameSettingsRepository` から View 型 `WorldMapViewSettings` を除去、インターフェース分割 | 高（依存方向違反） |
| M8-AB-1 | `WorldHudPresenter` / `InnStatusPanelPresenter` の `GetAwaiter().GetResult()` 除去 | 高 |
| M8-CR-1 | Application 層 DTO suffix ルールを `coding-rules.md` に明記 | 低 |
| M8-AB-2 | `GetInnGuestListUseCase` リスト再利用バッファ化 | 低 |
| M8-AB-3 | `InnGuestSummary.RecoveryRemainingSeconds` 実装（回復残時間計算） | 低（仕様未実装） |
| M8-IQ-1 | `MinimapPresenter.DrawActorDot()` の `SetPixel()` → `SetPixels()` バッチ化 | 低 |
| M8-IQ-2 | イベントアラートテキストのリフレクション除去 | 低 |
