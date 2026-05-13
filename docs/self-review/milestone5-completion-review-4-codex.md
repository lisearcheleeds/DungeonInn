# Milestone 5 完了確認レビュー 4（Codex）

作成日: 2026-05-14

## レビュー範囲

`docs/guidelines/self-review-preset.md` の形式に従い、以下を確認した。

- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/roadmap/milestone5-roadmap.md`
- `docs/design/` 配下の設計資料
- `docs/self-review/` 配下の既存レビューと対応ログ
- `Client/Assets/DungeonInn/Runtime/Scripts` 配下の現行実装

今回はレビュー文書作成のみを行った。コード修正、`uloop.cmd compile --project-path Client`、EditMode test、PlayMode 確認は実施していない。

既存レビューで未対応に見える項目でも、末尾の対応ログと現行コードで解消済みと確認できたものは未解決問題として再掲しない。一方、対応ログ上は完了扱いだが現行コードに問題が残っているものは、現行コードを根拠に再掲する。

## 設計レビュー

### 1. UseCase から別 UseCase を直接呼ぶ宿回復フローが残っている

重大度: 高

問題:

`RecoverAdventurerAtInnUseCase` が `ChargeInnFeeUseCase` と `DespawnAdventurerUseCase` を constructor injection し、宿泊料徴収や退去処理を直接呼んでいる。`application-boundary-guidelines.md` は UseCase を単一トランザクション境界とし、UseCase 間の順序制御は Orchestrator に置く方針を定めているため、宿回復フローだけ境界が崩れている。

原因:

宿回復、料金徴収、満室待機、所持金不足、退去/despawn の順序制御が `RecoverAdventurerAtInnUseCase` に集約され、Orchestrator と単体 UseCase の責務分離が不完全なまま残っている。

解決案:

`AdvanceInnRecoveryOrchestrator` または `RecoverAdventurerAtInnOrchestrator` を追加し、宿回復の進行順序をそこへ移す。`RecoverAdventurerAtInnUseCase` は回復処理そのもの、`ChargeInnFeeUseCase` は料金徴収、`DespawnAdventurerUseCase` は退去処理に限定する。料金徴収や退去が宿回復内部操作であると判断する場合は、UseCase ではなく Application Service として再分類する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/RecoverAdventurerAtInnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/ChargeInnFeeUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/DespawnAdventurerUseCase.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] 非 Orchestrator の `*UseCase` が他の `*UseCase` を constructor injection していない
- [ ] 宿回復、料金徴収、退去/despawn の順序が Orchestrator または明示的な Application Service に移っている
- [ ] 料金不足、満額支払い、満室待機、退去/despawn の EditMode test がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 2. 戦闘 helper が即時 publish 経路をまだ公開している

重大度: 中

問題:

`CombatDamageResolver` と `CombatEffectExecutor` は `IEventPublisher` を保持し、buffer / collector を渡さない public overload から即時 publish できる。主要経路では `BufferedEventPublisher` に寄せられているが、API としてはトランザクション完了後 publish が保証されていない。

原因:

既存の即時 publish 設計に後から buffer overload を追加したため、古い直接 publish 経路が互換用に残っている。結果として、今後の呼び出し追加時にガイドライン違反の経路を再利用できてしまう。

解決案:

Resolver / Executor から `IEventPublisher` field と no-buffer overload を削除し、event collector / buffer を明示引数にする。あるいは Resolver / Executor は発生イベント DTO を返し、外側の UseCase / Orchestrator がトランザクション完了後に publish する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatDamageResolver.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEffectExecutor.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AdvanceCombatUseCase.cs`
- `docs/design/game-event-design.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `CombatDamageResolver` / `CombatEffectExecutor` が global event bus に直接 publish できない
- [ ] buffer / collector を通さない public overload がない
- [ ] 通常攻撃、projectile、area effect、defeat / drop / reward のイベント発行順を検証する EditMode test がある
- [ ] `docs/design/game-event-design.md` が現行のイベント発行契約と一致している
- [ ] `uloop.cmd compile --project-path Client` が成功している

再発理由:

第3回レビューで「戦闘イベントのトランザクション完了後 publish」は対応済み扱いになったが、主要経路の修正に留まり、旧 API をコード上から閉じる完了条件が不足していた。

再発防止策:

レビュー完了条件に「古い publish overload が消えていること」を含め、`rg "IEventPublisher" Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat` で直接 publish 可能な helper を確認する。

### 3. Debug Log Presenter が広い world state を View 層から直接読んでいる

重大度: 中

問題:

`WorldGameLogPresenter` が `IGameWorldStateReader` を直接注入し、Actor、Guild、施設予約、所持金などを読みながら表示用ログ文字列を組み立てている。Milestone 5 roadmap は UI 表示を専用 Query / ReadModel / DTO に寄せる方針を定めており、debug build 限定であっても View 層が広い Domain 集約に到達できる状態は境界を曖昧にする。

原因:

イベントログに必要な補助情報が event DTO または narrow query として用意されておらず、Presenter が不足情報を `IGameWorldStateReader` から直接補っている。

解決案:

Debug log 用の narrow read model / query を Application 層に置く。Presenter は `IGameWorldStateReader` ではなく、表示に必要な DTO だけを参照する。イベントに含めるべき事実情報と、Query で後引きする表示補助情報を整理する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLogPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/IGameWorldState.cs`
- `docs/roadmap/milestone5-roadmap.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `WorldGameLogPresenter` が `IGameWorldStateReader` に依存していない
- [ ] ログ表示に必要な状態は event DTO または narrow Application query / read model から取得される
- [ ] `Debug.isDebugBuild` に境界違反の許容を依存していない
- [ ] Debug log 用 query / DTO の責務が docs またはテスト名で明確になっている

## 整合性レビュー

### 1. LifetimeScope / GameLoop 設計資料が現行実装と一致していない

重大度: 中

問題:

`docs/design/lifetime-scope-game-loop-design.md` が、現行では削除済みまたは変更済みの `ProductAssetLoader` 登録や、`WorldGameLoopEntryPoint` が `IGameLoopUseCase` を直接呼ぶ構成を現在形で説明している。現行コードでは `IWorldSimulationOrchestrator` がゲーム進行を所有しているため、設計資料から実装責務を逆引きすると誤った判断になる。

原因:

Milestone 5 レビュー対応で DI とゲームループ所有者が変わったが、設計資料が同時更新されなかった。

解決案:

`lifetime-scope-game-loop-design.md` の現行登録一覧とゲームループ説明を、`ProductLifetimeScope`、`WorldLifetimeScope`、`WorldGameLoopEntryPoint`、`WorldSimulationOrchestrator` の現行構成へ更新する。履歴として古い構成を残す場合は「過去構成」または「廃止済み」と明記する。

根拠となるファイルリスト:

- `docs/design/lifetime-scope-game-loop-design.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`

完了条件:

- [ ] 設計資料が `ProductAssetLoader` を現行登録として記載していない
- [ ] 設計資料が `IWorldSimulationOrchestrator` を World simulation の所有者として説明している
- [ ] `WorldGameLoopEntryPoint` の責務が初期化、delta time 受け渡し、View 更新、キャンセル管理に限定されていることが docs とコードで一致している
- [ ] docs 更新後、既存 self-review の完了済み項目と矛盾していない

### 2. 第3回レビューの現在状態と結論が混在している

重大度: 低

問題:

`milestone5-completion-review-3-codex.md` の前半結論では `WorldGameLoopEntryPoint` から Application 層へゲーム進行を移すことが最優先と書かれているが、後続の対応ログでは `WorldSimulationOrchestrator` 追加により対応済みになっている。レビュー本文を履歴として残す運用自体は正しいが、完了判断に使う現在状態が読み取りにくい。

原因:

レビュー本文を削除せず末尾対応ログで更新する運用に対し、文書の冒頭または末尾に最終状態を再集約する仕組みが不足している。

解決案:

第3回レビューに追記するか、本レビューを正として「第3回レビュー本文の該当結論は後続ログで supersede 済み」と明記する。今後のレビュー文書には「現在状態索引」を末尾にも置き、本文より新しい対応ログを優先する読み方を固定する。

根拠となるファイルリスト:

- `docs/self-review/milestone5-completion-review-3-codex.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs`
- `docs/guidelines/self-review-preset.md`

完了条件:

- [ ] 最新の完了判断文書で、ゲーム進行パイプライン移行が対応済みとして扱われている
- [ ] 履歴本文の未対応表現が、後続対応ログで supersede 済みであることが分かる
- [ ] 未対応項目だけが `未対応 / 一部対応 / Milestone X へ延期 / 別タスク化済み` として残っている

### 3. Milestone 5 の View 挙動テスト不足が既存レビュー条件から残っている

重大度: 中

問題:

既存レビューでは `WorldMapView` の chunk 生成、`WorldActorPresenter` の Actor 生成・削除・移動・layer 切り替えに対するテストが完了条件として挙げられていた。現行 tests には architecture test や DTO / store のテストはあるが、View 挙動そのものを直接検証するテストは確認できない。

原因:

Milestone 5 の主目的である debug primitive 置き換えは実装されたが、Unity View の振る舞い確認が PlayMode / 手動確認ログに寄っており、自動テスト化が後回しになっている。

解決案:

`WorldMapView` は chunk 生成数、layer root、mesh assignment を検証する PlayMode または EditMode test を追加する。`WorldActorPresenter` は changed / removed actor の反映、layer 移動、camera yaw による rotation / flip をテストする。Milestone 6 へ延期する場合は、未対応ではなく `Milestone 6へ延期` として追跡する。

根拠となるファイルリスト:

- `docs/self-review/milestone5-completion-review-3-codex.md`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/WorldGameLoopEntryPointArchitectureTests.cs`

完了条件:

- [ ] `WorldMapView` の chunk 生成と layer root 配置を検証するテストがある
- [ ] `WorldActorPresenter` の生成、削除、移動、layer 切り替えを検証するテストがある
- [ ] camera yaw による Actor 表示更新がテストまたは PlayMode smoke で確認されている
- [ ] 延期する場合は `Milestone 6へ延期` または `別タスク化済み` として docs に記録されている

## パフォーマンスレビュー

### 1. Frame loop に Actor / Item 数比例の全走査が残っている

重大度: 高

問題:

`WorldSimulationOrchestrator.AdvanceFrameAsync()` で、AI、戦闘検出、item pickup、actor effects、宿回復が frame loop 側から呼ばれている。特に `PickUpItemUseCase` は Actor と Item の組み合わせで探索するため、Actor 数と Item 数が増えると毎フレーム負荷が急増する。

原因:

第3回レビュー対応で売却や装備更新の一部は schedule tick 側へ移ったが、frame loop / schedule tick / event-driven の分類がまだコード上の所有者とデータ構造に落ち切っていない。Item 位置の spatial index や dirty actor 集合がないため、全体走査に頼っている。

解決案:

Frame loop には移動、戦闘進行、projectile / area effect のような毎フレーム必須処理だけを残す。Item pickup は item spatial index または近傍 dirty actor 起点へ移す。Actor effects と宿回復は effect / recovery state を持つ Actor の候補集合だけを処理する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Items/PickUpItemUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdvanceActorEffectsUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/RecoverAdventurerAtInnUseCase.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `PickUpItemUseCase` が毎フレーム Actor x Item 全組み合わせを探索しない
- [ ] Actor effects は効果を持つ Actor、または dirty / scheduled 候補だけを処理している
- [ ] 宿回復は回復中 Actor の state service / queue から処理対象を取得している
- [ ] Actor / Item 数を増やした EditMode または profiler 確認で、処理量が全件組み合わせに比例しないことを確認している

### 2. Schedule tick に複数の全 Actor / Inventory 系処理が集中している

重大度: 中

問題:

`AdvanceScheduleSystemsAsync()` は同一 schedule tick で spawn、lifecycle、予約、装備更新、売却、回復アイテム、帰還判断をまとめて実行している。`SellItemsUseCase` や `UpdateEquipmentUseCase` は Actor / Inventory / Facility 数に比例するため、schedule tick 到達フレームでスパイクが起きやすい。

原因:

毎フレームから外した処理を schedule tick に寄せたが、tick 内の処理予算や候補 queue がない。Inventory 変更、施設変更、Actor lifecycle 変更といった event-driven の起点もまだ処理対象絞り込みに使われていない。

解決案:

Schedule tick 内に処理予算を設け、複数フレームに分割する。装備更新、売却、回復アイテム、帰還判断は Actor lifecycle / Inventory dirty / Facility dirty の候補 queue から処理する。売却先施設や category lookup はキャッシュする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldSimulationOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/SellItemsUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Equipment/UpdateEquipmentUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/UseRecoveryItemOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/DecideAdventurerReturnUseCase.cs`

完了条件:

- [ ] Schedule tick 処理に per-frame work budget または候補 queue がある
- [ ] 装備更新、売却、回復アイテム使用が全 Actor 無条件 scan ではなく、候補 Actor だけを処理している
- [ ] Inventory / Facility 変更時に必要な dirty flag または candidate queue が更新される
- [ ] schedule tick 大量発生時の負荷を検証する EditMode test または profiler 記録がある

### 3. Adventurer spawn の候補抽選に LINQ / 一時配列が残っている

重大度: 中

問題:

第3回レビュー対応ログでは spawn / AI 周辺の LINQ が明示ループへ置き換え済みとされているが、現行 `SpawnScheduledAdventurerOrchestrator.SelectAdventurerSpawnEntry()` には `Where(...).ToArray()` と `Sum()` が残っている。schedule tick 経路ではあるが、候補数や spawn table 数が増えた場合に不要な allocation が発生する。

原因:

Monster spawn 側は明示ループへ修正されたが、Adventurer spawn 側の類似処理が対象から漏れている。対応ログの検証検索も対象ファイルを完全に網羅していなかった可能性がある。

解決案:

`SelectAdventurerSpawnEntry()` を明示ループへ置き換え、候補抽出と weight 合計を一時配列なしで行う。spawn 抽選の共通 helper を作る場合は、allocation しない API にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledAdventurerOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledMonsterOrchestrator.cs`
- `docs/self-review/milestone5-completion-review-3-codex.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `SpawnScheduledAdventurerOrchestrator` の schedule tick 経路に `Where` / `ToArray` / `Sum` が残っていない
- [ ] SpawnOnce 除外と weight 抽選の挙動を検証する EditMode test がある
- [ ] `rg "Where\\(|ToArray\\(|Sum\\(" Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn -g "*.cs"` で hot path の対象が残っていない

### 4. Actor View の生成 / 削除が pooled ではない

重大度: 中

問題:

`WorldActorViewRegistry` は Actor spawn 時に `new GameObject` と `AddComponent<SpriteRenderer>` を行い、despawn 時に `Destroy` する。Milestone 5 の placeholder 表示としては成立するが、Actor の入退場が増えると GameObject / Component の生成破棄によるスパイクが起きる。

原因:

debug Sphere から SpriteRenderer 表示へ置き換えることを優先し、Actor View の object pooling は後回しになっている。Registry が識別子管理と生成破棄の両方を持っており、pooling の差し込み点がまだ分離されていない。

解決案:

`WorldActorViewFactory` または `WorldActorViewPool` を導入し、`WorldActorViewRegistry` は ActorId と View の対応管理に寄せる。削除時は `Destroy` ではなく非表示化して pool へ戻す。Object name に GUID を含める処理は diagnostics 限定にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorViewRegistry.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `docs/roadmap/milestone5-roadmap.md`

完了条件:

- [ ] Actor spawn / despawn が pooled view を再利用する
- [ ] `WorldActorViewRegistry` が生成破棄の詳細ではなく対応管理を主責務にしている
- [ ] Actor の大量入退場テストまたは profiler 確認で GameObject / SpriteRenderer 生成破棄スパイクがない

## 重複した機能を持つクラス / データクラスレビュー

### 1. InnEconomyStatus / InnDailyReport の正典がまだ分散している

重大度: 中

問題:

`InnEconomySummary` は追加済みだが、`InnEconomyStatus` と `InnDailyReport` は同じ経済値を proxy property と constructor 引数として保持し続けている。`InnEconomyStatusCalculator` は report を作った後に status へ詰め替えており、現在値 DTO と履歴 DTO の共通値の正典がまだ分かれたままになっている。

原因:

既存 API 互換のために alias property を残した結果、Summary 抽出後も field-by-field の詰め替えが残っている。

解決案:

`InnEconomyStatus` と `InnDailyReport` を `(Day, InnEconomySummary)` の薄い wrapper に寄せる。UI 互換用の alias property が必要な場合は ViewModel に移すか、互換 API として残す理由と削除条件を docs に明記する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatus.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/InnDailyReport.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/InnEconomyStatusCalculator.cs`
- `docs/self-review/milestone5-completion-review-3-codex.md`
- `docs/guidelines/domain-design-guidelines.md`

完了条件:

- [ ] `InnEconomyStatus` が `InnEconomySummary` を直接受け取り保持している
- [ ] `InnDailyReport` が `InnEconomySummary` を直接受け取り保持している
- [ ] `InnEconomyStatusCalculator` に report-to-status の field-by-field 詰め替えがない
- [ ] status / report の summary equivalence を検証する EditMode test がある
- [ ] 互換 property を残す場合、削除条件が docs または task に記録されている

### 2. Actor-keyed transient state holder が増え続けている

重大度: 中

問題:

`ActorDecisionScheduler`、`AdventurerExplorationStateService`、`AdventurerRecoveryStateService`、`AdventurerReturnTrackingService`、`ActorViewDataStore` など、ActorId を key にする長期状態 holder が複数存在する。死亡・帰還時 cleanup は一部で対応されたが、新しい holder を追加するたびに cleanup 対象イベントを個別に実装する必要がある。

原因:

Actor-keyed state の所有者、寿命、cleanup event の共通契約がなく、各機能が個別に Dictionary / HashSet と購読処理を持っている。

解決案:

すぐに storage を統合しない場合でも、`IActorTransientState` のような cleanup 契約、または docs 上の actor-keyed state registry 表を作る。各 holder の owner、lifetime、cleanup event、テストを一覧化し、新規追加時のチェックリストにする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Ai/ActorDecisionScheduler.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Ai/AdventurerExplorationStateService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdventurerRecoveryStateService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdventurerReturnTrackingService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `docs/self-review/milestone5-completion-review-2-total.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] Actor-keyed state holder の owner、lifetime、cleanup event が docs または review log に一覧化されている
- [ ] `ActorDecisionScheduler` に removal cleanup がある、または scene lifetime persistence として明示されている
- [ ] `ActorDefeated` / `ActorDeparted` cleanup を各 transient state holder で検証する EditMode test がある
- [ ] 新しい Actor-keyed state holder の追加時に同じ cleanup checklist を通す運用が task template または guideline にある

再発理由:

第2回レビューでも ActorDeparted cleanup 漏れとして同系統の問題が出ており、個別 holder の修正だけでは新規 holder 追加時の漏れを防げなかった。

再発防止策:

「Actor-keyed state を追加したら cleanup event と test を必ず書く」ゲートを `application-boundary-guidelines.md` または task template に追加する。

### 3. ActorProfileRegistry が表示名辞書と gameplay snapshot を兼ねている

重大度: 中

問題:

`ActorProfile` は `ActorId`、`DisplayName`、`ArchetypeId`、`SpeciesId`、`BehaviorType` を保持し、`WorldGameLogPresenter` の表示名解決と `AdventurerReturnTrackingService` の戦闘帰還判定の両方で使われている。表示用 directory と、削除済み Actor の gameplay snapshot が同じ概念に混ざっている。

原因:

イベントが ActorId 中心で発行されるため、後から表示名や species / archetype を解決する side channel として registry が導入され、そのまま gameplay lookup にも拡張された。

解決案:

責務を分ける。表示用途は `ActorDisplayNameDirectory`、削除後も必要な gameplay 情報は `ActorSpawnSnapshotStore` のように命名し、寿命と cleanup / persistence を定義する。単一 registry を維持するなら、表示用ではなく「spawn snapshot store」であることを名前と docs に反映する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Profiles/ActorProfile.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Profiles/ActorProfileRegistry.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/CompleteActorSpawnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdventurerReturnTrackingService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLogPresenter.cs`

完了条件:

- [ ] Profile registry の責務が display-only、snapshot-only、または分割済みとして明確になっている
- [ ] gameplay code が display profile に依存していない
- [ ] 削除済み Actor 情報を保持する場合、その persistence / cleanup 方針がテストされている
- [ ] 新規概念追加ゲートとして既存 Actor / archetype / behavior との差分が docs に記録されている

### 4. Spawn UseCase に DI を迂回する重複 constructor が残っている

重大度: 低

問題:

`SpawnAdventurerUseCase` と `SpawnMonsterUseCase` が、`CompleteActorSpawnUseCase` を手動 `new` する public constructor を持っている。DI 管理対象の構成を Runtime public API で複製しており、セルフレビュープリセットの差分許可モデルに反する可能性がある。

原因:

Factory / Request 統合の過程で、既存テストや互換用の簡易構築 path が Runtime 側に残った。

解決案:

Runtime public constructor は DI で使う 1 系統に統一する。テスト側で `CompleteActorSpawnUseCase` を組み立てる helper / fixture を用意し、Runtime API にテスト都合の constructor を残さない。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnAdventurerUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnMonsterUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/CompleteActorSpawnUseCase.cs`
- `docs/guidelines/self-review-preset.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `SpawnAdventurerUseCase` / `SpawnMonsterUseCase` の Runtime constructor が DI 用の 1 系統に統一されている
- [ ] Tests 側に必要最小限の fixture / helper がある
- [ ] Runtime code に DI 管理対象 UseCase を手動 `new` する composition path がない
- [ ] `uloop.cmd compile --project-path Client` と該当 spawn tests が成功している

## その他総合レビュー

### 1. SceneManager.LoadSceneAsync の bootstrap 例外がコード上で追跡されていない

重大度: 高

問題:

`Launcher` に `UnityEngine.SceneManagement.SceneManager.LoadSceneAsync` が残っている。`lighthouse-patterns.md` は bootstrap / reboot 用 Launcher を例外として扱える条件を示しているが、現行コード側には例外理由や TODO がなく、禁止 API 検索では通常違反と区別しにくい。

原因:

Lighthouse bootstrap API が利用できない時点の例外実装として残したが、例外条件と将来置き換え先がコード・task・review 上で追跡されていない。

解決案:

可能なら Lighthouse の正式な bootstrap / reboot API へ置き換える。置き換え先がない場合は、`Launcher` の該当箇所に bootstrap / reboot 例外である理由と `TODO(milestone:X)` を明記し、review / task で追跡する。禁止 API 検索時に false positive として扱える allowlist 条件を docs に残す。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs`
- `docs/guidelines/lighthouse-patterns.md`
- `docs/self-review/milestone5-completion-review-3-codex.md`

完了条件:

- [ ] `rg "SceneManager\\.LoadScene" Client/Assets/DungeonInn/Runtime/Scripts -g "*.cs"` が 0 件、または `Launcher` の documented bootstrap 例外だけを返す
- [ ] 例外として残す場合、コードに `TODO(milestone:X)` と理由がある
- [ ] 例外の追跡 task または review log がある
- [ ] `uloop.cmd compile --project-path Client` が成功している

### 2. Runtime TODO が milestone / backlog に紐付いていない

重大度: 高

問題:

Runtime に `TODO:` のまま残っているコメントがあり、spawn timing / limit、spawn point、FactionMaster、移動到着距離、hostility、MessagePipe など、実挙動や性能・設計方針に影響するものが含まれる。`implementation-quality-guidelines.md` は実挙動・性能に影響する TODO を `TODO(milestone:X):` 形式で追跡する方針を定めている。

原因:

Milestone 5 の表示置き換え作業中に master data 未整備や将来置き換え前提の値が仮実装として残ったが、TODO の分類と追跡がされていない。

解決案:

各 TODO を「現在挙動に影響する」「性能に影響する」「将来拡張」「コメントのみ」に分類する。実挙動・性能に影響するものは `TODO(milestone:X): 理由` に変更し、対応 task または backlog に紐付ける。コメントだけで済まないものは docs / roadmap に未対応項目として記録する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledMonsterOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledAdventurerOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEncounterTargetResolver.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/MoveActorTowardDestinationUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/GameEventBus.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] 実挙動・性能に影響する plain `TODO:` が Runtime に残っていない
- [ ] 残す TODO は `TODO(milestone:X):` または `TODO(backlog):` と理由を持つ
- [ ] Milestone 5 完了 notes に、次 milestone へ送る TODO が明記されている
- [ ] `rg -n "TODO:" Client/Assets/DungeonInn/Runtime/Scripts -g "*.cs"` の結果が分類済みである

### 3. 最終検証証跡が文書間に分散している

重大度: 中

問題:

Milestone 5 roadmap と既存 self-review には compile / EditMode / PlayMode の証跡が複数あるが、最新状態としてどの検証を正とするかが分散している。第3回レビューの冒頭には「レビューのみで compile / PlayMode 未実施」とも書かれており、完了判定時に直近の検証状態を誤読しやすい。

原因:

各フェーズ対応ログに検証結果を追記する運用で、最後に current head の compile / tests / PlayMode smoke をまとめる final verification block がない。

解決案:

Milestone 5 の最終レビューまたは roadmap 末尾に、current head に対する `uloop.cmd compile --project-path Client`、`uloop.cmd run-tests --project-path Client --test-mode EditMode`、PlayMode smoke の結果を 1 箇所にまとめる。PlayMode を省略する場合はユーザー承認済み waiver として明記する。

根拠となるファイルリスト:

- `docs/roadmap/milestone5-roadmap.md`
- `docs/self-review/milestone5-completion-review-3-codex.md`
- `docs/guidelines/self-review-preset.md`
- `AGENTS.md`

完了条件:

- [ ] 最新 head に対する `uloop.cmd compile --project-path Client` の結果が final verification として記録されている
- [ ] 最新 head に対する `uloop.cmd run-tests --project-path Client --test-mode EditMode` の結果が final verification として記録されている
- [ ] PlayMode smoke の結果、またはユーザー承認済み waiver が記録されている
- [ ] 既存レビュー本文の古い「未実施」記述と final verification の優先関係が分かる

### 4. QuickFirst / FirstScene の命名が揺れている

重大度: 低

問題:

`QuickFirst` フォルダ配下にあるクラスが `FirstSceneScene`、`FirstScenePresenter`、`FirstSceneLifetimeScope` といった名前になっており、フォルダ名・インターフェース名・クラス名の対応が読み取りにくい。Lighthouse の generated scene id との兼ね合いがある可能性はあるが、手書きクラス名としては責務と配置が一致していない。

原因:

初期 scene 名とプロジェクト上の表示名が変わったか、generated ID に合わせた名前とフォルダ名が混在した。

解決案:

`QuickFirst` か `FirstScene` のどちらを project-facing name とするか決め、手書きクラス / ファイル / folder を揃える。generated file を編集する必要がある場合は手動編集せず、Lighthouse の生成元設定を確認する。例外として残すなら docs に generated ID 由来の命名例外として記録する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/QuickFirst/QuickFirstScene.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/QuickFirst/QuickFirstPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/QuickFirst/QuickFirstLifetimeScope.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/LighthouseGenerated/DungeonInnMainSceneId.g.cs`
- `docs/guidelines/coding-rules.md`

完了条件:

- [ ] class / file / folder / interface の命名が一致している、または例外理由が docs にある
- [ ] LighthouseGenerated 以下を手動編集していない
- [ ] rename する場合、scene registration と DI 登録が追従している
- [ ] `uloop.cmd compile --project-path Client` が成功している

## 最終チェック

- [x] 各レビュー項目に「問題」がある
- [x] 各レビュー項目に「原因」がある
- [x] 各レビュー項目に「解決案」がある
- [x] 各レビュー項目に「根拠となるファイルリスト」がある
- [x] 各レビュー項目に「完了条件」がある
- [x] 既存レビューの対応ログを確認した
- [x] 解消済み項目を未解決として再掲していない
- [x] 未解決項目を対応済みとして扱っていない
- [x] 同じ指摘が3回以上出ている項目には再発理由と再発防止策を書いた

## 今回未実施の確認

- `uloop.cmd compile --project-path Client`
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
- PlayMode smoke

