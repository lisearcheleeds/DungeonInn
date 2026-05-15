# Milestone 5 完了レビュー 第5回 Codex

作成日: 2026-05-16

## レビュー範囲

- `docs/` 配下の全 Markdown を確認した。
- レビュー基準として `docs/guidelines/` 配下の以下を確認した。
  - `lighthouse-patterns.md`
  - `coding-rules.md`
  - `domain-design-guidelines.md`
  - `application-boundary-guidelines.md`
  - `implementation-quality-guidelines.md`
  - `self-review-preset.md`
- 対象実装として `Client/Assets/DungeonInn/Runtime/Scripts` 配下の現行コードを確認した。
- 既存レビューは本文と対応ログを確認し、現行コードで解消済みと判断できるものは未解決問題として再掲していない。

## 総評

Milestone 5 の主目的である `WorldActorDebugVisualizer` の撤去、debug Sphere / Plane から chunk mesh / SpriteRenderer 表示への置換は現行実装上は到達している。`WorldGameLoopEntryPoint` もゲーム進行順序を直接持たず、`WorldSimulationOrchestrator` 経由に寄せられている。

未解決の主なリスクは、表示差分 DTO の API 契約、マイルストーン未紐付け TODO、毎フレーム表示更新のポーリング、局所的な GC Alloc、using 順序違反である。いずれも Milestone 5 の表示置換そのものを覆す問題ではないが、Milestone 6 で NavMesh / 戦闘表示 / UI を積む前に解消しておくべき。

---

### 1. `ActorViewDataStore.ConsumeChanges()` が内部バッファ参照を返している

重大度: 高

問題:

`ActorViewDataStore.ConsumeChanges()` は `changedActors` と `removedActorIds` の内部 `List` を `IReadOnlyList` としてそのまま `ActorViewDataChangeBuffer` に渡している。メソッド名に `Consume` は含まれているが、返却リストが次回 `ConsumeChanges()` で `Clear()` される内部バッファであることが契約として明示されていない。

呼び出し元が現在の `WorldActorPresenter.UpdateVisuals()` のように即時消費する間は動くが、後続で差分を一時保持した場合、保持したリストの内容が次回更新で空または別内容に変わる。`implementation-quality-guidelines.md` の「内部バッファを返す API はコントラクトを明示する」に抵触する。

原因:

表示差分を GC Alloc なしで渡すため、内部リストを再利用する設計になっている。一方で、API 名・型・コメントが「返却値の寿命は次の Consume まで」「呼び出し側は保持してはいけない」という契約を表していない。

解決案:

短期対応として、`ActorViewDataChangeBuffer` または `ConsumeChanges()` に返却リストの寿命をコメントで明示する。根治対応としては、`ConsumeChanges(Action<ActorViewData>, Action<Guid>)` のような callback 消費形式にするか、`ActorViewDataChangeSet` として snapshot 配列を返す経路を用意する。GC 回避を優先する場合も、`DrainChangesTo(List<ActorViewData> changed, List<Guid> removed)` のように呼び出し側バッファへ書き出す契約にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/guidelines/self-review-preset.md`

完了条件:

- [ ] `ActorViewDataStore.ConsumeChanges()` が内部 `changedActors` / `removedActorIds` を契約不明な `IReadOnlyList` として返していない
- [ ] コピーを返す、callback 消費にする、または呼び出し側バッファへ drain する形のいずれかに統一されている
- [ ] 内部バッファ再利用を続ける場合、返却値の有効期間がメソッド名またはコメントで明示されている
- [ ] `WorldActorPresenter.UpdateVisuals()` が新しい契約に沿って即時消費している
- [ ] 変更後に `uloop.cmd compile --project-path Client` が成功している

---

### 2. Spawn / Faction 周辺の TODO が milestone / task に紐付いていない

重大度: 中

問題:

実挙動に影響する TODO が Runtime コードに残っている。特に spawn interval / spawn limit / spawn point / faction hostility は、マスタ・派閥仕様と現行挙動の差分に直結する。`TODO:` のまま残っており、`TODO(milestone:X):` や task への紐付けがない。

`implementation-quality-guidelines.md` は、実挙動・性能に影響する TODO を milestone / task に紐付けず残すことをハードゲートにしている。Milestone 5 の表示置換とは直接別領域だが、完了レビューとしては「既知の仕様差分が追跡不能」な状態である。

原因:

Milestone 2〜4 の最小ゲームループ実装で仮定数・仮 Faction を置いた後、マスタデータ化 / FactionMaster 化のタスク境界が明文化されないまま Milestone 5 に進んだ。

解決案:

Milestone 6 以降のマスタ・派閥整理タスクとして TODO を分類する。短期的には TODO コメントを `TODO(milestone:6):` または `TODO(backlog):` に更新し、理由と正式対応先を書く。実装に入る場合は、SpawnTableMaster に spawn timing / limit / spawn point を持たせるか、既存 GameConstants の暫定仕様として docs に明記する。Faction は `FactionMaster` または Application 側の敵対判定 Policy へ切り出す。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledAdventurerOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledMonsterOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEncounterTargetResolver.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/GameEventBus.cs`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/self-review/milestone5-general-review.md`
- `docs/self-review/milestone5-phase2-review-response.md`

完了条件:

- [ ] Runtime コード内の実挙動に影響する `TODO:` が milestone / task / backlog のいずれかに分類されている
- [ ] Spawn timing / limit / spawn point の正式対応先が task または design docs に記録されている
- [ ] Faction 敵対判定の正式対応先が task または design docs に記録されている
- [ ] 暫定値を維持する場合、GameConstants または design docs に「Milestone 5 では暫定」と明記されている
- [ ] `rg -n "TODO:" Client/Assets/DungeonInn/Runtime/Scripts` で未分類 TODO が残っていない

---

### 3. `WorldMapView.UpdateVisuals()` が毎フレーム全 layer を確認している

重大度: 中

問題:

`WorldMapView.UpdateVisuals()` は毎フレーム `EnqueueMissingLayerTiles()` を呼び、`IWorldMapViewDataProvider.GetLayers()` を通じて Ground と Dungeon floor を列挙している。既に `scheduledLayerIds` で二重生成は防いでいるが、全 layer の確認自体は毎フレーム残る。

現時点の floor 数では小さいが、Milestone 6 以降で dungeon floor が増え、NavMesh / chunk 表示更新 / layer 切り替えが入ると、表示更新の hot path に不要なポーリングが残る。`application-boundary-guidelines.md` の Frame Loop では、毎フレーム処理に不要な走査やコレクション生成を増やさない方針である。

原因:

Map layer 追加の通知経路がなく、View が「未構築 layer が増えたか」を毎フレーム確認する構成になっている。Milestone 5 中に chunk mesh 化は完了したが、layer 追加検出はイベント駆動または差分駆動へ移っていない。

解決案:

`DungeonFloorGenerated` などのイベント、または `WorldMapViewDataProvider` 側の revision / dirty flag で layer 追加を通知する。`WorldMapView.UpdateVisuals()` は pending chunk の消化だけを毎フレーム行い、layer 追加確認は初期化時・floor 生成時・revision 変化時に限定する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `docs/roadmap/milestone5-roadmap.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/self-review/milestone5-completion-review-3-codex.md`

完了条件:

- [ ] `WorldMapView.UpdateVisuals()` が毎フレーム `GetLayers()` で全 layer を確認していない
- [ ] 新規 layer 追加の検出がイベント、revision、明示的 refresh のいずれかで行われている
- [ ] pending chunk の分割生成は維持され、1フレームに大量 chunk を同期生成しない
- [ ] floor 追加時に map chunk が生成される EditMode test または PlayMode 確認ログがある
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 4. `DecideAdventurerReturnUseCase` が dirty 評価ごとに `HashSet<Guid>` を生成している

重大度: 中

問題:

`DecideAdventurerReturnUseCase.ExecuteAsync()` は dirty actor 評価時に `var foundDirtyActorIds = new HashSet<Guid>();` を生成する。これは毎フレームではなく schedule / dirty 駆動に近いが、帰還判断はゲームループ中に継続的に呼ばれるため、Actor 数増加時に不要な GC Alloc になる。

過去レビューで `WorldActorPresenter` の `HashSet<Guid>` 毎フレーム生成は修正済みだが、同種の「ループ内で再利用可能な集合を都度生成する」パターンが Application 側に残っている。

原因:

`actorIdBuffer` はフィールド化されているが、`foundDirtyActorIds` はメソッドローカルの一時集合として追加された。`AdventurerReturnTrackingService.RemoveMissingDirtyActors()` に渡すための作業バッファ所有者が曖昧になっている。

解決案:

`readonly HashSet<Guid> foundDirtyActorIds = new();` をフィールド化し、`ExecuteAsync()` 冒頭で `Clear()` する。さらに `returnTrackingService.CollectDirtyActorIds()` / `RemoveMissingDirtyActors()` の契約を見直し、dirty actor cleanup が `List<Guid>` と `HashSet<Guid>` の両方を必要としない形に整理できるか確認する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/DecideAdventurerReturnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdventurerReturnTrackingService.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/self-review/milestone5-phase2-review-response.md`

完了条件:

- [ ] `DecideAdventurerReturnUseCase.ExecuteAsync()` 内に `new HashSet<Guid>()` が残っていない
- [ ] 作業用 `HashSet<Guid>` はフィールドで再利用され、実行前に `Clear()` されている
- [ ] dirty actor cleanup の契約がテストで確認されている
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している

---

### 5. Application / View の一部ファイルで using 順序が coding rule と一致していない

重大度: 低

問題:

`coding-rules.md` は using 順序を `System`、Unity、LighthouseExtends、VContainer、プロジェクト内の順にする方針を定義している。一部ファイルでは `DungeonInn.*` が `System` より前にあり、外部ライブラリとプロジェクト using の間に不要な空行もある。

これは挙動バグではないが、プロジェクト共通ルールの完了前チェックに反する。レビュー・実装差分が増えるほど不要な整形差分が混ざりやすくなる。

原因:

Milestone 5 の対応で Application / View 間の依存整理が進んだ際、using 整理が機械的に統一されていない。既存ファイルの一部も同じ形式になっているため、局所修正時に踏襲された可能性が高い。

解決案:

対象ファイルの using を coding rule 順に整理する。可能なら `.editorconfig` または IDE の code cleanup 設定で using order を固定し、レビュー時の手作業検出に依存しない形にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GameLoopUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledAdventurerOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledMonsterOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/DecideAdventurerReturnUseCase.cs`
- `docs/guidelines/coding-rules.md`

完了条件:

- [ ] 指摘ファイルの using が coding rule の順序に揃っている
- [ ] 不要な空行と未使用 using が削除されている
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] 以後の task レビューで using 順序が完了前チェックに含まれている

---

## 確認済みで再掲しない項目

- `WorldActorDebugVisualizer` は削除済み。正式表示更新は `WorldGameLoopEntryPoint` から `WorldMapView` / `WorldActorPresenter` を呼ぶ構成になっている。
- debug Plane / Sphere の通常表示経路は現行コード検索上見つからない。map は chunk mesh、actor は `SpriteRenderer` 表示へ移行済み。
- `Camera.main` 依存は現行 World camera 操作経路では見つからない。`WorldScene.GetSceneCameraList()` と `WorldCameraController` 経由で扱われている。
- `Resources.Load` / `Addressables.LoadAssetAsync` の直接利用は `Client/Assets/DungeonInn/Runtime/Scripts` の通常実装では見つからなかった。
- `GameWorldState.RemoveActor()` の `FindIndex` 二重検索は現行コードでは swap-remove 系に整理済みであり、未解決として再掲しない。
- Actor / Inventory / Equipment の公開 API は `IReadOnlyInventory` / `IReadOnlyActorEquipment` へ寄っており、過去レビューの「可変オブジェクト直接公開」は現行コードでは主要問題として再掲しない。

## 最終チェック

- [x] 各レビュー項目に「問題」がある
- [x] 各レビュー項目に「原因」がある
- [x] 各レビュー項目に「解決案」がある
- [x] 各レビュー項目に「根拠となるファイルリスト」がある
- [x] 各レビュー項目に「完了条件」がある
- [x] 既存レビューの対応ログを確認した
- [x] 解消済み項目を未解決として再掲していない
- [x] 未解決項目を対応済みとして扱っていない
- [x] レビュー結果を `docs/self-review/` 配下に保存した

## 検証

- [x] `uloop.cmd compile --project-path Client` 成功（Error 0 / Warning 0）
- [x] `uloop.cmd run-tests --project-path Client --test-mode EditMode` 成功（245 passed）
