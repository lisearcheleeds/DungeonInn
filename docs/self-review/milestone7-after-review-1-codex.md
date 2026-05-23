# Milestone 7 After Review 1 Codex

作成日: 2026-05-23
対象: `59a592c milestone7` (`HEAD`)
レビュー担当: Codex

## 事前確認

以下を確認した。

- `docs/guidelines/self-review-guidelines.md`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/roadmap/milestone7-roadmap.md`
- `docs/design/ai-class-relation-index.md`
- `docs/self-review/milestone7-completion-review-6-codex-1.md`
- `docs/self-review/milestone7-completion-review-6-codex-2.md`
- `docs/self-review/milestone7-completion-review-6-codex-3.md`
- `Client/Assets/DungeonInn/Runtime/Scripts` の対象差分

`self-review-guidelines.md` の専任レビュー運用については、外部 sub-agent はユーザーから明示依頼がないため使わず、Lighthouse / Coding Rules / Domain Design / Application Boundary / Implementation Quality の各軸で一次確認し、最後に統合レビューとして重複排除した。

## 差分分類

production 契約変更として確認した差分:

- `WorldGameLoopEntryPoint.Construct(...)` に `IObjectResolver` が追加されている。
- `WorldActorSelectionInputHandler` が新規追加され、constructor で `IGameWorldStateReader` / View 側 service を受け取っている。
- `ActorStatusViewData` / `ActorDetailViewData` / `PlayerEventLogEntry` / `PlayerEventLogStore` / `WorldAddressableViewFactory` など、新規 DTO / Store / Factory / Presenter が追加されている。

既存 self-review で `ActorDetailPopupPresenter` / `WorldActorStatusPresenter` の broad state 依存は対応済みとされ、現行コードでも直接 `IGameWorldStateReader` を受け取っていないことを確認したため、同じ指摘は再掲しない。

## レビュー結果

### 1. EntryPoint が `IObjectResolver.TryResolve` で Presenter を手動解決している

重大度: 高

問題:

`WorldGameLoopEntryPoint.Construct(...)` が `IObjectResolver` を受け取り、`ActorDetailPopupPresenter` と `PlayerGameEventLogPresenter` を `TryResolve` で任意依存として取得している。両 Presenter は `WorldLifetimeScope` で登録済みなので、本来は constructor / `Construct` の明示依存として扱うべき production 依存である。`TryResolve` にすると DI 登録漏れや初期化漏れが nullable optional 経路に吸収され、HUD / ログ UI が表示されない状態でも compile や起動で検出しにくい。

原因:

M7 の HUD / popup 初期化を `WorldGameLoopEntryPoint` に後付けした際、Presenter の存在を任意扱いにして EntryPoint から service locator 的に取得している。これは Application Boundary / Implementation Quality の「DI で解決すべき依存を手動解決しない」「注入依存と登録型を一致させる」方針と食い違う。

解決案:

`ActorDetailPopupPresenter` と `PlayerGameEventLogPresenter` を `WorldGameLoopEntryPoint.Construct(...)` の明示引数にする。任意依存にする必要があるなら、登録側を `#if` で分けるか、機能フラグ用の明示的な Null Object / no-op Presenter を設計し、なぜ optional なのかを task / design に記録する。初期化が Presenter 側ライフサイクルで完結できるなら `IInitializable` に寄せ、EntryPoint は `viewFactory.LoadAsync` 後に必要な表示更新だけを呼ぶ。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `WorldGameLoopEntryPoint.Construct(...)` から `IObjectResolver` 引数が消えている
- [ ] `WorldGameLoopEntryPoint` 内に `TryResolve<ActorDetailPopupPresenter>` / `TryResolve<PlayerGameEventLogPresenter>` が存在しない
- [ ] HUD / popup / player log Presenter の依存が constructor / `Construct` 引数、または明示的な lifecycle interface で解決されている
- [ ] DI 登録漏れ時に nullable optional 経路で黙殺されず、compile / container resolve / PlayMode 確認で検出できる
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 2. Actor 選択入力が broad world state を直接走査し、View root 生成副作用まで持っている

重大度: 高

問題:

`WorldActorSelectionInputHandler` は入力ハンドラでありながら `IGameWorldStateReader` を直接受け取り、クリック時に `worldState.Actors` を全走査している。さらに hit test 中に `layerViewRegistry.GetOrCreateActorRoot(...)` を呼ぶため、入力処理が View root の生成副作用も持つ。M7 では `ActorDetailPopupPresenter` / `WorldActorStatusPresenter` から broad state 依存を外したが、Actor 選択経路に同じ境界問題が残っている。

原因:

Actor 選択に必要なのは「選択対象 Actor の screen position 一覧」だが、その read model / provider が定義されていないため、Input 層が Application の broad state と View registry の両方を知る形になっている。入力イベント処理、表示座標解決、View root 作成、選択状態更新の責務が 1 クラスに集まっている。

解決案:

Actor 選択用の narrow provider を用意し、Input ハンドラは provider から選択候補の `ActorId` と screen position または world position snapshot を受け取るだけにする。View root は既存 layer が存在する場合のみ参照する API に寄せ、hit test 中に `GetOrCreateActorRoot` を呼ばない。選択候補の更新は `ActorViewDataStore` / Presenter 側で管理し、入力ハンドラは action と選択 command の橋渡しに限定する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Input/Layer/WorldActorSelectionInputHandler.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapLayerViewRegistry.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `WorldActorSelectionInputHandler` の constructor から `IGameWorldStateReader` が消えている
- [ ] `WorldActorSelectionInputHandler.HandleClick(...)` が `worldState.Actors` を直接走査していない
- [ ] `WorldActorSelectionInputHandler` から `GetOrCreateActorRoot(...)` 呼び出しが消えている
- [ ] Actor 選択用 provider / read model が ActorId と hit test に必要な座標だけを公開している
- [ ] クリック選択、再クリック解除、非選択クリック解除、Next / Previous / Back の EditMode test が存在する
- [ ] `uloop.cmd compile --project-path Client` が成功している

## 統合判定

今回の commit は M7 表示機能を大きく進めており、既存 self-review の主要指摘の多くは現行コードで対応済みだった。一方で、上記 2 件はいずれも production 契約変更に該当する DI / Input / View 境界の問題であり、次マイルストーンに進む前に修正するのが妥当。

今回レビューでは `uloop compile` / EditMode test / PlayMode 確認は実行していない。対象は commit 差分の設計レビューであり、修正後の完了条件として機械的確認を要求する。

