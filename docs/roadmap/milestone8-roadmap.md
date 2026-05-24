# Milestone 8 Roadmap — UI 基盤と HUD

## ゴール

プレイヤーがゲームの状態を一目で把握できる UI 基盤を構築する。
「今何時か」「資金はいくらか」「シミュレーション速度はどうか」を常に確認でき、
宿に何人いるかを把握できる状態にする。

## 対象範囲

### HUD（常時表示）

- ゲーム内時間の表示（時刻・日付）
- ギルド資金の表示
- シミュレーション速度コントロール（一時停止・等速・倍速）
- 簡易アラート表示（重要イベント通知のポップ）

### Inn ステータスパネル

- 現在宿屋に宿泊中の冒険者リスト（名前・回復状態・残滞在時間）
- 当日の収入・支出サマリ

### ミニマップ / 俯瞰表示

- Ground / Dungeon の 2D 俯瞰表示（簡易マップ）
- 現在表示中の layer と Actor の配置を概略で確認できる
- 詳細なマップ UI は Milestone 9 の施設管理と合わせて整理する

### UI アーキテクチャ基盤

- World シーンの UI Canvas 配置方針の確定
- HUD 専用 LifetimeScope の整理
- Application 層のクエリ UseCase（`GetGameTimeStateUseCase` など）を UI が利用する経路の確立
- View が `IGameWorldStateReader` を直接読んで集計しない方針の徹底

### Prefab / Scene アセット生成運用の整理

Prefab / Scene / ScriptableObject などの Unity アセットは、手作業で調整されたアセットファイルを原本とする。
AI が Prefab や Scene を構築・配線する場合は、永続セットアップコードに生成処理を残さず、一時的な Editor automation で作成する運用にする。

- Prefab を作る時は、生成用の一時 Editor コードまたは `uloop.cmd execute-dynamic-code` で `GameObject` / `Component` 追加と `SerializedObject` による参照配線を行う
- 生成結果に問題がなければ、生成用コードを削除して完了する
- `VisualAssetSetup` や validation 経路から、既存 Prefab / Scene の子要素を全削除して再生成する処理は呼ばない
- 既存アセットがある場合は、アセット側を原本として扱い、必要な場合でも足りない参照の補完に留める
- この作業を効率化する基盤を M8 で検討する
- 基盤の方針、形式、置き場所、実行方法は、作業開始前にユーザーと相談して決める

### World シーングループ LifetimeScope 分離

Milestone 7 で World の UI Canvas を `WorldUI` ModuleScene に分離した。
ただし、World のゲームロジックが `WorldLifetimeScope` に残ったままだと、`WorldLifetimeScope` の子ではない `WorldUILifetimeScope` が同じゲーム状態や UseCase を自然に受け取れない。
Milestone 8 では、World の 3D 表現と Canvas UI を兄弟 Scene として扱い、その共通親にゲームロジック用 LifetimeScope を置く構成へ整理する。

**スコープ名について**: 将来 Guildhouse 等の別ゲームシーンが追加された場合にも同じ親スコープを共有できるよう、`WorldGameLifetimeScope` ではなく `MainGameLifetimeScope` と命名する。

想定構造:

```text
RootLifetimeScope
  ProductLifetimeScope
    MainGameLifetimeScope   ← ゲームセッション全体の共有依存
      WorldLifetimeScope    ← World 3D 表現固有
      WorldUILifetimeScope  ← World Canvas/HUD/Popup 固有
      （将来）GuildhouseLifetimeScope
      （将来）GuildhouseUILifetimeScope
```

- `MainGameLifetimeScope`
  - ゲームセッション中に有効な共通 LifetimeScope として追加する
  - ゲーム状態、ゲームループ、Application UseCase、EventBus、ゲーム全体で共有する Service を登録する
  - 設定 ScriptableObject は Addressables から非同期ロードして値型に変換し、`ProductLifetimeScope.CreateChild<MainGameLifetimeScope>` のインストーラーで `RegisterInstance` する
  - 3D View / Canvas View / UI View Prefab の実体は保持しない
- `WorldLifetimeScope`
  - World MainScene 固有の 3D 表現に責務を限定する
  - Actor View、Map View、Projectile View、AreaEffect View、Environment Prop など 3D View 系 Presenter / Factory / Pool を登録する
  - Canvas / HUD / Popup / UI View の登録や直接参照を持たない
- `WorldUILifetimeScope`
  - `WorldUI` ModuleScene 固有の Canvas / HUD / Popup / UI Presenter / UI Factory を登録する
  - ゲーム状態は親の `MainGameLifetimeScope` から、Application Query / 表示専用 DTO / Event 購読経由で受け取る
  - `IGameWorldStateReader` のような広い Reader を UI Presenter が直接読んで集計しない

実装方針:

1. `ProductEntryPoint.StartAsync` で行っている `mainSceneManager.SetEnqueueParentLifetimeScope` / `moduleSceneManager.SetEnqueueParentLifetimeScope` は、デフォルト親を `ProductLifetimeScope` にする初期設定として維持する。
2. `ProductSceneManager` から利用する `SceneGroupLifetimeScopeManager` を追加し、遷移先 `MainSceneId` に応じて SceneGroup 用親 LifetimeScope を生成・保持・破棄する。
3. ゲームシーン（World 等）へ遷移する前に、設定 SO を Addressables から非同期ロードし、値型に変換したうえで `productLifetimeScope.CreateChild<MainGameLifetimeScope>(installer)` で `MainGameLifetimeScope` を生成する。MainScene / ModuleScene の enqueue parent を `MainGameLifetimeScope` に差し替える。
4. Lighthouse の `LoadSceneGroupStep` は MainScene → ModuleScene の順で `Load` するため、World MainScene と `WorldUI` ModuleScene の両方が同じ `MainGameLifetimeScope` を親にできる。
5. 非ゲームシーン（Title 等）へ遷移する場合は enqueue parent を `ProductLifetimeScope` に戻し、World シーングループの unload 完了後に `MainGameLifetimeScope` を破棄する。
6. Reboot / BackScene / 例外復旧時に `MainGameLifetimeScope` が残留しないよう、`PreReboot` と遷移完了時の cleanup 経路を定義する。

完了条件:

- [ ] `MainGameLifetimeScope` が追加され、World のゲームロジック共有依存がそこに移動している
- [ ] `WorldLifetimeScope` は 3D 表現固有の登録に限定され、Canvas / HUD / Popup の責務を持っていない
- [ ] `WorldUILifetimeScope` は `MainGameLifetimeScope` の子として生成され、HUD / Popup が親 scope の Query / Event / DTO 経由で World 状態を表示できる
- [ ] `ProductSceneManager` またはその協調クラスが、ゲームシーン遷移時だけ SceneGroup 用親 LifetimeScope を enqueue parent に設定している
- [ ] World から別 MainScene へ離脱した後、`MainGameLifetimeScope` とその scoped disposable が破棄される
- [ ] `WorldLifetimeScope` / `WorldUILifetimeScope` にゲームコンテンツ Prefab / UI View Prefab / Popup View 実体の `SerializedField` がない
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している
- [ ] Play モードで World へ遷移し、`[World] GameWorldState initialized` が出力され、エラーログが存在しない

## キャリーオーバー項目（前マイルストーンレビューからの持ち越し）

### T-2 — Actor 選択ハンドラの `IGameWorldStateReader` 直接依存除去

由来: milestone7-after-review-7-total-1.md T-2

- `WorldActorSelectionInputHandler` が `IGameWorldStateReader.Actors` を全走査している
- `WorldActorSelectionInputHandler.HandleClick` が `layerViewRegistry.GetOrCreateActorRoot(...)` を呼び出し View root 生成副作用を持つ
- `ActorSelectionService` が `IGameWorldStateReader` を直接注入している

対応方針: Actor 選択用 narrow provider を用意し、Input ハンドラは `ActorId` と screen/world position snapshot のみを受け取る形に変更する。`GetOrCreateActorRoot` は hit test 中には呼ばない。

完了条件:
- [ ] `WorldActorSelectionInputHandler.cs` から `IGameWorldStateReader` 依存が消えている
- [ ] `WorldActorSelectionInputHandler.HandleClick` が `worldState.Actors` を直接走査していない
- [ ] `WorldActorSelectionInputHandler` から `GetOrCreateActorRoot(...)` 呼び出しが消えている
- [ ] `ActorSelectionService` が `IGameWorldStateReader` を直接注入していない
- [ ] `uloop.cmd compile --project-path Client` が成功している

### T-4 残 — `GetActorDetailQuery` の `ToArray()` フレームごと割り当て

由来: milestone7-after-review-7-total-1.md T-4（クエリ呼び出し回数は統合済み、割り当て問題が残存）

- `GetActorDetailQuery.Query()` が毎回 `equipmentNameBuffer.ToArray()` / `effectBuffer.ToArray()` を呼び出してヒープ割り当てを発生させている
- `GetActorStatusSummaryQuery` に signature ベースキャッシュがあるのに対し非対称

対応方針: `GetActorDetailQuery` に signature キャッシュを追加する、または戻り値型を `IReadOnlyList` に変更してコピーを避ける。

完了条件:
- [ ] `GetActorDetailQuery.Query()` がフレームごとの `ToArray()` 割り当てを発生させない
- [ ] `uloop.cmd compile --project-path Client` が成功している

### T-5 残 — `WorldAddressableViewFactory.PlayerEventLogViewPrefab` public プロパティ整理

由来: milestone7-after-review-7-total-1.md T-5（Factory メソッド追加は済み、プロパティ公開が残存）

- `PlayerEventLogViewPrefab` が public プロパティとして残っており、Factory を通さない Instantiate の再発経路になりうる
- 他の Consumer が存在しないことを確認してから internal / private に下げる

完了条件:
- [ ] `WorldAddressableViewFactory.PlayerEventLogViewPrefab` の Consumer が Factory メソッド経由のみであることを確認した
- [ ] public プロパティが不要であれば削除または internal 化されている

### S5-1 — `WorldHudCanvasProvider` の `FindFirstObjectByType` 除去

由来: milestone7-completion-review-6 S5-1

- `WorldHudCanvasProvider.Initialize()` が `FindFirstObjectByType<WorldUIModuleScene>()` を使用している
- Lighthouse ルールでは `FindFirstObjectByType` は禁止パターン

対応方針: `WorldUI` ModuleScene のシーン分離が完了したら、依存注入または Scene 参照経由で `WorldUIModuleScene` を取得する。

完了条件:
- [ ] `WorldHudCanvasProvider` から `FindFirstObjectByType` が消えている
- [ ] `uloop.cmd compile --project-path Client` が成功している

### P3-1 — `WorldProjectileViewPool` / `WorldAreaEffectViewPool` の Stack → Queue

由来: milestone7-completion-review-6 P3-1

- 両 Pool が `Stack<T>` を使用しているが、ロードマップ設計指定は `Queue<T>`

完了条件:
- [ ] `WorldProjectileViewPool` / `WorldAreaEffectViewPool` が `Queue<T>` を使用している
- [ ] `uloop.cmd compile --project-path Client` が成功している

### P7-1 — `PlayerEventLogStore.Add()` の公開範囲

由来: milestone7-completion-review-6 P7-1

- `PlayerEventLogStore.Add()` が public 公開されており、Store を直接書き込める経路が広すぎる

対応方針: 書き込み経路を UseCase / EventBus 経由に限定し、`Add()` を internal 化するか Store 自体をイベント購読型に変更する。

完了条件:
- [ ] `PlayerEventLogStore.Add()` が外部から直接呼べない設計になっている
- [ ] `uloop.cmd compile --project-path Client` が成功している

### VAS-1 — `VisualAssetSetup` の自動再生成・ロールバック経路整理

由来: milestone7-roadmap.md Phase 0 / task_0001、および ActorDetailPopup Prefab 配線時の運用確認

- `VisualAssetSetup` は visual asset の初期作成・Addressables 登録を担ってきたが、Prefab / Scene を人間が手調整する運用では、自動修復や再生成が手作業をロールバックするリスクになる
- `ActorDetailPopup` のような UI Prefab は、生成後の `.prefab` を原本とし、永続 setup code で再構築し続けない
- validation は副作用なしで不足・未配線を検出するだけにし、修正は明示 setup または一時生成コードで行う

対応方針:

1. `VisualAssetSetup` を副作用なし validation と、ユーザーが明示実行する setup に分ける。
2. Prefab / Scene の再生成処理を `ValidateSetup()` や domain reload から呼ばない。
3. 既存アセットを全削除・再作成する処理が必要な場合は、作業用の一時コードとして実行し、完了後に削除する。
4. 一時生成作業を効率化する基盤を設計する場合は、実装前にユーザーと方針・形式を確認する。

完了条件:

- [ ] `VisualAssetSetup` の validation 経路が既存 Prefab / Scene / ScriptableObject を変更しない
- [ ] `VisualAssetSetup` から手調整済み Prefab / Scene を自動再生成する経路が消えている
- [ ] Prefab / Scene 生成用の一時コードは、生成完了後に削除する運用が docs または guideline に明記されている
- [ ] 一時生成基盤を実装する場合、その方針・形式・置き場所・実行方法を事前にユーザー確認している
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## タスク分解と作業順序

### Task 1 — 小修正まとめ（独立・依存なし）

対象: P3-1 / T-4残 / T-5残 / P7-1

| ID | ファイル | 修正内容 |
|---|---|---|
| P3-1 | `WorldProjectileViewPool.cs` / `WorldAreaEffectViewPool.cs` | `Stack<T>` → `Queue<T>` |
| T-4残 | `GetActorDetailQuery.cs` | `ToArray()` フレームごと割り当て除去（`IReadOnlyList` 返却または signature キャッシュ） |
| T-5残 | `WorldAddressableViewFactory.cs` | `PlayerEventLogViewPrefab` public プロパティを internal または削除 |
| P7-1 | `PlayerEventLogStore.cs` | `Add()` の公開範囲を絞る（internal 化またはイベント購読型へ） |

### Task 2 — WorldGameLifetimeScope 分離（アーキテクチャ基盤）

後続タスク（T-2, S5-1, HUD UI 実装）すべての前提となる。

- `WorldGameLifetimeScope` を新規追加し、`WorldLifetimeScope` からゲームロジック共有依存を移動する
- `WorldLifetimeScope` は 3D 表現固有の登録に限定する
- `WorldUILifetimeScope` を `WorldGameLifetimeScope` の子として構成する
- `ProductSceneManager` またはその協調クラスが World 遷移時のみ enqueue parent を差し替える
- 完了条件は本ドキュメント「World シーングループ LifetimeScope 分離」セクションの完了条件に準拠する

### Task 3 — T-2 + S5-1（Task 2 完了後に着手）

**T-2: Actor 選択の narrow provider 化**

- `WorldActorSelectionInputHandler` / `ActorSelectionService` の `IGameWorldStateReader` 直接依存を除去する
- `HandleClick` 内の `layerViewRegistry.GetOrCreateActorRoot(...)` 副作用を除去する
- Actor 選択用 narrow provider（Actor スクリーン位置スナップショットを提供）を用意する

**S5-1: WorldHudCanvasProvider の FindFirstObjectByType 除去**

- Task 2 で LifetimeScope 分離が完了し、`WorldUIModuleScene` が DI 解決可能になった後に対処する
- `FindFirstObjectByType<WorldUIModuleScene>()` を DI 注入または Scene 参照に置き換える

### Task 4 — VAS-1（ユーザー確認後に着手）

着手前に以下をユーザーと確認する:

- 一時 Editor コードの置き場所（`Editor/` 以下の専用フォルダ等）
- 実行方法（メニュー項目 / `uloop execute-dynamic-code` 等）
- 完了後の削除タイミングと基準

確認完了後、`VisualAssetSetup` の validation 経路から自動再生成・ロールバック処理を除去し、一時生成基盤方針を docs に明記する。

### Task 5〜7 — UI 実装（Task 2〜4 完了後に着手）

- Task 5: HUD UI（時刻・日付 / ギルド資金 / 速度コントロール / アラート）
- Task 6: Inn ステータスパネル（冒険者リスト / 収支サマリ）
- Task 7: ミニマップ / 俯瞰表示（簡易）

---

## Milestone 9 へ移動する項目

- 施設アップグレード画面
- スタッフ管理 / 雇用画面
- 経済レポート詳細
