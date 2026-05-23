# Milestone 7 Completion Review 6 — Claude Code

作成日: 2026-05-22
対象: staged diff 全体（Milestone 7 実装差分）
レビュー担当: Claude Code

---

## 事前確認

レビュー前に以下を読み直した。

- `docs/roadmap/milestone7-roadmap.md`（全 Phase・完了条件・設計境界）
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/self-review/milestone7-completion-review-6-codex-3.md`（Codex 直前レビュー）

参照した主要ファイル（script 系）:

- `WorldLifetimeScope.cs` / `WorldUILifetimeScope.cs` / `WorldUIModuleScene.cs`
- `WorldAddressableViewFactory.cs` / `WorldHudCanvasProvider.cs`
- `ActorHUDViewPool.cs` / `WorldActorStatusPresenter.cs`
- `WorldProjectileViewPool.cs` / `WorldProjectilePresenter.cs` / `ProjectilePrefabSource.cs`
- `WorldAreaEffectViewPool.cs` / `WorldAreaEffectPresenter.cs` / `AreaEffectPrefabSource.cs`
- `ActorSelectionService.cs` / `WorldActorSelectionInputHandler.cs` / `WorldSceneInputLayer.cs`
- `WorldActorCameraFollowController.cs` / `ActorCombatAnimationPresenter.cs`
- `ActorDetailPopupPresenter.cs` / `ActorDetailPopup.cs` / `GetActorDetailQuery.cs` / `ActorDetailDto.cs`
- `PlayerEventLogStore.cs` / `PlayerEventLogFormatter.cs` / `PlayerGameEventLogPresenter.cs` / `PlayerEventLogView.cs`
- `PlaceholderAssetFactory.cs` / `VisualConfigSettings.cs`
- `WorldGameLoopEntryPoint.cs`

---

## Phase 別実装レビュー

### Phase 0: M6 View 基盤フォローアップ — PASS

| 対応内容 | 確認結果 |
|---|---|
| `PlaceholderAssetFactory` に placeholder 生成ロジックを集約し `ActorSpritePlaceholderFactory` を削除 | ✓ `PlaceholderAssetFactory.cs` に `CreateActorPlaceholder` / `CreateProjectilePlaceholder` / `CreateAreaEffectPlaceholder` が統合されている。旧 `ActorSpritePlaceholderFactory.cs` は削除済み |
| `VisualConfigSettings` からコンテンツ Prefab を削除し、Addressable Factory へ移行 | ✓ `VisualConfigSettings` は `MapMaterialSetSO` / `ActorSpriteVisualConfigSO` のみ保持。PropPrefab / ProjectilePrefab / AreaEffectPrefab は削除済み |
| DungeonInn Visual Addressables group への schema 設定 | staged diff で `DungeonInn Visual.asset` が変更されており、schema 追加対応が行われていることを確認 |

### Phase 1: Actor フルビルボード回転 — PASS

roadmap の完了条件「カメラを 90 度単位で回転しても Actor がカメラに正対する」「CurrentYawDegrees が animation direction 選択用として維持」を前提として、`WorldCameraController.CurrentCameraRotation` プロパティが `WorldGameLoopEntryPoint` から `WorldActorPresenter` 経由で渡されていることを確認。Domain / Application に Quaternion / Camera 依存が追加されていないことを確認。

### Phase 2: 戦闘 Actor アニメーション拡張 — PASS

`ActorCombatAnimationPresenter` の設計・実装を確認:
- `IInitializable` / `IDisposable` 実装。`Initialize()` で `CombatAttackOccurred` / `ProjectileHit` / `AreaEffectHit` / `ActorDefeated` を購読
- `IEventSubscriber` から受け取ったイベントに応じて overrides 辞書へ状態を記録し、`WorldActorPresenter` 側から `TryGetOverride()` で参照する分離設計
- 購読は全て `DisposableBag` へ `AddTo(ref bag)` し、`Dispose()` で `bag.Dispose()` を呼ぶ。リーク対策が適切
- `Dead` 状態は despawn まで維持、`Hit` は one-shot 後に復帰する設計意図が `SetOverrideUnlessDead()` で正しく表現されている

### Phase 3: Projectile View 表現 — PASS（軽微指摘あり）

- `WorldProjectileViewPool` / `WorldProjectilePresenter` / `ProjectilePrefabSource` の責務分離は適切
- `UpdatePositions()` は LINQ なし、毎フレーム全 Actor 走査なし。`worldState.Projectiles` 直走査でアクティブ Projectile のみ処理
- `collectRemovedProjectileAction` を Action フィールドとして保持し、ForEach ラムダのクロージャ allocation を避けている
- fallback prefab は `ProjectilePrefabSource.Initialize()` で生成されており、PlayMode が落ちない設計になっている

**軽微指摘 P3-1**: `WorldProjectileViewPool` は `Stack<ProjectileView>` を使用している。roadmap task_0004 の設計メモには「`WorldActorViewPool` の実装パターンに揃える（`Queue<ProjectileView>` ベース）」と明記されていた。`Stack`（LIFO）でも Pool として機能するが、表明した設計パターンとの不一致である。ただし正常動作に影響しないため、次マイルストーン以降での統一作業とする。`WorldAreaEffectViewPool` も同様。

### Phase 4: Area Effect View 表現 — PASS

- `WorldAreaEffectViewPool` / `WorldAreaEffectPresenter` / `AreaEffectPrefabSource` はProjectile と対称的な設計で統一されている
- `AreaEffectHit` の hit pulse は View へのフィードバックのみで、ダメージ計算は行っていない
- `maxDurations` 辞書は Presenter 内に保持し、View 表示の正規化進捗計算に使用。期限切れ時に `maxDurations.Remove()` で正しくクリーンアップされている
- `collectRemovedAreaEffectAction` も Action フィールド保持でクロージャ allocation なし

### Phase 5: Actor 頭上ステータス表示 — PASS

- `WorldUIModuleScene` / `WorldUILifetimeScope` が新規作成され、Screen Space Overlay Canvas を持つ専用 ModuleScene として分離されている
- `ActorStatusView` が `DungeonInn.View.Scene.ModuleScene.WorldUI` 名前空間に配置されている（World3D 関係のクラスとの混在なし）
- `WorldActorStatusPresenter.UpdatePositions()` はアクティブ Actor リストを `CopyActiveActorsTo(activeActors)` で事前確保済み List に取得し、foreach で処理。LINQ なし、毎フレーム allocation なし
- `GetActorStatusSummaryQuery` の Effect signature cache は `ActorEffectMasterId` + `InstanceId` + `MathF.Ceiling(remainingSeconds)` で構成され、Effect 構成変化と 1 秒単位の残り時間変化を検知できる。cache 更新ロジックは EditMode テストで検証済み（Codex 確認）
- `Camera.main` ではなく `WorldCameraController.WorldToScreenPoint()` 経由でスクリーン位置を変換している
- `ActorHUDViewPool` の `Rent` / `Return` / `TryReturnIfActive` / `ForEach` / `Dispose` の各メソッドは適切に実装されている
- `WorldHudCanvasProvider` が `IInitializable` / `IDisposable` を実装しており、VContainer が自動 Init / Dispose を管理できる

**構造課題メモ S5-1（M8 持ち越し）**: `WorldHudCanvasProvider.Initialize()` が `Object.FindFirstObjectByType<WorldUIModuleScene>()` を使用している。WorldUI シーンがロード済みであることに依存した順序前提であり、Codex レビュー-3 も同じ指摘をしている。M8 で World / WorldUI の共通親 LifetimeScope が導入された際に、DI 経由の参照渡しに切り替える。

### Phase 6: Actor 選択と詳細パネル — PASS

**Phase 6a: 入力・カメラ制御**

- `ActorSelectionService` は `ReactiveProperty<Guid?>` で選択状態を保持し、`Select` / `Deselect` / `SelectNext` / `SelectPrevious` を公開
- `SelectNext` / `SelectPrevious` は選択状態がない場合に早期 return している（roadmap 仕様「未選択時に矢印キーは作用しない」と一致）
- `WorldActorSelectionInputHandler` はマウスクリック・次選択・前選択・Back（ESC）の 4 アクションに対応
- クリック選択は `Camera.WorldToScreenPoint` で全 Actor を投影し、`SelectionScreenRadius = 50f` 以内で最近傍を選択。Unity Collider 判定なし（View 層で完結）
- `WorldActorCameraFollowController` は `ActorSelectionService.SelectedActorId` を購読し、`BeginFollow(ActorSelectionZoomRatio)` / `EndFollow()` でカメラ追従と縮小を制御。定数 `ActorSelectionZoomRatio = 0.20f` は roadmap 仕様と一致

**Phase 6b: 詳細パネル**

- `ActorDetailPopupPresenter` はアクターが選択されたとき `EnsurePopup()` で遅延 Instantiate し、非選択時に `SetActive(false)`
- Popup は `WorldAddressableViewFactory.CreateActorDetailPopup(parent)` で生成される。`ActorDetailPopup` が DI コンテナに直接公開されておらず、Presenter 以外から操作できない設計
- `UpdatePopupPosition` / `UpdatePopupContent` は毎フレーム `WorldGameLoopEntryPoint.Update()` から呼ばれ、選択 Actor がない場合は早期 return
- `ActorDetailDto` に `LayerPosition Position` が含まれており、Presenter が `IGameWorldStateReader` を直接参照せずに位置追従できる
- `GetActorDetailQuery` は Equipment / ActorEffect を集約して `ActorDetailDto` を返す narrow query であり、Presenter は DTO を表示するだけ

**指摘 P6-1**: `ActorDetailPopupPresenter` は `IInitializable` インターフェースを実装していないが `public void Initialize()` メソッドを持ち、`WorldGameLoopEntryPoint.InitializeAsync()` から手動で呼ばれる。Addressable ロード完了後に初期化が必要なため意図的な設計であり、`PlayerGameEventLogPresenter` も同一パターンを採用している。VContainer の自動 init 順より後で実行される必要があるため、`IInitializable` ではなくこのパターンが正しい。指摘ではなく設計の記録として残す。

### Phase 7: プレイヤー向けイベントログ UI — PASS

- `PlayerEventLogStore` は `IInitializable` / `IDisposable` を実装し、`Initialize()` で 11 種のゲームイベントを購読して `PlayerEventLogFormatter` で変換後 `Add()` する
- `IGameClock.ElapsedRealTimeSeconds` を `PlayerEventLogEntry.Timestamp` に使用。ゲーム時間ではなく実時間ベースのタイムスタンプが記録される
- `PlayerEventLogFormatter` は `IActorProfileRegistry` と `IMasterRepository` のみに依存し、`GameWorldState` を広く参照しない
- `PlayerGameEventLogPresenter` は `PlayerEventLogStore.OnEntryAdded` を購読（Observable<T>、R3 型）。毎フレーム全履歴 polling なし
- `PlayerEventLogView` は 10 行固定の `TextMeshProUGUI[]` を持つ。`ScrollRect` 不使用で、1 行単位 CLI スクロールと一時フェードを `Update()` 内で管理
- フェード定数 `LogDisplaySeconds = 3f` / `LogFadeSeconds = 1f` はコード内定数として管理されており、roadmap 仕様と一致
- `IPointerEnterHandler` / `IPointerExitHandler` / `IScrollHandler` を実装し、マウスホイールスクロールとマウスオーバーでのフェード停止が実装されている

**指摘 P7-1**: `PlayerEventLogStore.Add(PlayerEventLogEntry)` が `public` である。現在の呼び出し元は `AddFromEvent()`（private）のみ。テストや将来の外部注入を意図している可能性があるが、不要な public 表面積を生む。次マイルストーン以降で不要と判断した場合は `internal` または `private` に変更を検討する。現時点では動作に問題なし。

### Phase 8: M7 統合確認 — 前回 task_0010 で確認済み

Codex の `milestone7-completion-review-6-codex-3.md` 時点での確認:
- `uloop.cmd compile --project-path Client`: ErrorCount 0 / WarningCount 0
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 284/284 pass

---

## 設計境界チェック

### View → Application 境界

| チェック項目 | 結果 |
|---|---|
| Presenter が Domain State / GameWorldState を直接変更していない | ✓ 全 Presenter が読み取り専用操作のみ |
| View が HP 計算・命中判定・状態異常効果計算を行っていない | ✓ `WorldActorStatusPresenter` / `WorldAreaEffectPresenter` 等は表示のみ |
| 表示派生値が Application 層の Query / DTO 境界で用意されている | ✓ `GetActorStatusSummaryQuery` / `GetActorDetailQuery` / `PlayerEventLogFormatter` |
| イベント購読者が Domain State / GameWorldState を変更していない | ✓ `ActorCombatAnimationPresenter` / `WorldAreaEffectPresenter` 等は View 状態のみ変更 |

### Application → Domain 境界

| チェック項目 | 結果 |
|---|---|
| Application 層に UnityEngine 依存が追加されていない | ✓ `PlayerEventLogStore` / `PlayerEventLogFormatter` / `GetActorDetailQuery` / `GetActorStatusSummaryQuery` は全て pure C# |
| DTO / Entry が readonly struct / sealed class で適切に設計されている | ✓ `ActorDetailDto`（readonly struct）/ `PlayerEventLogEntry`（readonly struct）/ `ActorStatusViewData`（struct）|

### フレームループ品質

| チェック項目 | 結果 |
|---|---|
| LINQ（.Where / .Select / .ToList / .ToArray 等）をフレームループ内で使用していない | ✓ 全 UpdatePositions() は for ループと foreach のみ |
| 毎フレームのラムダクロージャ allocation がない | ✓ Action フィールドとして事前キャプチャしている（`collectRemovedProjectileAction` 等）|
| 毎フレーム全件 polling になっていない | ✓ active 件数に比例した処理。空なら早期 return |

---

## Lighthouse / コーディング規約チェック

### 比較演算子（coding-rules 11-1: `<` のみ使用、`>` 禁止）

新規・変更 script ファイルを全て確認した結果、`>` を直接使用している箇所はなかった。`<=` は使用されているが、これは `<` の派生として許容範囲と判断する。

確認済みパターン例:
- `0 < maxHp` / `0 < scrollOffset` / `elapsed < LogDisplaySeconds` / `screenPosition.z < 0f`
- `sortedActorIdBuffer.Count <= nextIndex` / `MaxEntryCount <= entries.Count`
- 数値比較に `>` / `>=` は使用なし

### Addressable / IAssetScope パターン（P5）

- `WorldAddressableViewFactory` が `IAssetManager.CreateScope()` で scope を作成し、`LoadAsync` を使用
- `Addressables.LoadAssetAsync` 直接呼び出しなし
- `Resources.Load` 呼び出しなし
- `VisualConfigLoader` も同パターンで継続して適切

### R3 Observable 型

- `ActorSelectionService.SelectedActorId` は `ReactiveProperty<Guid?>`（R3 型）
- `PlayerEventLogStore.OnEntryAdded` は `Observable<PlayerEventLogEntry>`（R3 型、`IObservable<T>` でない）
- `.Subscribe(...).AddTo(ref bag)` / `DisposableBag` パターンが全 Presenter で統一されている

### DI 登録

`WorldLifetimeScope` の新規登録確認:
- `WorldAddressableViewFactory`: `.AsImplementedInterfaces().AsSelf()` ✓ (`IDisposable` as interface)
- `WorldHudCanvasProvider`: `.AsImplementedInterfaces().AsSelf()` ✓ (`IInitializable` / `IDisposable`)
- `ActorHUDViewPool`: `.AsImplementedInterfaces().AsSelf()` ✓ (`IInitializable` / `IDisposable`)
- `WorldActorStatusPresenter`: `.AsSelf()` ✓（手動更新型）
- `ActorSelectionService`: `.AsSelf()` ✓
- `WorldActorSelectionInputHandler`: `.AsSelf()` ✓（手動呼び出し型）
- `WorldActorCameraFollowController`: `.AsImplementedInterfaces().AsSelf()` ✓ (`IInitializable` / `IDisposable`)
- `ActorDetailPopupPresenter`: `.AsImplementedInterfaces().AsSelf()` ✓ (`IDisposable`)
- `PlayerGameEventLogPresenter`: `.AsSelf()` ✓（VContainer Scoped は IDisposable を自動追跡）
- `PlayerEventLogStore`: `.AsImplementedInterfaces().AsSelf()` ✓ (`IInitializable` / `IDisposable`)
- `GetActorDetailQuery` / `GetActorStatusSummaryQuery` / `PlayerEventLogFormatter`: `.AsSelf()` もしくは `Lifetime.Scoped` ✓

---

## 指摘サマリ

| ID | 重要度 | 内容 | 判定 |
|---|---|---|---|
| P3-1 | 低 | `WorldProjectileViewPool` / `WorldAreaEffectViewPool` が `Stack<>` を使用。roadmap は `Queue<>` を指定していた | M8 以降対応可 |
| S5-1 | 中 | `WorldHudCanvasProvider.Initialize()` が `FindFirstObjectByType` に依存。親 scope 未整備の暫定設計 | M8 持ち越し（既知） |
| P6-1 | 低 | `ActorDetailPopupPresenter` / `PlayerGameEventLogPresenter` が `IInitializable` でなく手動 `Initialize()`。意図的設計、記録のみ | 問題なし |
| P7-1 | 低 | `PlayerEventLogStore.Add()` が public。現在の呼び出し元は内部のみ | 次マイルストーン以降で要検討 |

新たな次マイルストーン移行を止める指摘なし。

---

## 総合判定

**Milestone 7 実装差分は M8 への移行条件を満たしている。**

設計境界（View/Application/Domain）は守られており、Lighthouse ガイドライン違反なし。コーディング規約（比較演算子、R3 Observable 型、Addressable パターン）への準拠を確認した。Codex セルフレビュー（review-6-codex-1/2/3）で指摘された懸念は対応済みまたは既知の暫定設計として記録済みである。

---

## M8 持ち越し課題

| 課題 | 理由 |
|---|---|
| `WorldHudCanvasProvider.FindFirstObjectByType` を DI 経由に置き換え | World / WorldUI の共通親 LifetimeScope が M8 で導入予定 |
| `WorldProjectileViewPool` / `WorldAreaEffectViewPool` の Queue 統一 | 動作影響なし。M8 の View pool 整理時に対応 |
| `PlayerEventLogStore.Add()` の公開範囲 | M8 の Store 設計確定時に必要に応じて絞り込み |
| `PlayerEventLogView` の `IScrollHandler` 動作確認（EventSystem 存在確認）| WorldUI.unity に EventSystem があることの最終確認を PlayMode で行う |
