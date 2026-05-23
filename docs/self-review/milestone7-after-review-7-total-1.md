# Milestone 7 After Review — 統合レビュー

作成日: 2026-05-23  
対象コミット: 59a592c milestone7  
統合元: milestone7-after-review-1-codex.md / milestone7-completion-review-7-claude-1.md  
担当: Claude Code（統合）

---

## 重複排除の記録

| Claude 側 | Codex 側 | 統合判定 |
|---|---|---|
| AB-2: View 層が `IGameWorldStateReader` を直接列挙 | Item 2: Actor 選択入力が broad world state を直接走査し View root 生成副作用まで持つ | **T-2 に統合**。Codex 版が詳細（`GetOrCreateActorRoot` 副作用まで含む）なため Codex をベースに Claude の `ActorSelectionService` 言及を補足 |
| CR-1 / AB-1 / LH-1 / IQ-1 | なし | Claude 側のみに存在するため T-3〜T-6 として収録 |
| M8 キャリーオーバー（S5-1 / P3-1 / P7-1） | なし | 末尾にそのまま収録 |
| なし | Item 1: EntryPoint が `TryResolve` で Presenter を手動解決 | T-1 として収録 |

---

## 統合指摘一覧

| ID | 問題 | 重大度 | 由来 |
|---|---|---|---|
| T-1 | `WorldGameLoopEntryPoint` が `IObjectResolver.TryResolve` で Presenter を手動解決 | 高 | Codex |
| T-2 | Actor 選択ハンドラが broad world state を直接走査し、View root 生成副作用も持つ | 高 | Claude AB-2 + Codex Item 2 統合 |
| T-3 | `WorldActorSelectionInputHandler` コンストラクタに `[Inject]` がない | MUST | Claude CR-1 |
| T-4 | `ActorDetailPopupPresenter` がフレームごとに `GetActorDetailQuery` を 2 回呼び出し `ToArray()` 割り当てが発生 | MUST | Claude AB-1 |
| T-5 | `PlayerGameEventLogPresenter` が `Object.Instantiate` を直接呼び出している | SHOULD | Claude LH-1 |
| T-6 | `ActorDetailPopup.PopupOffsetX = 120f` が根拠コメントなし | MAY | Claude IQ-1 |

---

## 各指摘詳細

### T-1. EntryPoint が `IObjectResolver.TryResolve` で Presenter を手動解決している

重大度: 高

問題:

`WorldGameLoopEntryPoint.Construct(...)` が `IObjectResolver` を受け取り、`ActorDetailPopupPresenter` と `PlayerGameEventLogPresenter` を `TryResolve` で任意依存として取得している。両 Presenter は `WorldLifetimeScope` で登録済みなので本来は constructor / `Construct` の明示依存として扱うべき production 依存である。`TryResolve` にすると DI 登録漏れや初期化漏れが nullable optional 経路に吸収され、HUD / ログ UI が表示されない状態でも compile や起動で検出しにくい。

原因:

M7 の HUD / popup 初期化を `WorldGameLoopEntryPoint` に後付けした際、Presenter の存在を任意扱いにして EntryPoint から service locator 的に取得している。これは application-boundary-guidelines / implementation-quality-guidelines の「DI で解決すべき依存を手動解決しない」「注入依存と登録型を一致させる」方針と食い違う。

解決案:

`ActorDetailPopupPresenter` と `PlayerGameEventLogPresenter` を `WorldGameLoopEntryPoint.Construct(...)` の明示引数にする。任意依存にする必要があるなら、機能フラグ用の明示的な Null Object / no-op Presenter を設計し、なぜ optional なのかを task / design に記録する。初期化が Presenter 側ライフサイクルで完結できるなら `IInitializable` に寄せ、EntryPoint は必要な表示更新だけを呼ぶ。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [x] `WorldGameLoopEntryPoint.Construct(...)` から `IObjectResolver` 引数が消えている
- [x] `WorldGameLoopEntryPoint` 内に `TryResolve<ActorDetailPopupPresenter>` / `TryResolve<PlayerGameEventLogPresenter>` が存在しない
- [x] HUD / popup / player log Presenter の依存が constructor / `Construct` 引数、または明示的な lifecycle interface で解決されている
- [x] DI 登録漏れ時に nullable optional 経路で黙殺されず、compile / container resolve / PlayMode 確認で検出できる
- [x] `uloop.cmd compile --project-path Client` が成功している

---

### T-2. Actor 選択ハンドラが broad world state を直接走査し、View root 生成副作用も持つ

重大度: 高

問題:

`WorldActorSelectionInputHandler`（Input 層）は入力ハンドラでありながら `IGameWorldStateReader` を直接受け取り、クリック時に `worldState.Actors` を全走査している。さらに hit test 中に `layerViewRegistry.GetOrCreateActorRoot(...)` を呼ぶため、入力処理が View root の生成副作用も持つ。  
加えて `ActorSelectionService`（View 層）も `worldState.Actors` を直接列挙して `sortedActorIdBuffer` を再構築しており、同じ境界問題が2クラスに及んでいる。  
M7 では `ActorDetailPopupPresenter` / `WorldActorStatusPresenter` から broad state 依存を外したが、Actor 選択経路に同様の違反が残存している。

原因:

Actor 選択に必要なのは「選択対象 Actor の screen position 一覧」だが、その read model / provider が定義されていないため、Input 層が Application の broad state と View registry の両方を知る形になっている。入力イベント処理、表示座標解決、View root 作成、選択状態更新の責務が 1 クラスに集まっている。

解決案:

Actor 選択用の narrow provider を用意し、Input ハンドラは provider から選択候補の `ActorId` と screen position または world position snapshot を受け取るだけにする。View root は既存 layer が存在する場合のみ参照する API に寄せ、hit test 中に `GetOrCreateActorRoot` を呼ばない。選択候補の更新は `ActorViewDataStore` / Presenter 側で管理し、入力ハンドラは action と選択 command の橋渡しに限定する。  
`ActorSelectionService` の `worldState.Actors` 直接参照も同 provider を通じて解決する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Input/Layer/WorldActorSelectionInputHandler.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSelectionService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapLayerViewRegistry.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件（M8 タスク化）:

- [ ] `WorldActorSelectionInputHandler` の constructor から `IGameWorldStateReader` が消えている
- [ ] `WorldActorSelectionInputHandler.HandleClick(...)` が `worldState.Actors` を直接走査していない
- [ ] `WorldActorSelectionInputHandler` から `GetOrCreateActorRoot(...)` 呼び出しが消えている
- [ ] `ActorSelectionService` が `IGameWorldStateReader` を直接注入していない
- [ ] Actor 選択用 provider / read model が `ActorId` と hit test に必要な座標だけを公開している
- [ ] クリック選択、再クリック解除、非選択クリック解除、Next / Previous の EditMode test が存在する
- [ ] `uloop.cmd compile --project-path Client` が成功している

設計記録: `WorldActorSelectionInputHandler.cs` コメントで M8 対応予定を明文化済み (2026-05-23)

---

### T-3. `WorldActorSelectionInputHandler` コンストラクタに `[Inject]` がない

重大度: MUST

問題:

`WorldActorSelectionInputHandler` のコンストラクタに `[Inject]` 属性が付与されていない。coding-rules §15-4 は「DI コンテナから注入を受ける全コンストラクタには `[Inject]` を付与すること」と定めている。VContainer はコンストラクタが 1 つの場合は属性なしでも動作するが、規約統一と可読性のために全 DI コンストラクタへの付与が求められている。

原因:

実装時に `[Inject]` 属性の付与が漏れた。

解決案:

```csharp
[Inject]
public WorldActorSelectionInputHandler(
    InputActions inputActions,
    ...
```

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Input/Layer/WorldActorSelectionInputHandler.cs` (L25)
- `docs/guidelines/coding-rules.md` (§15-4: [Inject] 属性)

完了条件:

- [x] `WorldActorSelectionInputHandler` のコンストラクタ宣言直前に `[Inject]` 属性が付与されている

---

### T-4. `ActorDetailPopupPresenter` がフレームごとに `GetActorDetailQuery` を 2 回呼び出し `ToArray()` 割り当てが発生

重大度: MUST

問題:

`WorldGameLoopEntryPoint.Update()` は毎フレーム `actorDetailPopupPresenter.UpdatePopupPosition()` と `actorDetailPopupPresenter.UpdatePopupContent()` を順番に呼び出す。両メソッドはそれぞれ独立して `actorDetailQuery.Query(selectedId.Value)` を呼び出す。`GetActorDetailQuery.Query()` は毎回 `equipmentNameBuffer.ToArray()` と `effectBuffer.ToArray()` を呼び出してヒープ割り当てを行うため、Actor 選択中は毎フレーム 2 回のクエリ実行 × 2 回の配列割り当てが発生する。`GetActorStatusSummaryQuery` が signature ベースのキャッシュを持つのに対し `GetActorDetailQuery` が同等のキャッシュを持たない設計の非対称性もある。application-boundary-guidelines はフレームループ内の `ToArray()` / `ToList()` を禁止している。

原因:

`UpdatePopupPosition` と `UpdatePopupContent` がそれぞれ独立して Query を呼び出す構造になっており、同一フレーム内で結果を共有する仕組みが設計されていない。また `GetActorDetailQuery` にキャッシュが実装されていない。

解決案:

案A（Presenter で結果を 1 回取得して共有）: `ActorDetailPopupPresenter` に `lastDto` フィールドを設け、フレームの先頭でクエリを 1 回実行して両メソッドで共有する。`UpdatePopup()` 1 本に統合する方法も有効。  
案B（Query にキャッシュを追加）: `GetActorDetailQuery` に `GetActorStatusSummaryQuery` と同様の signature-based キャッシュを実装する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorDetailPopupPresenter.cs` (L67, L94)
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetActorDetailQuery.cs` (L79-98)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/GetActorStatusSummaryQuery.cs`（キャッシュ実装の参考）
- `docs/guidelines/application-boundary-guidelines.md`（フレームループ禁止事項: ToArray/ToList）

完了条件:

- [x] Actor 選択中の 1 フレームあたり `GetActorDetailQuery.Query()` 呼び出し回数が 1 回以下になっている（`UpdatePopup()` に統合）
- [ ] `GetActorDetailQuery` の `ToArray()` 呼び出しがフレームごとの割り当てを発生させない（キャッシュ or 戻り値型変更） — M8 対応
- [x] `uloop.cmd compile --project-path Client` が成功している

---

### T-5. `PlayerGameEventLogPresenter` が `Object.Instantiate` を直接呼び出している

重大度: SHOULD

問題:

`PlayerGameEventLogPresenter.EnsureLogView()` が `UnityEngine.Object.Instantiate(prefab, hudCanvasProvider.HUDCanvas.transform)` を直接呼び出している。lighthouse-patterns.md のコンテンツ Prefab 生成パターンでは Prefab のインスタンス化は Factory を通じて行うことが原則であり、Presenter が直接 Instantiate を呼ぶことは Factory パターンの迂回になる。また `WorldAddressableViewFactory.PlayerEventLogViewPrefab` が public プロパティとして公開される必要も生じている。

原因:

`WorldAddressableViewFactory` に `PlayerEventLogView` 用の生成メソッドが存在せず、Presenter が直接 Prefab プロパティにアクセスして Instantiate している。

解決案:

`WorldAddressableViewFactory` に `CreatePlayerEventLogView(Transform parent)` メソッドを追加し、Presenter からは Factory メソッド経由で生成する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/PlayerGameEventLogPresenter.cs` (L64-78)
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldAddressableViewFactory.cs`
- `docs/guidelines/lighthouse-patterns.md`（コンテンツ Prefab は Addressable Factory / Pool 経由で生成する）

完了条件:

- [x] `PlayerGameEventLogPresenter` から `Object.Instantiate` の直接呼び出しが消えている
- [x] `WorldAddressableViewFactory.CreatePlayerEventLogView(Transform)` が追加されている
- [ ] `WorldAddressableViewFactory.PlayerEventLogViewPrefab` の public プロパティが不要になっている — `ActorDetailPopupPresenter` 等の他 Consumer 確認が必要、M8 で整理

---

### T-6. `ActorDetailPopup.PopupOffsetX` が根拠コメントなしのマジックナンバー

重大度: MAY

問題:

`const float PopupOffsetX = 120f` が定義されているが、なぜ 120 ピクセルなのかの根拠がコード上にない。implementation-quality-guidelines はマジックナンバーをコード中に置くことを禁じており、定数化されていても意図が伝わらない。

原因:

UI 配置の調整値をハードコードした際にコメントが付与されなかった。

解決案:

定数に意図を示すコメントを追加する。または Inspector から調整できるよう `[SerializeField] float popupOffsetX = 120f;` として設定可能にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorDetailPopup.cs` (L9)
- `docs/guidelines/implementation-quality-guidelines.md`（マジックナンバー禁止）

完了条件:

- [x] `PopupOffsetX` の値の根拠を示すコメントが付与されるか、Inspector 調整可能な `SerializedField` に変更されている

---

## M8 キャリーオーバー（既存記録の継続）

以下は milestone7-completion-review-6-codex-3 / 6-claude-1 で記録済みの M8 対応項目。現行コードで未対応のまま維持。

| ID | 問題 | 現行状態 |
|---|---|---|
| S5-1 | `WorldHudCanvasProvider.Initialize()` が `FindFirstObjectByType<WorldUIModuleScene>()` を使用 | 未対応 |
| P3-1 | `WorldProjectileViewPool` / `WorldAreaEffectViewPool` が Stack を使用（ロードマップ指定は Queue） | 未対応 |
| P7-1 | `PlayerEventLogStore.Add()` が public 公開 | 未対応 |

---

## 統合判定

**判定: M8 移行前に T-1〜T-4 の対応が必要**

| ID | 重大度 | 推奨対応タイミング |
|---|---|---|
| T-1 | 高 | M8 移行前に修正（service locator パターンは DI 崩壊リスクあり） |
| T-2 | 高 | M8 移行前に修正、または設計記録を明文化してから M8 タスク化 |
| T-3 | MUST | M8 移行前に修正（1 行の追記で完結） |
| T-4 | MUST | M8 移行前に修正（フレームループ禁止事項の直接違反） |
| T-5 | SHOULD | M8 移行前修正が望ましい（影響範囲が Factory 追加のみ） |
| T-6 | MAY | コメント追記のみ。M8 移行前に対応しておくと望ましい |

M8 移行最低ライン:

- [x] T-1: `WorldGameLoopEntryPoint` から `IObjectResolver.TryResolve` を除去 — 対応済み (2026-05-23)
- [x] T-2: `WorldActorSelectionInputHandler` / `ActorSelectionService` の `IGameWorldStateReader` 直接依存を除去または設計記録を明文化 — 設計記録を `WorldActorSelectionInputHandler.cs` コメントで明文化し M8 タスク化 (2026-05-23)
- [x] T-3: `WorldActorSelectionInputHandler` コンストラクタに `[Inject]` を付与 — 対応済み (2026-05-23)
- [x] T-4: `ActorDetailPopupPresenter` のフレームごとクエリを 1 回に統合し `ToArray()` 割り当てを排除 — `UpdatePopup()` に統合、1 回呼び出しに変更 (2026-05-23)
- [x] T-5: `WorldAddressableViewFactory.CreatePlayerEventLogView` を追加し Presenter の直接 Instantiate を除去 — 対応済み (2026-05-23)
- [x] T-6: `PopupOffsetX` にコメント追記または `SerializedField` 化 — `[SerializeField]` 化 + WHY コメント追加 (2026-05-23)
