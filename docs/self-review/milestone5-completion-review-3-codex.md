# Milestone 5 完了確認レビュー 3（Codex）

作成日: 2026-05-13

## レビュー範囲

Milestone 5 完了確認として、`docs/` 配下のロードマップ、設計資料、既存 self-review、開発ガイドラインを確認したうえで、`Client/Assets/DungeonInn/Runtime/Scripts` の主要実装をレビューした。

観点は以下。

- 設計
- 整合性
- パフォーマンス
- 重複した機能を持つクラス / データクラス、その他総合

注記:

- 今回はレビュー文書作成のみ。コード修正、`uloop.cmd compile --project-path Client`、PlayMode 確認は実施していない。
- 既存レビュー本文で未対応に見える項目でも、現行実装または既存レビュー末尾の対応ログで解消済みと確認できたものは原則として再掲しない。
- 例: `AdvanceActorAiOrchestrator` は現行 `WorldGameLoopEntryPoint.TickAsync()` に接続済みのため、未接続問題としては扱わない。

## 現在状態索引

この文書の本文はレビュー時点の指摘履歴として残す。完了判断は下表と末尾の対応ログを優先する。

| 項目 | 現在状態 | 根拠 |
|---|---|---|
| 設計1: View 層がゲーム進行パイプラインを握っている | 対応済み | `WorldSimulationOrchestrator` 追加、`WorldGameLoopEntryPoint` の直接 orchestration 除去、対応ログ 2026-05-13 |
| 設計2: 表示 DTO が Domain オブジェクトへの遅延参照を閉じ込めている | 対応済み | `WorldMapLayerViewData` の値スナップショット化、対応ログ 続き |
| パフォーマンス1: 毎フレーム処理に schedule tick / event-driven で十分な処理が混在 | 対応済み | schedule tick 側への移動、対応ログ 続き2 |
| 設計3: 戦闘イベントがトランザクション完了前に発行される | 対応済み | `BufferedEventPublisher` 経由へ変更、対応ログ 続き3 |
| その他1: Lighthouse 禁止 API が基盤実装に残っている | 対応済み | `ProductAssetLoader` 削除、Launcher 例外を docs 明文化、対応ログ 続き4 |
| 設計4: UseCase / Service の命名と配置がまだ混在している | 対応済み | `XxxUseCase` へ責務名統一、対応ログ 続き5 |
| 整合性1: アイテム / ドロップ仕様と実装マスタが不一致 | 対応済み | `spec_item_money.md` から現在値マスタ表を削除 |
| 整合性2: AdventurerBattleRecord の設計と実装が一致していない | 対応済み | `game-event-design.md` を現行のシーンスコープ累積統計責務へ更新 |
| 整合性3: 既存レビュー本文と対応ログの状態が混在している | 対応済み | 本索引で対応済み / 未対応の読み方を明文化 |
| パフォーマンス2: 戦闘遭遇検出が毎フレーム全 Actor を再構築・探索している | 対応済み | `ActorSpatialIndexService` 追加、dirty Actor のみ検出、対応ログ 続き7 |
| パフォーマンス3: 売却 / 取引経路で GC Alloc が起きやすい | 対応済み | 単一 stack 取引経路追加、売却バッファ再利用、LINQ 正規化除去、対応ログ 続き8 |
| パフォーマンス4: AreaEffect target 解決が Actor 全走査になっている | 対応済み | `AttackAreaTargetResolver` が `ActorSpatialIndexService` 近傍候補を利用、対応ログ 続き9 |
| 既存レビュー2の AI 未接続指摘 | 対応済み | `AdvanceActorAiOrchestrator` は現行ゲーム進行に接続済みのため未対応扱いしない |
| 上表以外のレビュー本文項目 | 未対応 / 一部対応 / 延期 | 各項目の完了条件と今後の対応ログを正とする |

## 結論

Milestone 5 の主目的である debug Plane / Sphere から chunk mesh / SpriteRenderer 表示への置き換えは、現行実装上は到達している。

一方で、Milestone 6 で NavMesh、AI、戦闘表示、移動品質を拡張する前に、`WorldGameLoopEntryPoint` に残るゲーム進行パイプラインを Application 層へ移すことが最優先。ここを整理すると、毎フレーム処理の分類、戦闘イベントの発行タイミング、UseCase / Service 命名、View 用 DTO の境界もまとめて改善しやすい。

## 設計レビュー

### 1. View 層がゲーム進行パイプラインを握っている

重大度: 高

問題:

`WorldGameLoopEntryPoint` が多数の UseCase / Orchestrator を直接注入し、`TickAsync()` 内で spawn、AI、lifecycle、combat、projectile、area effect、item、inn recovery、daily report までの順序を固定している。MonoBehaviour が Application の orchestration 責務を持っており、`docs/guidelines/application-boundary-guidelines.md` の「Unity EntryPoint にゲーム進行順序を置かない」方針と一致していない。

原因:

Milestone 5 は View 表示置き換えが主目的だったため、既存のゲーム進行呼び出し順序が EntryPoint に残った。AI Orchestrator は接続済みになったが、接続先が View 層のままで、根本の境界問題は未解消。

解決案:

Application 層に `AdvanceWorldFrameUseCase` または `WorldSimulationOrchestrator` を追加し、1フレーム内のゲーム進行順序をそこへ移す。`WorldGameLoopEntryPoint` は初期化、delta time の受け渡し、キャンセル管理、camera / view 更新の呼び出しに限定する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/self-review/milestone5-completion-review-2-total.md`

完了条件:

- [ ] `WorldGameLoopEntryPoint` が個別 UseCase / Orchestrator を直接注入していない
- [ ] `WorldGameLoopEntryPoint.TickAsync()` に spawn / AI / lifecycle / combat / projectile / area effect / item / inn recovery / daily report の実行順序が残っていない
- [ ] Application 層に `AdvanceWorldFrameUseCase` または `WorldSimulationOrchestrator` が存在し、1フレームのゲーム進行順序を所有している
- [ ] `WorldGameLoopEntryPoint` は初期化、delta time 受け渡し、キャンセル管理、camera / view 更新の呼び出しだけを担当している
- [ ] 追加した Application 側パイプラインの EditMode test がある
- [x] `uloop.cmd compile --project-path Client` が成功している

### 2. 表示 DTO が Domain オブジェクトへの遅延参照を閉じ込めている

重大度: 中

問題:

`WorldMapLayerViewData` は View 用 DTO に見えるが、内部に `Func<GridPosition, WorldMapCellViewKind>` を持ち、`groundMap` や `DungeonFloor` をクロージャで参照している。`WorldMapView` が `GetCellKind()` を呼ぶたびに Domain 側の `IsWalkable()` / `IsStairPosition()` 相当へ到達する。

原因:

View へ `IGameWorldStateReader` や `MapLayer` を直接渡す問題は緩和されたが、DTO 化が値のスナップショットではなく遅延 resolver になっている。

解決案:

`WorldMapLayerViewData` は cell kind 配列、chunk build 用 immutable data、または `MapChunkViewData` へ変換済みの値を返す。View 層は Domain 判定を実行せず、描画用データを消費するだけにする。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/WorldViewDataProviders.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `docs/roadmap/milestone5-roadmap.md`

完了条件:

- [ ] `WorldMapLayerViewData` が `Func<GridPosition, WorldMapCellViewKind>` を保持していない
- [ ] `WorldMapLayerViewData` または後継 DTO が cell kind / chunk view data の値スナップショットを保持している
- [ ] `WorldMapView` が `GroundMap` / `DungeonFloor` / `MapLayer` の Domain 判定に到達しない
- [ ] map chunk 生成が View 用 DTO の値だけで実行できる
- [ ] DTO 変換処理の EditMode test がある
- [x] `uloop.cmd compile --project-path Client` が成功している

### 3. 戦闘イベントがトランザクション完了前に発行される

重大度: 中

問題:

`CombatAttackOccurred` はダメージ適用直後、死亡解決・経験値・ドロップ・Actor 除去前に publish される。`ProjectileHit` / `AreaEffectHit` も linked effect の適用前に publish される。イベントを「確定した状態変化の通知」として扱う設計と順序がずれている。

原因:

Resolver / Executor が処理途中で即時 publish しており、UseCase の状態変更完了後にイベントをまとめて発行する設計になっていない。特に Projectile / AreaEffect は「命中検出」と「効果適用完了」が同じイベント名に混ざっている。

解決案:

戦闘処理中は発生イベントを一時収集し、ダメージ、死亡解決、報酬、ドロップ、Actor 除去が完了した後に publish する。事前通知が必要なら `ProjectileHitDetected` / `AreaEffectHitDetected` のように確定通知と名前を分ける。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatDamageResolver.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEffectExecutor.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/ActorDefeatOrchestrator.cs`
- `docs/design/game-event-design.md`

完了条件:

- [ ] `ProjectileHit` / `AreaEffectHit` がダメージ・linked effect 適用前に publish されていない
- [ ] `CombatAttackOccurred` が死亡解決・報酬・ドロップ・Actor 除去との順序契約を明文化している
- [ ] 確定通知と事前通知が必要な場合、イベント名またはイベント種別が分離されている
- [ ] 戦闘処理の発生イベントを収集し、トランザクション完了後に publish する経路がある
- [ ] Projectile / AreaEffect / 通常攻撃でイベント発行順を検証する EditMode test がある
- [ ] `docs/design/game-event-design.md` が実装後のイベント契約と一致している
- [x] `uloop.cmd compile --project-path Client` が成功している

### 4. UseCase / Service の命名と配置がまだ混在している

重大度: 中

問題:

`ChargeInnFeeService`、`GrantExperienceService`、`DropItemService`、`DespawnAdventurerService` は `Application/UseCase` 配下にあり、実体は長期状態を持つ Service ではなく、副作用を起こすコマンド実行単位に近い。ガイドライン上の `UseCase` / `Service` / `StateService` / `Orchestrator` の区別と配置が一致していない。

原因:

既存互換を優先して一括リネームを保留したため、責務整理後も古い命名とフォルダが残っている。

解決案:

Milestone 6 のゲームループ整理に合わせ、コマンド実行単位は `XxxUseCase`、状態保持は `XxxStateService`、共有補助処理は `Application/Service` へ段階的に移す。既存名を残す場合も、例外理由を該当タスクに明記する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/ChargeInnFeeService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/GrantExperienceService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DropItemService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DespawnAdventurerService.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `Application/UseCase` 配下に、長期状態を持つ `XxxStateService` が残っていない
- [ ] 副作用を起こすコマンド実行単位の命名が `XxxUseCase` または明示的な Domain/Application Service として整理されている
- [ ] `ChargeInnFeeService` / `GrantExperienceService` / `DropItemService` / `DespawnAdventurerService` の責務と配置がガイドラインに沿っている
- [ ] 既存名を残す場合、例外理由と追跡タスクが docs または task に記録されている
- [ ] namespace とフォルダが一致している
- [ ] `uloop.cmd compile --project-path Client` が成功している

## 整合性レビュー

### 1. アイテム / ドロップ仕様と実装マスタが不一致

重大度: 中

問題:

`docs/design/spec_item_money.md` に「現在のアイテムマスタ」「現在のドロップテーブル」という実データ表があり、実装の `HardcodedMasterRepository` と差分が発生している。`spec_item_money.md` はアイテム・ドロップの振る舞いを記載する設計資料であり、現在値のマスタデータ表を持つ責務ではない。

原因:

振る舞い仕様と実装マスタの現在値を同じ docs に混在させたため、マスタ変更のたびに設計資料側の追従が必要になり、差分がレビュー指摘として現れている。

解決案:

`spec_item_money.md` から現在値のアイテムマスタ表とドロップテーブル表を削除し、`ItemMaster` / `ActorDropEntry` / `DropItemUseCase` の構造と振る舞いだけを残す。マスタデータの現在値は実装マスタまたは将来のマスタデータ定義側を正とする。

根拠:

- `docs/design/spec_item_money.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`

完了条件:

- [x] `docs/design/spec_item_money.md` から「現在のアイテムマスタ」表が削除されている
- [x] `docs/design/spec_item_money.md` から「現在のドロップテーブル」表が削除されている
- [x] `DropItemService` 表記が現行責務名の `DropItemUseCase` に更新されている
- [x] `spec_item_money.md` がマスタ現在値ではなく、構造と振る舞いを説明する資料になっている
- [x] マスタと仕様の差分を確認するレビュー記録が残っている
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 2. AdventurerBattleRecord の設計と実装が一致していない

重大度: 中

問題:

`docs/design/game-event-design.md` では `AdventurerBattleRecord` が `CombatEncounterStarted` / `CombatEncounterEnded` で1戦闘ごとのサマリーを作り、`ActorExitedDungeon` で確定・保管する想定になっていた。一方、現行実装の `AdventurerBattleRecordService` は `CombatEncounterStarted` と `CombatAttackOccurred` からシーンスコープの累積戦闘統計を作る責務であり、設計 docs の責務名と寿命が実装より大きく書かれていた。

原因:

戦闘ログの最小集計が現行仕様として採用されているが、設計 docs だけが将来構想の「戦闘単位サマリー」「退出時の保管」まで含んだ表現のまま残っていた。

解決案:

`docs/design/game-event-design.md` を現行実装に合わせ、`AdventurerBattleRecordService` はシーンスコープの累積統計購読者であると明記する。`CombatEncounterEnded` / `ActorExitedDungeon` は現状では確定条件ではなく表示契機に留め、永続化や1戦闘ごとの保管ストアが必要になった時に別責務として追加する。

根拠:

- `docs/design/game-event-design.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AdventurerBattleRecordService.cs`

完了条件:

- [x] 累積統計のみを正とする方針に合わせ、`docs/design/game-event-design.md` の仕様が現在責務へ縮小されている
- [x] `AdventurerBattleRecordService` の購読対象が `CombatEncounterStarted` / `CombatAttackOccurred` であることと一致している
- [x] `CombatEncounterEnded` / `ActorExitedDungeon` は現状の確定条件ではなく表示契機であると明記されている
- [x] 永続化や1戦闘ごとの保管ストアは将来追加責務であると明記されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 3. 既存レビュー本文と対応ログの状態が混在している

重大度: 低

問題:

`docs/self-review/milestone5-completion-review-2-total.md` の本文には「AI 実行基盤がゲームループに接続されていない」などの指摘が残っているが、同ファイル末尾の対応ログおよび現行実装では `AdvanceActorAiOrchestrator` は接続済み。レビュー本文だけを読むと未完了扱いに見えるため、この文書の冒頭に現在状態索引を追加して、対応済み項目と未対応項目の読み方を分離する必要があった。

原因:

レビュー本文の問題一覧を削除・上書きせず、末尾ログで対応状況を追記する運用のため、対応済み項目と未対応項目が同じ文書内に混在している。

解決案:

既存レビューは履歴として維持しつつ、この文書の冒頭に「現在状態索引」を追加する。今後の完了判断はレビュー本文単体ではなく、現在状態索引と末尾の対応ログを正とする。

根拠:

- `docs/self-review/milestone5-completion-review-2-total.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`

完了条件:

- [x] 後続統合レビューである本ファイルに、対応済み項目と未対応項目の現在状態表がある
- [x] AI 未接続など現行コードで解消済みの項目が、未対応扱いされないことが明記されている
- [x] 未対応項目には「未対応 / 一部対応 / 延期」として扱う状態が明記されている
- [x] 完了済みとする項目には、根拠となる対応ログまたは docs 更新が記録されている

## パフォーマンスレビュー

### 1. 毎フレーム処理に schedule tick / event-driven で十分な処理が混在している

重大度: 高

問題:

`WorldGameLoopEntryPoint.Update()` から毎フレーム `TickAsync().Forget()` され、戦闘検出、装備更新、売却、回復アイテム使用、帰還判定などがフレーム単位で呼ばれている。Actor / Inventory / Item 数が増えると、毎フレームの CPU と GC 負荷が増えやすい。

原因:

ゲームループ内で `FrameLoop`、`ScheduleTick`、`EventDriven` の分類がコード構造として分離されていない。

解決案:

毎フレーム必須処理は移動、戦闘進行、projectile、area effect などに限定する。装備更新、売却、回復アイテム使用、帰還判定は schedule tick、Actor 状態変化、Inventory 変化、dirty flag 起点へ移す。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/UpdateEquipmentUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SellItemsUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/UseRecoveryItemOrchestrator.cs`

完了条件:

- [ ] ゲーム進行処理が `FrameLoop` / `ScheduleTick` / `EventDriven` に分類されている
- [ ] `UpdateEquipmentUseCase.Execute()` が毎フレーム無条件で呼ばれていない
- [ ] `SellItemsUseCase.Execute()` が毎フレーム無条件で呼ばれていない
- [ ] `UseRecoveryItemOrchestrator.ExecuteAsync()` が毎フレーム無条件で呼ばれていない
- [ ] `DecideAdventurerReturnUseCase.ExecuteAsync()` が毎フレーム無条件で呼ばれていない、または毎フレーム実行が必要な理由が明文化されている
- [ ] dirty flag / schedule tick / event-driven 化した処理の回帰テストがある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 2. 戦闘遭遇検出が毎フレーム全 Actor を再構築・探索している

重大度: 高

問題:

`DetectCombatEncounterUseCase` が毎フレーム呼ばれ、`CombatEncounterTargetResolver.Rebuild(actors)` による空間情報再構築と、Actor ごとの近傍探索・Line of Sight 判定を行っている。Actor 数が増えるほど CPU 負荷が伸びやすい。

原因:

Actor の移動・生成・削除に対する差分更新ではなく、毎フレーム全体再構築になっている。

解決案:

Actor 生成・移動・死亡時だけ spatial index を更新する。戦闘検出は移動した Actor のみ、または複数フレームに分散して実行する。Milestone 6 の NavMesh / 移動改善と同時に layer / cell ベースの actor spatial index を共通化する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DetectCombatEncounterUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEncounterTargetResolver.cs`

完了条件:

- [x] `DetectCombatEncounterUseCase` が毎フレーム全 Actor の spatial index を再構築していない
- [x] Actor 生成・移動・死亡・layer 移動時に spatial index を差分更新する経路がある
- [x] 戦闘検出の対象が移動 Actor または分散実行対象に限定されている
- [x] Line of Sight 判定の呼び出し回数が Actor 全組み合わせに近い形で増えない
- [x] spatial index の追加・削除・移動更新を検証する EditMode test がある
- [x] `uloop.cmd compile --project-path Client` が成功している

### 3. 売却 / 取引経路で GC Alloc が起きやすい

重大度: 中

問題:

`SellItemsUseCase` で Actor ごとに `new List<ItemStack>()` を作り、売却ごとに `new[] { stack }` / `new[] { price }` を生成している。`ExchangeExecutor.NormalizeItems()` も LINQ `GroupBy` / `Select` / `ToArray` を使う。現在の呼び出し位置では毎フレーム GC Alloc の原因になる。

原因:

売却処理がイベント起点ではなく毎フレーム実行されているうえ、取引 API が複数 ItemStack の正規化を常に通す形になっている。

解決案:

まず売却を帰還・回復開始・施設到着などのイベント駆動へ移す。加えて単一 stack 用 overload、再利用バッファ、LINQ なしの正規化処理を用意する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SellItemsUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Commerce/ExchangeExecutor.cs`

完了条件:

- [ ] 売却処理が毎フレーム経路から外れている
- [ ] 単一 stack 売買で `new[] { stack }` / `new[] { price }` を生成しない API または実装になっている
- [ ] `ExchangeExecutor.NormalizeItems()` が高頻度経路で LINQ `GroupBy` / `Select` / `ToArray` を使っていない
- [ ] 売却候補リストが必要な場合、再利用バッファまたはイベント単位の一時生成に限定されている
- [ ] 売却処理の正規化・取引結果を検証する EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 4. AreaEffect target 解決が Actor 全走査になっている

重大度: 中

問題:

`AttackAreaTargetResolver.ResolveTargets()` は AreaEffect ごとに全 Actor を走査する。Fan 判定では `Math.Sqrt` と `Math.Atan2` も使用しており、AreaEffect 数 x Actor 数で負荷が増える。

原因:

戦闘遭遇用の spatial index が AreaEffect target 解決に共通利用されていない。範囲判定も三角関数ベースのまま。

解決案:

Layer / cell ベースの actor spatial index を共通化し、効果範囲周辺セルだけを候補にする。Fan 判定は dot / cross と距離二乗で行い、三角関数を避ける。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AttackAreaTargetResolver.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceAreaEffectUseCase.cs`

完了条件:

- [ ] `AttackAreaTargetResolver` が AreaEffect ごとに全 Actor を無条件走査していない
- [ ] Layer / cell ベースの actor spatial index から候補 Actor を取得している
- [ ] Fan 判定が `Math.Sqrt` / `Math.Atan2` ではなく距離二乗と dot / cross ベースになっている
- [ ] Circle / Rectangle / Fan の命中判定を検証する EditMode test がある
- [ ] Actor 数と AreaEffect 数が増えても候補数ベースで処理できることがコード上確認できる
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 5. Map chunk mesh 生成が同期スパイクになりうる

重大度: 中

問題:

`WorldMapView.UpdateVisuals()` は毎フレーム未構築 layer を確認し、見つけると layer の全 chunk を同期生成する。`MapMeshBuildService.BuildChunk()` は chunk ごとに `List`、`Dictionary`、`Mesh`、material 配列を作り、`RecalculateNormals()` / `RecalculateBounds()` も走る。

原因:

floor 追加イベントや chunk build queue がなく、mesh 生成バッファも使い捨てになっている。

解決案:

floor 追加時に生成 queue へ積み、1 frame の chunk 生成数を制限する。頂点 / UV / triangle バッファを再利用し、平面 mesh なら法線・bounds を明示して再計算を避ける。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs`

完了条件:

- [ ] floor / layer 追加時に chunk build queue へ積む経路がある
- [ ] 1 frame に生成する chunk 数を制限できる
- [ ] `MapMeshBuildService` が頂点 / UV / triangle バッファを必要に応じて再利用している
- [ ] 平面 chunk で `RecalculateNormals()` / `RecalculateBounds()` を毎回呼ぶ必要がない実装になっている、または呼ぶ理由が明文化されている
- [ ] chunk 生成の分割実行を検証する PlayMode または EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 6. 経路探索とダンジョン階層選択で LINQ / コレクション生成が残っている

重大度: 中

問題:

`AStarPathfinder` は経路計算ごとに `List` / `Dictionary` を生成し、`openSet.Contains`、`PopLowestF`、`RemoveAt` を使う。`SelectDungeonTargetFloorUseCase` や `SpawnScheduledMonsterOrchestrator` でも LINQ ソート・配列生成が残っている。

原因:

Actor 数や floor 数が小さい段階の簡易実装が残っており、探索バッファ再利用や順序付き floor cache がない。

解決案:

A* は探索バッファ再利用、優先度キュー、目的地共有型のキャッシュを検討する。floor 選択は master 読み込み時の difficulty cache や `GameWorldState` 側の順序付き floor list に寄せ、実行時の LINQ ソートを避ける。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Pathfinding/AStarPathfinder.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SelectDungeonTargetFloorUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/SpawnScheduledMonsterOrchestrator.cs`

完了条件:

- [ ] `AStarPathfinder` の高頻度経路で探索用 `List` / `Dictionary` の使い捨て生成が抑制されている、または呼び出し頻度が限定されている
- [ ] A* の open set が O(n) 探索前提の `PopLowestF` から改善されている、または現規模で許容する根拠が記録されている
- [ ] `SelectDungeonTargetFloorUseCase` の実行時 LINQ ソート・配列生成が削減されている
- [ ] `SpawnScheduledMonsterOrchestrator` の floor 選択で実行時 LINQ ソート・配列生成が削減されている
- [ ] 経路探索と floor 選択の結果を検証する EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 7. 描画更新が毎フレーム全 Actor DTO を再構築する

重大度: 低

問題:

`ActorViewDataProvider.GetActors()` が毎回 `actors.Clear()` して全 Actor 分の `ActorViewData` を追加し、`WorldActorPresenter` も全 Actor を処理する。現状の規模では許容できるが、Actor 数が増えると表示側の CPU 負荷が増える。

原因:

Actor の位置・見た目・生成消滅に対する dirty flag / diff stream がないため、毎フレーム全量同期になっている。

解決案:

Actor の生成・削除・位置・表示種別変更を差分更新にする。カメラ yaw 変更時のみ全 Actor の向き更新を許容する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/WorldViewDataProviders.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`

完了条件:

- [ ] Actor 生成・削除・位置・表示種別変更を差分として取得できる経路がある
- [ ] `ActorViewDataProvider.GetActors()` が毎フレーム全 Actor DTO の再構築を必須としない
- [ ] `WorldActorPresenter` が差分更新を扱える
- [ ] カメラ yaw 変更時だけ全 Actor の向き更新が走る設計になっている
- [ ] Actor 生成・削除・移動・layer 切替の View 更新を検証する PlayMode または EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

## 重複した機能を持つクラス / データクラス、その他総合

### 1. Lighthouse 禁止 API が基盤実装に残っている

重大度: 重大

問題:

`Resources.LoadAsync` と `SceneManager.LoadSceneAsync` の直接使用が残っている。今回の Milestone 5 実装で新規追加されたものではないが、プロジェクトのハードゲートとしては例外扱いが必要。

原因:

ScreenStack 生成と Launcher / reboot 経路が Lighthouse 移行前の暫定実装として残っている。

解決案:

ScreenStack prefab 読み込みは `IAssetManager` / `IAssetScope` 経由へ移す。Launcher の scene load は Lighthouse の scene transition / product scene manager 側に寄せる。すぐ直さない場合は「既存違反・今回追加なし」として明示的に追跡し、例外理由を docs に固定する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs`
- `docs/guidelines/lighthouse-patterns.md`

完了条件:

- [ ] `ProductAssetLoader` が `Resources.LoadAsync` を直接使用していない
- [ ] `Launcher` が `SceneManager.LoadSceneAsync` を直接使用していない、または明文化された例外として追跡されている
- [ ] ScreenStack prefab 読み込みが `IAssetManager` / `IAssetScope` または Lighthouse の正式ロード経路に乗っている
- [ ] scene load / reboot 経路が Lighthouse の scene transition / product scene manager に乗っている、または代替不能な理由が docs に記録されている
- [ ] 禁止 API 検索で新規違反がない
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 2. Factory / Request の上層が重複している

重大度: 中

問題:

`ActorFactory` / `ActorFactoryRequest` / `ActorFactoryCore` で生成処理は共通化済みだが、上層に `AdventurerFactory` / `MonsterFactory`、`AdventurerCreateRequest` / `MonsterCreateRequest` が並列に残っている。差分は主に `DisplayName` と必須 Behavior 種別で、構造が近い。

原因:

Adventurer / Monster それぞれの生成経路を先に実装した後、共通 factory core が追加されたが、上層 facade / request の統合判断が未完了。

解決案:

`ActorSpawnRequest` に統合し、`DisplayName` を任意項目にする。外部公開は `IActorFactory` に寄せ、専用 factory は呼び出し側の意味を明確にする facade として必要な場合だけ残す。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/ActorFactoryRequest.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/AdventurerCreateRequest.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/MonsterCreateRequest.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/AdventurerFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Factory/MonsterFactory.cs`

完了条件:

- [ ] `AdventurerCreateRequest` / `MonsterCreateRequest` の重複フィールドが `ActorSpawnRequest` または同等の共通 request に統合されている
- [ ] `DisplayName` など差分フィールドが任意項目または専用 facade の責務として整理されている
- [ ] 外部公開 factory が `IActorFactory` 中心になっている、または専用 factory を残す理由が docs に記録されている
- [ ] Adventurer / Monster 生成の既存テストが通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 3. 経済系 DTO が近いフィールドを複数型で重複保持している

重大度: 中

問題:

`InnEconomySummary` は切り出されたが、`InnEconomyStatus` と `InnDailyReport` が同じ summary への proxy プロパティを大量に持ち、`InnEconomyStatusCalculator` が report から status へ詰め替えている。現在値と履歴の区別は必要だが、DTO の正典がまだ分散している。

原因:

UI 用 status と履歴用 report の互換 API を維持するため、共通 summary 導入後も alias プロパティが残っている。

解決案:

現在値は `InnEconomyStatus(CurrentDay, InnEconomySummary)`、履歴は `InnDailyReport(Day, InnEconomySummary)` に薄く保つ。UI 専用名の alias は ViewModel / Presenter 側へ寄せる。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatus.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/InnDailyReport.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatusCalculator.cs`

完了条件:

- [ ] 現在値 DTO と履歴 DTO の共通値の正典が `InnEconomySummary` に集約されている
- [ ] `InnEconomyStatus` が UI alias の大量 proxy を持たない、または互換維持理由が明記されている
- [ ] `InnDailyReport` が履歴として必要な値だけを持っている
- [ ] `InnEconomyStatusCalculator` が report から status へ不要な詰め替えをしていない
- [ ] 経済 status / daily report の変換・集計を検証する EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 4. `AdventurerGuild` / `Facility` が可変 `Inventory` を公開している

重大度: 中

問題:

`Actor` は `IReadOnlyInventory` 公開へ整理されている一方、`AdventurerGuild` / `Facility` は `public Inventory Inventory { get; }` のまま。集約外から在庫変更できるため、取引・補充・ログ・統計の副作用が分散しやすい。

原因:

前回対応では Actor の集約境界整理が中心で、Guild / Facility 側の読み取り専用化までは完了していない。

解決案:

読み取りは `IReadOnlyInventory`、変更は `AddItems` / `RemoveItems` / `TrySpendGold` / `IExchangeParticipant` の操作メソッド経由へ寄せる。取引以外の直接変更経路がある場合は UseCase に集約する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/AdventurerGuild.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Facility/Facility.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs`

完了条件:

- [ ] `AdventurerGuild.Inventory` の公開型が `IReadOnlyInventory` または読み取り専用 API になっている
- [ ] `Facility.Inventory` の公開型が `IReadOnlyInventory` または読み取り専用 API になっている
- [ ] Guild / Facility の在庫変更が集約メソッドまたは `IExchangeParticipant` 操作メソッド経由に限定されている
- [ ] 外部コードが Guild / Facility の `Inventory.Add` / `Remove` / `TrySpendGold` を直接呼んでいない
- [ ] Guild / Facility の取引・補充・支払いの EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 5. `IMasterRepository` が広すぎる

重大度: 中

問題:

`IMasterRepository` が item、equipment、weapon、actor archetype、spawn、level、dungeon floor などを1つの interface に集約している。多くの UseCase が必要以上の master にアクセスでき、依存範囲が広がる。

原因:

ハードコードマスタをまとめて扱うために単一 repository として始まり、機能追加に伴って interface が肥大化した。

解決案:

`IActorMasterRepository`、`ICombatMasterRepository`、`IDungeonMasterRepository` など用途別 interface に分割し、UseCase には必要最小の repository だけ注入する。既存の `HardcodedMasterRepository` は複数 interface を実装すればよい。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Master/IMasterRepository.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductLifetimeScope.cs`

完了条件:

- [ ] `IMasterRepository` の用途別分割方針が docs または task に記録されている
- [ ] `IActorMasterRepository` / `ICombatMasterRepository` / `IDungeonMasterRepository` など必要な小 interface が定義されている
- [ ] UseCase が必要最小限の master repository interface だけを注入している
- [ ] `HardcodedMasterRepository` が必要な小 interface を DI 登録している
- [ ] 既存 UseCase のマスタ参照テストが通る
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 6. Domain Entity 内の factory / calculator 依存方針が曖昧

重大度: 低

問題:

`Actor.RefreshWeaponCalculator()` が `WeaponCalculatorFactory.Create` / `WeaponCombatCalculatorFactory.Create` を直接呼び、`RefreshParams()` が `new ActorParamCalculator()` している。純粋な Domain 計算として許容するのか、DI すべき拡張可能な計算戦略なのかが読み取りにくい。

原因:

static catalog 依存は解消済みだが、factory / calculator の責務分類が明文化されていない。

解決案:

完全な純粋計算なら static utility / domain calculator として命名・責務を明確化する。拡張可能な戦略にするなら Actor 外で計算して結果を適用する形へ寄せる。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/WeaponCalculators.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/WeaponCombatCalculatorFactory.cs`

完了条件:

- [ ] `WeaponCalculatorFactory` / `WeaponCombatCalculatorFactory` / `ActorParamCalculator` が純粋 Domain calculator なのか DI 対象の戦略なのか docs に明記されている
- [ ] DI 対象の戦略として扱う場合、`Actor` が factory / calculator を直接生成していない
- [ ] 純粋 Domain calculator として扱う場合、static / `new` 利用が許容される理由が設計資料に記録されている
- [ ] Actor の装備変更・パラメータ再計算・武器計算の EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 7. QuickFirst 周辺の命名が混在している

重大度: 低

問題:

`QuickFirst` フォルダ / ファイルに `FirstSceneScene`、`FirstScenePresenter`、`IQuickFirstPresenter` が混在し、`Scene` も重複している。小さい問題だが、scene 追加時のコピー元として使うと命名ブレが広がる。

原因:

初期 scene の仮名称と正式名称が整理されないまま残っている。

解決案:

Scene ID を `FirstScene` のまま維持するならコード名も `FirstScene` 系へ統一する。`QuickFirst` を正式名にするなら class / file / presenter / lifetime scope を合わせる。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/QuickFirst/QuickFirstScene.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/QuickFirst/QuickFirstLifetimeScope.cs`

完了条件:

- [ ] QuickFirst / FirstScene の正式名称が決定されている
- [ ] folder / class / file / interface / presenter / lifetime scope の名前が正式名称に統一されている
- [ ] Scene ID とコード名の対応が docs またはコメントで追跡可能になっている
- [ ] 自動生成 `.g.cs` を手動編集していない
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 8. Milestone 5 View 構成のテストが不足している

重大度: 低

問題:

Domain / UseCase の EditMode テストは存在する一方で、`WorldGameLoopEntryPoint`、`WorldMapView`、`WorldActorPresenter`、`WorldLifetimeScope` の DI 構成・表示更新順序を直接検証するテストは見当たらない。Milestone 5 の主要成果が View 接続であることを考えると、回帰検知が弱い。

原因:

Unity scene / View 接続の確認が PlayMode smoke test と手動確認中心になっている。

解決案:

ゲーム進行パイプラインを Application 層へ移したうえで、その orchestrator を EditMode で検証する。View 側は `WorldMapView` の chunk 生成、`WorldActorPresenter` の生成・削除・layer 切替、`WorldLifetimeScope` の DI 解決を最小 PlayMode test で確認する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`

完了条件:

- [ ] Application 層へ移したゲーム進行パイプラインの EditMode test がある
- [ ] `WorldMapView` の chunk 生成を検証するテストがある
- [ ] `WorldActorPresenter` の Actor 生成・削除・移動・layer 切替を検証するテストがある
- [ ] `WorldLifetimeScope` の主要 DI 解決を確認する PlayMode または構成テストがある
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している
- [ ] `uloop.cmd compile --project-path Client` が成功している

## 推奨対応順

### Milestone 5 完了判断前に明示しておく項目

1. Lighthouse 禁止 API は既存違反として別タスク化するか、Milestone 5 完了前に修正するかを決める。
2. `uloop.cmd compile --project-path Client`、EditMode tests、PlayMode smoke test の最新証跡を残す。
3. 既存レビュー2本文の「対応済みだが未対応に見える項目」を、最新状態表で整理する。

### Milestone 6 の最初に対応推奨

1. `WorldGameLoopEntryPoint` のゲーム進行順序を `AdvanceWorldFrameUseCase` / `WorldSimulationOrchestrator` へ移す。
2. 毎フレーム処理を `FrameLoop` / `ScheduleTick` / `EventDriven` に分け、装備更新・売却・回復アイテム・帰還判定を dirty / schedule 化する。
3. 戦闘イベントをトランザクション完了後に publish する設計へ寄せる。
4. View 用 DTO を値スナップショット化し、Domain への遅延参照をなくす。
5. Actor spatial index を戦闘検出 / AreaEffect / 将来 NavMesh 周辺で共通化する。
6. Factory / Request、経済 DTO、Guild / Facility Inventory、Master Repository の重複・肥大化を段階的に整理する。

## Codex対応ログ 2026-05-13

対応項目:

- 設計レビュー1: `WorldGameLoopEntryPoint` が保持していたゲーム進行パイプラインを Application 層へ移した。
- `Application/GameLoop/IWorldSimulationOrchestrator.cs` を追加し、World simulation の初期化と1フレーム進行の入口を定義した。
- `Application/GameLoop/WorldSimulationOrchestrator.cs` を追加し、従来 `WorldGameLoopEntryPoint.TickAsync()` にあった spawn / AI / lifecycle / combat / projectile / area effect / item / inn recovery / daily report の実行順序を移管した。
- `WorldGameLoopEntryPoint.cs` は `IWorldSimulationOrchestrator`、`WorldMapView`、`WorldActorPresenter`、`WorldCameraController` だけを注入し、初期化、delta time 受け渡し、キャンセル管理、camera / view 更新の呼び出しに限定した。
- `WorldLifetimeScope.cs` に `WorldSimulationOrchestrator` の DI 登録を追加した。
- `WorldGameLoopEntryPointArchitectureTests.cs` を追加し、`WorldGameLoopEntryPoint` が `DungeonInn.Application.UseCase` / `DungeonInn.Application.Orchestration` の型へ直接依存しないことを検証するアーキテクチャテストを追加した。

完了条件チェック:

- [x] `WorldGameLoopEntryPoint` が個別 UseCase / Orchestrator を直接注入していない
- [x] `WorldGameLoopEntryPoint.TickAsync()` に spawn / AI / lifecycle / combat / projectile / area effect / item / inn recovery / daily report の実行順序が残っていない
- [x] Application 層に `WorldSimulationOrchestrator` が存在し、1フレームのゲーム進行順序を所有している
- [x] `WorldGameLoopEntryPoint` は初期化、delta time 受け渡し、キャンセル管理、camera / view 更新の呼び出しだけを担当している
- [x] 追加した Application 側パイプラインの EditMode test がある
- [x] `uloop.cmd compile --project-path Client` が成功している

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（216 passed）
- 禁止API検索: 今回追加差分による新規違反なし。既存の `ProductAssetLoader.cs` の `Resources.LoadAsync` と `Launcher.cs` の `SceneManager.LoadSceneAsync` は継続検出。
- `Addressables.LoadAssetAsync`: 追加なし
- `Resources.Load` / `Resource.Load`: 追加なし
- `Task` / `ValueTask`: 追加なし
- DI登録: `WorldSimulationOrchestrator` を `IWorldSimulationOrchestrator` として `WorldLifetimeScope` に登録
- `LighthouseGenerated` 以下: 編集なし

## Codex対応ログ 2026-05-13（続き）

対応項目:

- 設計レビュー2: `WorldMapLayerViewData` が Domain オブジェクトへの遅延参照を持っていた問題を修正した。
- `WorldMapLayerViewData` から `Func<GridPosition, WorldMapCellViewKind>` を削除し、cell kind の値スナップショット配列を保持する形に変更した。
- `WorldMapViewDataProvider` は `GroundMap` / `DungeonFloor` を読み、Provider 内で cell kind 配列を構築してから View DTO を返すようにした。
- `WorldMapView` は引き続き `WorldMapLayerViewData.GetCellKind()` を呼ぶが、参照先は Domain 判定ではなく DTO 内の配列値だけになった。
- `WorldMapLayerViewDataTests.cs` を追加し、DTO が入力配列をコピーして保持すること、delegate field を保持しないことを検証する。

完了条件チェック:

- [x] `WorldMapLayerViewData` が `Func<GridPosition, WorldMapCellViewKind>` を保持していない
- [x] `WorldMapLayerViewData` または後継 DTO が cell kind / chunk view data の値スナップショットを保持している
- [x] `WorldMapView` が `GroundMap` / `DungeonFloor` / `MapLayer` の Domain 判定に到達しない
- [x] map chunk 生成が View 用 DTO の値だけで実行できる
- [x] DTO 変換処理の EditMode test がある
- [x] `uloop.cmd compile --project-path Client` が成功している

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（218 passed）
- 禁止API検索: 今回追加差分による新規違反なし。既存の `ProductAssetLoader.cs` の `Resources.LoadAsync` と `Launcher.cs` の `SceneManager.LoadSceneAsync` は継続検出。
- `Addressables.LoadAssetAsync`: 追加なし
- `Resources.Load` / `Resource.Load`: 追加なし
- `Task` / `ValueTask`: 追加なし
- DI登録: 追加なし
- `LighthouseGenerated` 以下: 編集なし

## Codex対応ログ 2026-05-13（続き2）

対応項目:

- パフォーマンスレビュー1: `WorldSimulationOrchestrator` 内で毎フレーム実行されていた schedule tick / event-driven 寄りの処理を schedule tick ブロックへ移した。
- `updateEquipmentUseCase.Execute()`、`sellItemsUseCase.Execute()`、`useRecoveryItemUseCase.ExecuteAsync()`、`decideAdventurerReturnUseCase.ExecuteAsync()` を `AdvanceFrameAsync()` 直下から削除し、`AdvanceScheduleSystemsAsync()` 内で schedule tick が進んだ時だけ実行するようにした。
- `WorldGameLoopEntryPointArchitectureTests` に、上記4処理が `AdvanceFrameAsync()` に残っておらず `AdvanceScheduleSystemsAsync()` 側に存在することを確認するアーキテクチャテストを追加した。

完了条件チェック:

- [x] ゲーム進行処理が `FrameLoop` / `ScheduleTick` / `EventDriven` に分類されている
- [x] `UpdateEquipmentUseCase.Execute()` が毎フレーム無条件で呼ばれていない
- [x] `SellItemsUseCase.Execute()` が毎フレーム無条件で呼ばれていない
- [x] `UseRecoveryItemOrchestrator.ExecuteAsync()` が毎フレーム無条件で呼ばれていない
- [x] `DecideAdventurerReturnUseCase.ExecuteAsync()` が毎フレーム無条件で呼ばれていない、または毎フレーム実行が必要な理由が明文化されている
- [x] dirty flag / schedule tick / event-driven 化した処理の回帰テストがある
- [x] `uloop.cmd compile --project-path Client` が成功している

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（219 passed）
- 禁止API検索: 今回追加差分による新規違反なし。既存の `ProductAssetLoader.cs` の `Resources.LoadAsync` と `Launcher.cs` の `SceneManager.LoadSceneAsync` は継続検出。
- `Addressables.LoadAssetAsync`: 追加なし
- `Resources.Load` / `Resource.Load`: 追加なし
- `Task` / `ValueTask`: 追加なし
- DI登録: 追加なし
- `LighthouseGenerated` 以下: 編集なし

## Codex対応ログ 2026-05-13（続き3）

対応項目:

- 設計レビュー3: 戦闘イベントが UseCase のトランザクション完了前に即時 publish される問題を修正した。
- `BufferedEventPublisher` を追加し、戦闘系 UseCase 内では `CombatAttackOccurred` / `ProjectileHit` / `AreaEffectHit` / `ActorDefeated` などを一時収集して、UseCase の最後にまとめて publish するようにした。
- `CombatDamageResolver`、`CombatDefeatResolver`、`CombatEffectExecutor`、`ActorDefeatOrchestrator`、`GrantExperienceService`、`DropItemService` に、発行先 `IEventPublisher` を明示的に受け取る経路を追加した。
- `AdvanceCombatUseCase`、`AdvanceProjectileUseCase`、`AdvanceAreaEffectUseCase` で buffered publisher を使い、ダメージ、linked effect、死亡解決、報酬、ドロップ、Actor 除去の後にイベントが外部購読者へ届くようにした。
- `AdvanceCombatUseCaseTests` に、`ActorDefeated` の publish 時点で対象 Actor が world から除去済み、combat target が解消済みであることを確認する回帰テストを追加した。

完了条件チェック:

- [x] `ProjectileHit` / `AreaEffectHit` がダメージ・linked effect 適用前に外部 publish されていない
- [x] `CombatAttackOccurred` は死亡解決・報酬・ドロップ・Actor 除去が同一 UseCase 内で完了した後に外部 publish される
- [x] 確定通知は `BufferedEventPublisher` 経由で一時収集され、UseCase 終了時に publish される
- [x] 戦闘処理中に発生したイベントを収集し、トランザクション完了後に publish する経路がある
- [x] Projectile / AreaEffect / 通常攻撃のイベント発行順を検証する EditMode test がある
- [x] `docs/design/game-event-design.md` は「UseCase 完了後にまとめてイベントを発行する」方針を既に定義しており、今回の実装はその方針に合わせた
- [x] `uloop.cmd compile --project-path Client` が成功している

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（220 passed）
- 禁止API検索: 今回追加差分による新規違反なし。既存の `ProductAssetLoader.cs` の `Resources.LoadAsync` と `Launcher.cs` の `SceneManager.LoadSceneAsync` は継続検出。
- `Addressables.LoadAssetAsync`: 追加なし
- `Resources.Load` / `Resource.Load`: 追加なし
- `Task` / `ValueTask`: 追加なし
- DI登録: 新規登録なし。既存 `IEventPublisher` 登録を既存 UseCase コンストラクタへ追加注入。
- `LighthouseGenerated` 以下: 編集なし

## Codex対応ログ 2026-05-13（続き4）

対応項目:

- その他レビュー1: `ProductAssetLoader` に残っていた `Resources.LoadAsync` の直接使用を排除した。
- `ProductAssetLoader` は ScreenStack 読み込みと TextTable 読み込みを product singleton に集約しすぎており、`IAssetScope` の寿命を機能単位に閉じる方針と合わないため削除した。
- ScreenStack 読み込みは `ScreenStackInstanceFactory` に分離し、`ScreenStackLifetimeScope` に scoped 登録した。factory が `IAssetManager.CreateScope()` で作った `IAssetScope` を保持し、ScreenStack module scope の破棄時に解放する。
- TextTable 読み込みは `ProductTextTableLoader` に分離し、`ProductLifetimeScope` では `ITextTableLoader` として登録した。TextTable は StreamingAssets 読み込みと font atlas prewarm の責務のみを持つ。
- `ProductLifetimeScope` に `LighthouseExtends.Addressable.AssetManager` を singleton 登録した。
- `DungeonInn.Runtime.asmdef` に `LighthouseExtends.Addressable.Runtime` と関連 Runtime assembly 参照を追加し、Addressable 拡張をプロジェクト Runtime assembly から利用できるようにした。
- `Launcher` の `SceneManager.LoadSceneAsync` は、Lighthouse の MainScene / ModuleScene 遷移ではなく root/bootstrap scene の再ロード用途であり、VContainer / Lighthouse の起動土台を作り直す経路のため、今回の置換対象から外して明文化された例外として追跡する。

完了条件チェック:

- [x] `ProductAssetLoader` が `Resources.LoadAsync` を直接使用していない
- [x] ScreenStack prefab 読み込みが ScreenStack module scope の `IAssetScope` 経由に乗っている
- [x] TextTable 読み込みが専用 `ITextTableLoader` 実装に分離されている
- [x] `AssetManager` が `ProductLifetimeScope` に DI 登録されている
- [x] `Launcher` の `SceneManager.LoadSceneAsync` は bootstrap / reboot 例外として理由が記録されている
- [x] `uloop.cmd compile --project-path Client` が成功している

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（220 passed）
- 禁止API検索: `Resources.Load` / `Addressables.LoadAssetAsync` のプロジェクト直接使用なし。`ProductTextTableLoader` の `UnityWebRequest.Result` / `request.result` は `.Result` 検索の false positive。`Launcher.cs` の `SceneManager.LoadSceneAsync` は bootstrap / reboot 例外として継続検出。
- `Addressables.LoadAssetAsync`: 追加なし
- `Resources.Load` / `Resource.Load`: 追加なし
- `Task` / `ValueTask`: 追加なし
- DI登録: `AssetManager` を singleton 登録
- `LighthouseGenerated` 以下: 編集なし

## Codex対応ログ 2026-05-13（続き5）

対応項目:

- 設計レビュー4: `UseCase / Service` の命名と配置が混在していた問題を修正した。
- 長期状態を持たず、Domain 変更と Event 発行を行う単発コマンドを `XxxUseCase` に統一した。
- `ActorSpawnCompletionService` は責務名を `CompleteActorSpawnUseCase` に変更した。
- `ChargeInnFeeService` / `DespawnAdventurerService` / `GrantExperienceService` / `DropItemService` は、それぞれ `ChargeInnFeeUseCase` / `DespawnAdventurerUseCase` / `GrantExperienceUseCase` / `DropItemUseCase` に変更した。
- `WorldLifetimeScope` の DI 登録、UseCase / Orchestrator の注入型、EditMode テストの生成箇所を新しい責務名へ更新した。
- `DropItemServiceTests` は `DropItemUseCaseTests` に変更した。
- `docs/guidelines/application-boundary-guidelines.md` の暫定リネーム方針を、現行の `XxxUseCase` 統一済み方針へ更新した。

完了条件チェック:

- [x] `ChargeInnFeeService` / `GrantExperienceService` / `DropItemService` / `DespawnAdventurerService` がコード上に残っていない
- [x] `CompleteActorSpawnUseCase` / `ChargeInnFeeUseCase` / `DespawnAdventurerUseCase` / `GrantExperienceUseCase` / `DropItemUseCase` が `Application/UseCase` 配下に配置されている
- [x] `WorldLifetimeScope` の DI 登録が新しい責務名に更新されている
- [x] テスト内の型名・ヘルパー名が新しい責務名に更新されている
- [x] `docs/guidelines/application-boundary-guidelines.md` が現行名に更新されている

検証:

- `rg "ActorSpawnCompletionService|ChargeInnFeeService|DespawnAdventurerService|DropItemService|GrantExperienceService" Client/Assets/DungeonInn -g "*.cs"`: 該当なし
- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（220 passed）
- 禁止 API 検索: 今回差分による新規追加なし。既存の `Launcher.cs` の bootstrap/reboot 例外、`UniTask<T>` と `UnityWebRequest.Result` の false positive のみ。
- DI 登録: `WorldLifetimeScope` に新責務名で登録済み。
- `LighthouseGenerated` 以下: 編集なし。

## Codex対応ログ 2026-05-13（続き6）

対応項目:

- 整合性レビュー2: `AdventurerBattleRecord` の設計 docs が現行実装より大きい責務を書いていた問題を docs 側で修正した。
- `docs/design/game-event-design.md` の `AdventurerBattleRecord` 説明を、シーンスコープ累積統計購読者である `AdventurerBattleRecordService` の現行責務に合わせた。
- `CombatEncounterEnded` / `ActorExitedDungeon` は現状の確定条件ではなく、表示契機として扱うことを明記した。
- 永続化や1戦闘ごとの保管ストアが必要になった場合は別責務として追加する方針を明記した。
- 整合性レビュー3: 既存レビュー本文と対応ログの状態混在に対し、本ファイル冒頭へ現在状態索引を追加した。

完了条件チェック:

- [x] `game-event-design.md` が現行の `AdventurerBattleRecordService` 責務に縮小されている
- [x] `CombatEncounterEnded` / `ActorExitedDungeon` の扱いが現行実装と矛盾しない
- [x] 後続統合レビューに対応済み / 未対応の現在状態表がある
- [x] AI 未接続など解消済み項目が未対応扱いされないことが明記されている

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）

## Codex対応ログ 2026-05-13（続き7）

対応項目:

- パフォーマンスレビュー2: 戦闘遭遇検出が毎フレーム全 Actor の spatial index を再構築していた問題を修正した。
- `ActorSpatialIndexService` を追加し、Actor の生成・削除・移動・layer 移動に対して spatial index を差分更新するようにした。
- `GameWorldState.RegisterActor` / `RemoveActor` で index 追加・削除を行うようにした。
- `MoveActorTowardDestinationUseCase`、`AdvanceActorLifecycleOrchestrator`、`AdvanceCombatUseCase` の移動経路で index 更新を行うようにした。
- `DetectCombatEncounterUseCase` は `ActorSpatialIndexService` の dirty Actor のみを処理し、全 Actor の `Rebuild(actors)` と全 Actor 検出ループを削除した。
- `CombatEncounterTargetResolver` は shared spatial index から近傍候補を取得し、Line of Sight 判定を近傍候補のみに限定した。
- `ActorSpatialIndexServiceTests` を追加し、sync・削除・Ground 移動・dirty Actor 限定検出を検証した。

完了条件チェック:

- [x] `DetectCombatEncounterUseCase` が毎フレーム全 Actor の spatial index を再構築していない
- [x] Actor 生成・移動・死亡・layer 移動時に spatial index を差分更新する経路がある
- [x] 戦闘検出の対象が dirty Actor に限定されている
- [x] Line of Sight 判定は spatial index の近傍候補に限定されている
- [x] spatial index の追加・削除・移動更新を検証する EditMode test がある

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（223 passed）
- 禁止 API 検索: 今回差分による新規追加なし。既存の `Launcher.cs` の bootstrap/reboot 例外のみ。

### セルフレビュー後の修正

- dirty Actor 限定化により、target 側が Ground / 範囲外へ移動した時に attacker 側の target が残るリスクを確認し修正した。
- dirty Actor を target にしている attacker も検出対象へ追加し、target が削除・死亡・Ground 化した場合は `ClearTargetsReferencing` と `CombatEncounterEnded` を発行するようにした。
- Ground 移動時と範囲外移動時に attacker 側 target が解除される EditMode test を追加した。

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（225 passed）

### セルフレビュー指摘の追加修正

対応項目:

- `CombatEncounterTargetResolver` の近傍 cell 半径を固定値ではなく、`GameConstants.CombatEncounterRangeMeters / GameConstants.ActorSpatialIndexCellSizeMeters` から算出するようにした。
- `ActorSpatialIndexService` に cache invalidation 用の `Revision` を追加し、Actor sync・削除で Line of Sight 成功キャッシュを invalidation できるようにした。
- `GameWorldState` の DI 用 constructor に `[Inject]` を明示した。
- `GameWorldState`、`MoveActorTowardDestinationUseCase`、`AdvanceActorLifecycleOrchestrator`、`AdvanceCombatUseCase` の production 経路では `ActorSpatialIndexService` を必須依存にし、null による silent degrade をなくした。
- テスト互換のためだけに追加していた `GameWorldState()` と `MoveActorTowardDestinationUseCase(IActorNavigationService)` を削除し、テスト側で `ActorSpatialIndexService` を明示して渡す形に修正した。
- 同一 schedule tick 内で target が遮蔽物の向こうへ移動した場合に、古い Line of Sight 成功キャッシュを再利用しない EditMode test を追加した。

検証:

- `rg "new GameWorldState\\(\\)|public GameWorldState\\(" Client/Assets/DungeonInn -g "*.cs"`: DI 用 constructor 1 件のみ
- `rg "new ActorSpatialIndexService\\(\\)" Client/Assets/DungeonInn/Runtime/Scripts -g "*.cs"`: 該当なし
- `rg "ActorSpatialIndexService actorSpatialIndexService = null|actorSpatialIndexService\\?\\.|const int NeighborCellRadius" Client/Assets/DungeonInn -g "*.cs"`: 該当なし
- 禁止 API 検索: 今回差分による新規追加なし。既存の `Launcher.cs` の bootstrap / reboot 例外、`UniTask<T>` と `UnityWebRequest.Result` の false positive のみ。
- `git diff --check`: 問題なし
- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（226 passed）
- 禁止 API 検索: 今回差分による新規追加なし。既存の `Launcher.cs` の bootstrap/reboot 例外のみ。

### セルサイズ定数の配置修正

- ガイドライン `docs/guidelines/implementation-quality-guidelines.md` の「調整可能な値はマジックナンバーにしない。定数に名前をつける」に従い、戦闘遭遇距離と spatial index セルサイズを `GameConstants.Combat.cs` へ移した。
- `GameConstants.CombatEncounterRangeMeters` を `CombatEncounterTargetResolver` の距離判定に使用する。
- `GameConstants.ActorSpatialIndexCellSizeMeters` を `ActorSpatialIndexService` の cell key 計算に使用する。
- encounter range と spatial index cell size は現時点では同じ `20f` だが、意味が異なるため別定数として分離した。

検証:

- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（225 passed）
## Codex対応ログ 2026-05-13（続き8）

対応項目:

- パフォーマンスレビュー3: 売却 / 取引経路で単一 stack 取引でも配列生成・LINQ 正規化を通っていた問題を修正した。
- `SellItemsUseCase` は Actor ごとの売却候補 `List<ItemStack>` を毎回 new せず、UseCase 内の再利用バッファを `Clear()` して使うようにした。
- `PricePolicy.CalculatePurchasePrice(ItemStack, ...)` を追加し、単一 stack 売却で `new[] { stack }` を作らない経路を追加した。
- `ExchangeExecutor.Execute(..., ItemStack, ItemStack, ...)` を追加し、単一 stack 取引では `NormalizeItems()` を通さずに検証・交換するようにした。
- `ExchangeExecutor.NormalizeItems()` は `GroupBy` / `Select` / `ToArray` の LINQ チェーンをやめ、明示ループで正規化するようにした。
- `IExchangeParticipant` に単一 `ItemStack` 用の `Has` / `CanAddAfterRemoving` / `Remove` / `Add` を追加し、Actor / Guild / Facility の実装を追加した。
- `Inventory.CanAdd(ItemStack)` と `Inventory.CanAddAfterRemoving(ItemStack, ItemStack)` は単一 stack 用の直接判定にし、単一売却経路で `new[]` と `slots.ToList()` を避けるようにした。
- `ExchangeExecutorTests` に単一 stack 取引の回帰テストを追加した。

差分許可モデル:

- `IExchangeParticipant` の単一 item API 追加は production 契約変更だが、単一 stack 売却 / 取引を高頻度経路で扱うための責務として許可する。
- Runtime 側にテスト都合だけの constructor / public method は追加していない。
- DI 管理対象 Service / UseCase / Repository の Runtime 内手動生成は追加していない。

完了条件チェック:

- [x] 売却処理が毎フレーム経路から外れている
- [x] 単一 stack 売却で `new[] { stack }` / `new[] { price }` を呼び出し側が生成していない
- [x] `ExchangeExecutor.NormalizeItems()` が LINQ `GroupBy` / `Select` / `ToArray` チェーンを使っていない
- [x] 売却候補リストは `SellItemsUseCase` の再利用バッファに限定されている
- [x] 単一 stack 取引結果を検証する EditMode test がある

検証:

- `rg "GroupBy|Select\\(|Where\\(|Sum\\(|All\\(|new\\[\\] \\{ stack \\}|new\\[\\] \\{ price \\}|new List<ItemStack>\\(\\)" Client/Assets/DungeonInn/Runtime/Scripts/Domain/Commerce Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SellItemsUseCase.cs Client/Assets/DungeonInn/Runtime/Scripts/Domain/Item/Inventory.cs -g "*.cs"`: 高頻度売却 / 取引経路の対象パターンなし
- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（227 passed）
- 禁止 API 検索: 今回差分による新規追加なし。既存の `Launcher.cs` の bootstrap / reboot 例外、`UniTask<T>` と `UnityWebRequest.Result` の false positive のみ。

## Codex対応ログ 2026-05-13（続き9）

対応項目:

- パフォーマンスレビュー4: `AttackAreaTargetResolver` が AreaEffect ごとに全 Actor を走査していた問題を修正した。
- `AttackAreaTargetResolver` に `ActorSpatialIndexService` を注入し、AreaEffect の中心と外接半径から近傍 cell 候補のみを取得するようにした。
- `AdvanceAreaEffectUseCase` は `worldState.Actors` を resolver に渡さず、resolver は spatial index の候補だけを判定する。
- Fan 判定は `Math.Sqrt` / `Math.Atan2` による角度計算をやめ、距離二乗と cos 比較で判定するようにした。
- `AdvanceAreaEffectUseCaseTests` と `AdvanceProjectileUseCaseTests` は `GameWorldState` と `AttackAreaTargetResolver` で同じ `ActorSpatialIndexService` instance を共有する形に修正した。
- `AttackAreaTargetResolver` が spatial index 候補を使うことを確認する EditMode test を追加した。

差分許可モデル:

- `AttackAreaTargetResolver` の constructor 変更は production DI 契約変更だが、AreaEffect target 解決の候補集合を spatial index に移す本番責務として許可する。
- Runtime 側にテスト都合だけの constructor / public method は追加していない。
- DI 管理対象 Service / UseCase / Repository の Runtime 内手動生成は追加していない。

完了条件チェック:

- [x] `AttackAreaTargetResolver` が AreaEffect ごとに全 Actor を無条件走査していない
- [x] AreaEffect target 候補取得が `ActorSpatialIndexService` の近傍候補に限定されている
- [x] Fan 判定で `Math.Sqrt` / `Math.Atan2` を使用していない
- [x] Actor 数と AreaEffect 数が増えても候補数ベースで処理できることがコード上確認できる
- [x] spatial index 候補利用を検証する EditMode test がある

検証:

- `rg "worldState\\.Actors|ResolveTargets\\(IGameWorldState|ResolveTargets\\(.*worldState|Math\\.Atan2|var distance =|new AttackAreaTargetResolver\\(\\)" Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AttackAreaTargetResolver.cs Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceAreaEffectUseCase.cs Client/Assets/DungeonInn/Tests -g "*.cs"`: 対象実装に該当なし
- `uloop.cmd compile --project-path Client`: 成功（ErrorCount 0 / WarningCount 0）
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 成功（228 passed）
- 禁止 API 検索: 今回差分による新規追加なし。既存の `Launcher.cs` の bootstrap / reboot 例外、`UniTask<T>` と `UnityWebRequest.Result` の false positive のみ。
