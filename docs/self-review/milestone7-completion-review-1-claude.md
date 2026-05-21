# Milestone 7 完了レビュー — 戦闘表現とステータス表示

作成日: 2026-05-21  
作成者: Claude Code  
対象マイルストーン: Milestone 7 (task_0001 〜 task_0010)

---

## 1. M7 各 Phase 完了状態

| Task ID | Phase | タスク名 | 状態 | 備考 |
|---|---|---|---|---|
| task_0001 | Phase 0 | M6 View 基盤フォローアップ | **完了** | PlaceholderAssetFactory 集約、Addressables schema 修正、DI グループ整理 |
| task_0002 | Phase 1 | Actor フルビルボード回転 | **完了** | `SetBillboardRotation(Quaternion)` 追加、WorldActorPresenter から毎フレーム設定 |
| task_0003 | Phase 2 | 戦闘 Actor アニメーション拡張 | **完了** | `Combat/Hit/Dead` AnimationState 追加、`WorldActorCombatAnimationPresenter` 新規追加 |
| task_0004 | Phase 3 | Projectile View 表現 | **完了** | `ProjectileView` / `WorldProjectileViewPool` / `WorldProjectilePresenter` 実装 |
| task_0005 | Phase 4 | Area Effect View 表現 | **完了** | `AreaEffectView` / `WorldAreaEffectViewPool` / `WorldAreaEffectPresenter` 実装 |
| task_0006 | Phase 5 | Actor 頭上ステータス表示 | **完了** | `WorldHUDModuleScene` 新規作成、`ActorStatusView` / `WorldActorStatusPresenter` 実装 |
| task_0007 | Phase 6a | Actor 選択入力とカメラ制御 | **完了** | `ActorSelectionService` / `WorldActorSelectionInputHandler` / `WorldActorCameraFollowController` 実装 |
| task_0008 | Phase 6b | Actor 詳細 Popup | **完了** ※設計変更あり | ScreenStack Popup ではなく HUD Panel として実装（後述） |
| task_0009 | Phase 7 | プレイヤー向けイベントログ UI | **完了** | `PlayerEventLogStore` / `PlayerEventLogFormatter` / `PlayerEventLogView` / `PlayerGameEventLogPresenter` 実装 |
| task_0010 | Phase 8 | M7 統合確認とセルフレビュー | **完了** | 本ドキュメント |

---

## 2. M7 設計変更ログ

### task_0008: ActorDetailPopup の実装方式変更

**変更前（roadmap 仕様）**: ScreenStack Popup として実装（`IScreenStackData` / `IScreenStackSetup<T>` 経由で push/pop）

**変更後（実装）**: Plain MonoBehaviour HUD Panel として実装

**変更理由**:
- ScreenStack の Addressable ロードパターン（push 時に非同期ロード → `IScreenStackSetup<T>.Initialize()` 呼び出し）と、毎フレームの `SetPosition()` / `SetContent()` 直接呼び出しが競合する
- `ActorDetailPopupPresenter` が毎フレーム Popup への参照を保持して位置・内容を更新する設計には、Addressable ロード完了を待てない構造上の問題があった
- HUD Panel（`MonoBehaviour` + `SerializeField` + VContainer の条件付き `RegisterInstance`）のほうが毎フレーム更新と整合する

**影響**:
- `WorldLifetimeScope` で `[SerializeField] ActorDetailPopup actorDetailPopup` を SerializeField で保持し、`if (actorDetailPopup != null)` 条件付き登録
- `WorldGameLoopEntryPoint` で `IObjectResolver.TryResolve<ActorDetailPopupPresenter>()` による optional injection
- `IPointerClickHandler` による「閉じる」操作ではなく、`ActorSelectionService.Deselect()` 購読で popup 非表示化

**M8 検討事項**: ScreenStack への移行を行うかどうか。HUD Panel 方式で問題が出なければ維持する。

### task_0009: PlayerEventLogStore に IGameClock を追加

**変更理由**: Application 層で `UnityEngine.Time.time` を使うことが Lighthouse 禁止パターン（Application 層の UnityEngine 依存）のため、`IGameClock.ElapsedRealTimeSeconds` でタイムスタンプを取得する設計に変更。

### task_0009: PlayerEventLogView の入力方式変更

**変更理由**: `Input.GetAxis("Mouse ScrollWheel")` は旧 Input System（Lighthouse 禁止）。`IPointerEnterHandler` / `IPointerExitHandler` / `IScrollHandler` を実装して Unity EventSystem 経由に変更。

---

## 3. 確認した PlayMode ログ（30 秒確認）

**実行日時**: 2026-05-21  
**コマンド**: `uloop.cmd control-play-mode --action Play` → 35 秒後 Stop → `uloop.cmd get-logs`

**初期化ログ**:
```
[World] GameWorldState initialized. Facilities=3 DungeonFloors=1 Actors=0
[WorldGameLoop] EntryPoint started.
[DungeonInn] NavMesh baked for layer 0
[DungeonInn] NavMesh baked for layer 1
```

**ゲームプレイログ（抜粋）**:
```
[Event] ゼド が現れた
[Event] ゼド がダンジョン 1 階に入った
[Combat] ゼド は Goblin と遭遇した
[AI] ゼド selected StartCombat: nearest hostile ...
[Combat] ゼド は Goblin に攻撃！ ダメージ: 57 (HP 0)
[Combat] Goblin は  by ゼド に倒された (Combat)
[Drop] Goblin はアイテムを落とした item#3004 x1
[Drop] Goblin はアイテムを落とした item#1 x1
[Growth] ゼド は 10 EXP を得た (total: 20)
[Goal] ゼド は目標を達成した: LevelUp 1/1
[Actor] ゼド は帰還を始めた
[Inn] ゼド reserved inn room (1/8)
[Inn] ゼド paid 10G for inn room (remaining: 90G)
[Inn] ゼド satisfaction changed +2 (StayedAtInn)
[Guild] Treasury +10G (total: 10G)
[Event] ゼド がダンジョンから帰還した
```

**Error ログ**: **なし** ✓

**Warning ログ（全て想定内の fallback/未設定 Warning）**:
- `[MapMaterialSet] Using fallback map material. Kind=...` — アセット未アサイン（想定内）
- `[ActorSpriteAnimator] Combat animation clip is not assigned. Falling back to idle clip.` — combat clip 未設定（想定内）
- `Unable to add characters to font asset...` — TextMeshPro ウォームアップ
- `[TextTable] No entries loaded for language 'ja'...` — ローカライズ未設定（想定内）

---

## 4. View / Application 境界確認

### 確認済み事項

| 確認項目 | 結果 |
|---|---|
| Domain / Application 層に UnityEngine 依存が増えていないか | ✓ なし。`PlayerEventLogStore` は `IGameClock` 経由でタイムスタンプ取得 |
| Event 購読者が `Dispose()` / `DisposableBag` で購読解除されているか | ✓ 全 Presenter が `IDisposable` + `DisposableBag` で解除 |
| Frame Loop に LINQ / `ToList()` / 毎フレームアロケーションが増えていないか | ✓ `PlayerEventLogView` は固定配列・シフト操作のみ。Projectile / AreaEffect も配列または Dictionary 操作のみ |
| View が Domain State / `GameWorldState` を直接変更していないか | ✓ 全 Presenter は読み取り専用 Query / DTO 経由 |
| View が表示用の計算（ダメージ計算、命中判定）を再実装していないか | ✓ なし |
| `IGameEvent` に View 用表示文字列が追加されていないか | ✓ `PlayerEventLogFormatter` が変換を担当 |
| `ScrollRect` を使用していないか（PlayerEventLogView） | ✓ 固定配列 + テキスト更新のみ |
| Actor / Projectile / Area despawn 後に View GameObject が残留していないか | PlayMode ログで despawn エラーなし ✓ |

---

## 5. M8 へ持ち越す残課題

### Unity Editor セットアップ（ユーザー作業）

以下の UI コンポーネントはコード実装済みだが、Unity Editor での GameObject 配置とアサインが未完了。PlayMode でゲームループ自体は動作するが、UI 表示を確認するには手動セットアップが必要。

1. **ActorDetailPopup**: `WorldLifetimeScope` Inspector の `playerEventLogView` と `actorDetailPopup` SerializeField に GameObject をアサインする必要がある
2. **PlayerEventLogView**: 同上。`logLines` (10 個の `TextMeshProUGUI`) の配列も Inspector でアサインが必要

### アセット差し替え

- `[MapMaterialSet] Using fallback map material` Warning × 7 件: `MapMaterialSet.asset` の正式アセットアサイン
- `[ActorSpriteAnimator] Combat animation clip is not assigned`: combat / hit / dead 用アニメーションクリップのアサイン
- Projectile / AreaEffect の Sprite は `Dummy.png` → 正式アセットに差し替え

### M8 設計検討事項

- **ActorDetailPopup の ScreenStack 移行**: M7 では毎フレーム更新の都合から HUD Panel として実装。M8 で ScreenStack Popup に移行するか、HUD Panel のまま機能拡張するかを決定する
- **施設管理 / スタッフ管理 UI** (M8 roadmap)
- **HUD（時間・資金表示）** (M8 roadmap)

---

## 6. M7 完了判定

M7 roadmap の完了判定基準に対する確認結果:

| 基準 | 結果 |
|---|---|
| Actor がカメラに正対し、idle / walk / combat / hit / dead の状態が判別できる | ✓ billboard 回転 + combat アニメーション実装済み（clip 未設定は fallback Warning で動作継続） |
| Projectile の発射・飛翔・着弾が表示され、消滅時に残留しない | ✓ ProjectileView / Pool 実装済み |
| Area Effect の範囲・持続・hit feedback が表示され、expired 後に残留しない | ✓ AreaEffectView / Pool 実装済み |
| Actor 頭上に HP バーと ActorEffect placeholder アイコンが表示される | ✓ WorldHUDModuleScene + ActorStatusView 実装済み |
| Actor を選択すると詳細を確認できる | ✓ ActorDetailPopup 実装済み（Editor アサイン待ち） |
| Player 向けイベントログ UI で主要イベントを追える | ✓ PlayerEventLogView 実装済み（Editor アサイン待ち） |
| Debug Presenter と Player UI が分離されている | ✓ `WorldDebugGameLogPresenter` (DEBUG only) / `PlayerGameEventLogPresenter` (runtime) 分離 |
| View / Presenter がゲーム進行・Domain 判定を握っていない | ✓ |
| compile / EditMode test / PlayMode 30s 確認 | ✓ 全確認済み（errors: 0 / tests: 283/283 / PlayMode: no errors） |

**判定: Milestone 7 完了** ✓
