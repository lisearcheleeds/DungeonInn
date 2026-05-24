# Milestone 8 完了レビュー

**日付**: 2026-05-23
**レビュー担当**: Claude Code
**対象**: milestone8-roadmap.md 全範囲

---

## 総合判定: **条件付き完了**

機能面はすべて動作しているが、アーキテクチャ上の未完了項目が2件残存している。
Milestone 9 着手前に対処することを推奨する。

---

## 1. 機能実装チェック

### HUD（常時表示）— 完了 ✅

| 項目 | 状態 |
|---|---|
| ゲーム内時刻・日付表示 | ✅ WorldHudPresenter / WorldHudView |
| ギルド資金表示 | ✅ GetInnEconomyStatusUseCase 経由 |
| 速度コントロール（一時停止・等速・倍速） | ✅ ToggleGamePauseUseCase / SetGameTimeScaleUseCase |
| 簡易アラート（イベント通知） | ✅ IEventSubscriber 経由 3 秒表示 |

### Inn ステータスパネル — 完了 ✅

| 項目 | 状態 |
|---|---|
| 宿泊中冒険者リスト（名前・HP・回復状態） | ✅ GetInnGuestListUseCase / InnStatusPanelView |
| 当日収支サマリ | ✅ GetInnEconomyStatusUseCase / InnEconomyStatus |

宿泊中判定: `AdventurerLifecycleState.Recovering` のみ対象（`WaitingForInn` は入室前のため除外、Domain Entity への新規プロパティ追加なし）。

### ミニマップ — 完了 ✅

| 項目 | 状態 |
|---|---|
| Ground / Dungeon 2D 俯瞰 | ✅ Texture2D 直接描画、セル色正確 |
| Actor ドット表示 | ✅ IActorSelectionCandidateProvider 経由、色分け |
| レイヤー切り替え検出 | ✅ MapLayerViewRegistry.ActiveLayerId nullable 対応済み |

### UI アーキテクチャ基盤 — 部分完了

| 項目 | 状態 |
|---|---|
| MainGameLifetimeScope（ゲームロジック共有親） | ✅ 実装済み、Application UseCase・EventBus 登録済み |
| View が IGameWorldStateReader を直接読まない | ✅ T-2 対応済み。IActorSelectionCandidateProvider 経由 |
| WorldAddressableViewFactory 経由の View 生成 | ✅ HUD・InnStatus・Minimap すべて Factory 経由 |
| VAS-1（VisualAssetSetup 非破壊化） | ✅ ValidationOnly、Editor/OneShot パターン確立 |

---

## 2. キャリーオーバー項目チェック

| ID | 内容 | 状態 |
|---|---|---|
| T-2 | WorldActorSelectionInputHandler / ActorSelectionService の IGameWorldStateReader 依存除去 | ✅ 完了 |
| T-4残 | GetActorDetailQuery の ToArray() フレームごと割り当て除去 | ✅ 完了 |
| T-5残 | WorldAddressableViewFactory.PlayerEventLogViewPrefab public プロパティ削除 | ✅ private field 化 |
| P3-1 | WorldProjectileViewPool / WorldAreaEffectViewPool の Stack→Queue | ✅ 完了 |
| P7-1 | PlayerEventLogStore.Add() の公開範囲縮小 | ✅ private 化 |
| VAS-1 | VisualAssetSetup 自動再生成経路除去 | ✅ 完了 |
| **S5-1** | **WorldHudCanvasProvider の FindFirstObjectByType 除去** | **❌ 未対応** |

---

## 3. 未完了項目（要対処）

### S5-1 — WorldHudCanvasProvider.FindFirstObjectByType（重要度: 高）

**問題**: `WorldHudCanvasProvider.Initialize()` が `Object.FindFirstObjectByType<WorldUIModuleScene>()` を使用している。これは Lighthouse 禁止パターン（[P8] LifetimeScope ルール違反）。

**場所**: `Runtime/Scripts/View/Scene/MainScene/World/WorldHudCanvasProvider.cs:18`

**対処方針**: LifetimeScope UI 分離（次項）と連動して解消する。`WorldUIModuleScene` を DI 注入経由で受け取るか、Scene 参照で渡す。

---

### LifetimeScope UI 分離 — WorldLifetimeScope の HUD/UI Presenter 残存（重要度: 高）

**問題**: ロードマップ完了条件「`WorldLifetimeScope` は 3D 表現固有の登録に限定」が未達。

現在 `WorldLifetimeScope` に残存している UI Presenter:
- `WorldHudCanvasProvider`
- `ActorHUDViewPool`
- `WorldActorStatusPresenter`
- `ActorDetailPopupPresenter`
- `PlayerGameEventLogPresenter`
- `WorldHudPresenter`
- `InnStatusPanelPresenter`
- `MinimapPresenter`

**原因**: Milestone 8 の UI 実装タスク（task_0005〜0007）を `WorldLifetimeScope` に仮登録して進めた。`WorldUILifetimeScope` への移行が残っている。

**現状の `WorldUILifetimeScope`**: `WorldUIModuleScene` のみ登録。

**対処方針**: Milestone 9 開始前に「WorldUILifetimeScope 分離タスク」として切り出し、上記の UI Presenter 群を `WorldUILifetimeScope` へ移動する。移動後は S5-1 の FindFirstObjectByType も DI 注入で解消できる。

---

## 4. 動作確認結果

| チェック項目 | 結果 |
|---|---|
| uloop compile | ✅ ErrorCount=0 / WarningCount=0 |
| uloop run-tests EditMode | ✅ 298/298 pass |
| Play 30秒 GameWorldState initialized | ✅ 確認 |
| エラーログ | ✅ なし |

Warning は HUDCanvas fallback・MapMaterial fallback・TextMeshPro・TextTable の既知のもののみ。

---

## 5. Milestone 9 引き継ぎ事項

以下を Milestone 9 のキャリーオーバーとして記録する。

| ID | 内容 | 優先度 |
|---|---|---|
| M8-S5-1 | WorldHudCanvasProvider.FindFirstObjectByType 除去 | 高（Lighthouse 禁止パターン） |
| M8-LS-1 | WorldLifetimeScope の UI Presenter を WorldUILifetimeScope へ移動 | 高（ロードマップ完了条件） |
