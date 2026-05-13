# Milestone 5 完了確認レビュー 2（Codex）

作成日: 2026-05-13

## レビュー範囲

Milestone 5 完了確認として、`docs/` 配下のロードマップ、設計資料、既存 self-review、開発ガイドラインを確認したうえで、`Client/Assets/DungeonInn/Runtime/Scripts` の主要実装をレビューした。

観点は以下。

- 設計
- 整合性
- パフォーマンス
- 重複した機能を持つクラス / データクラス
- その他総合

注記:

- 今回はレビュー文書作成のみ。コード修正、`uloop.cmd compile --project-path Client`、PlayMode 確認は実施していない。
- 既存の `docs/self-review/milestone5-completion-review.md` にある指摘のうち、現在の実装で解消済みと確認できたものは再掲していない。例: World camera / layer 入力は現在 `WorldSceneInputLayer` 経由になっている。

## 結論

Milestone 5 の主目的である debug Plane / Sphere から chunk mesh / SpriteRenderer 表示への置き換えは、実装上はおおむね到達している。

ただし、Milestone 6 で NavMesh、移動品質、戦闘表示、AI 拡張を進める前に、以下は優先して整理した方がよい。

1. `WorldGameLoopEntryPoint` からゲーム進行パイプラインを Application 層へ移す。
2. AI Orchestrator が DI 登録済みだがゲームループ未接続である点を修正または設計資料へ明記する。
3. Actor 削除時の actor-keyed state cleanup を一本化する。
4. AreaEffect / commerce / map chunk 生成の高負荷経路を Milestone 6 の前提タスクにする。
5. Runtime state と master/spec 値の重複、`InnEconomyStatus` と `InnDailyReport` の重複を整理する。

## 設計レビュー

### 1. View 層がゲーム進行パイプラインを握っている

重大度: 高

問題:

`WorldGameLoopEntryPoint` が `InitializeGameWorldOrchestrator`、spawn、lifecycle、combat、projectile、area effect、item、inn recovery、daily report など多数の UseCase / Orchestrator を直接注入し、実行順序まで View 層で固定している。`Update()` 起点で `TickAsync()` を呼び、Application のシミュレーション手順が MonoBehaviour に集約されている。

原因:

Unity のフレーム更新入口と Application 層のゲーム進行責務が分離されていない。Milestone 5 は表示置き換えが主目的だったため、既存の逐次呼び出しが EntryPoint に残った。

解決案:

Application 層に `AdvanceWorldFrameUseCase` または `WorldSimulationOrchestrator` を追加し、ゲーム進行順序をそこへ移す。`WorldGameLoopEntryPoint` は初期化、delta time の受け渡し、View 更新呼び出し、キャンセル管理に限定する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs:16`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs:43`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs:142`
- `docs/guidelines/usecase-boundary-guidelines.md`

### 2. Actor 集約が可変 Inventory を直接公開している

重大度: 高

問題:

`Actor.Inventory` が可変オブジェクトとして公開され、UseCase が `Add`、`Remove`、`TrySpendGold` などを直接呼んでいる。Actor 集約を経由せずに inventory 状態を変更できるため、将来的にキャッシュ更新、イベント発行、装備との整合性維持が漏れやすい。

原因:

Equipment は読み取り専用 interface 化が進んでいる一方、Inventory 側は同じ集約境界整理が未完了。取引・回復・売却の実装が Inventory の内部 API に直接依存している。

解決案:

`IReadOnlyInventory` を導入し、Actor 外部には読み取りのみ公開する。変更は `actor.GainItem(...)`、`actor.TrySpendGold(...)`、`actor.RemoveItem(...)`、`actor.EquipFromInventory(...)` のように Actor 経由へ寄せる。`IExchangeParticipant` も `Inventory` を返すのではなく、取引操作を表すメソッドを持たせる。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs:21`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Item/Inventory.cs:72`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/UpdateEquipmentUseCase.cs:147`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/PickUpItemUseCase.cs:66`

### 3. View が広すぎる `IGameWorldStateReader` と Domain 型に依存している

重大度: 中

問題:

`IGameWorldStateReader` は `Guild`、`GroundMap`、`Dungeon`、`InnEconomy`、`Actors`、`Items`、`Projectiles`、`AreaEffects` まで公開している。View の `WorldMapView` / `WorldActorPresenter` がこの interface を直接受け取るため、表示層から Domain 全体を読める。Actor 表示でも Behavior 型を元に表示種別を解決しており、View が Domain の具体型に引っ張られている。

原因:

Read / Write 分離は行われているが、「Application 内部の読み取り」と「View 表示用の読み取り」が同じ interface に残っている。

解決案:

Application 側に `IWorldMapViewDataProvider`、`IActorViewDataProvider`、または Query UseCase を追加し、View には `ActorId`、座標、表示ロール、表示状態、必要な map cell 情報だけを渡す。Behavior 型判定は `ActorVisualRoleResolver` のような Application/View 境界の resolver に閉じ込める。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/IGameWorldState.cs:12`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs:12`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs:10`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/ActorSpriteVisualConfig.cs:59`

### 4. 状態保持 Service が `Application.UseCase` に混在している

重大度: 中

問題:

`AdventurerRecoveryStateService`、`AdventurerExplorationStateService`、`AdventurerReturnTrackingService` のように `IDisposable` とイベント購読、長期状態を持つ Service が `Application/UseCase` 名前空間に残っている。UseCase はステートレスなトランザクション単位というガイドラインと配置が一致していない。

原因:

Milestone 4/5 の設計整理で責務は Service 化されたが、フォルダ / namespace が追従していない。

解決案:

`Application/Service` または `Application/State` へ移動し、namespace も分ける。UseCase フォルダには一回の実行で Domain を変更し Event を発行するクラスだけを置く。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerRecoveryStateService.cs:10`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerExplorationStateService.cs:11`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerReturnTrackingService.cs:11`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs:50`

## 整合性レビュー

### 1. AI 実行基盤がゲームループに接続されていない

重大度: 高

問題:

`docs/design/actor-ai-desing.md` では `ActorDecisionScheduler` と `AdvanceActorAiOrchestrator` が AI 評価、dirty 管理、Decision 適用を担う設計になっている。実装でも `WorldLifetimeScope` に DI 登録されているが、`WorldGameLoopEntryPoint` の注入一覧と `TickAsync()` の実行順に `AdvanceActorAiOrchestrator` がない。

原因:

Milestone 5 の表示置き換え中に、旧来の lifecycle / usecase 直接呼び出しがゲームループに残り、AI Orchestrator は登録済み未使用の状態になっている。

解決案:

ゲーム進行パイプライン側で `AdvanceActorAiOrchestrator.ExecuteAsync(...)` を呼ぶ。現行の直接 lifecycle 制御を正とするなら、AI 設計 docs に「現在は未接続」「Milestone 6 で接続予定」などの状態を明記する。

根拠:

- `docs/design/actor-ai-desing.md:25`
- `docs/design/actor-ai-desing.md:202`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs:124`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs:43`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs:142`

### 2. モンスタースポーンが SpawnTable の重みを無視している

重大度: 高

問題:

設計では `SpawnTableMaster` がスポーン対象と重みを持つ。実装では `SpawnScheduledMonsterOrchestrator` が `spawnTable.Entries[0]` 固定で対象を選んでおり、重み付き抽選をしていない。Floor 1 の Goblin / Goblin Archer 70 / 30 のような設定が実際の出現に反映されない。

原因:

TODO の暫定実装が残っており、`SpawnTableEntryMaster.Weight` が使われていない。

解決案:

冒険者側の spawn entry 抽選と同様に、モンスター側も `Weight` に基づく抽選へ統一する。あわせて `SpawnTableTargetType.ActorArchetype` の検証を追加する。

根拠:

- `docs/design/guild-domain-design.md:420`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/SpawnScheduledMonsterOrchestrator.cs:61`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs:336`

### 3. 宿泊費不足時の状態遷移が仕様と異なる

重大度: 高

問題:

Milestone 3 仕様では「満室または残金不足の場合 `WaitingForInn` に入る」とされている。実装では満室時は `WaitingForInn` になるが、宿泊費を払えない場合は `Preparing` に戻る。

原因:

空室待ちと支払い不能が別扱いになっており、支払い不能時だけ待機 / 再試行ループから外れている。

解決案:

仕様通りにするなら、支払い不能時も `WaitingForInn` 相当へ遷移させる。現行挙動を正とするなら、仕様を「残金不足時は再準備へ戻る」に更新する。

根拠:

- `docs/roadmap/milestone3-roadmap.md:333`
- `docs/roadmap/milestone3-roadmap.md:344`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/RecoverAdventurerAtInnUseCase.cs:135`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/RecoverAdventurerAtInnUseCase.cs:137`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/ChargeInnFeeService.cs:61`

### 4. アイテム / ドロップ設計資料と実装マスタが不一致

重大度: 中

問題:

`docs/design/spec_item_money.md` の現在アイテムマスタには `Iron Sword (3004)` が見当たらない。また Goblin のドロップは Goblin Ear と Gold のみと記載されている。実装では `HardcodedMasterRepository` に `Iron Sword` があり、Goblin drop にも含まれている。

原因:

マスタ拡張後に設計資料の「現在のマスタ」表が更新されていない。もしくは実装側のドロップ追加が暫定値のまま残っている。

解決案:

`Iron Sword` とドロップ率を正式仕様にするなら docs を更新する。意図しない値なら `SpeciesMaster` の Goblin drop から `3004` を削除または確率調整する。

根拠:

- `docs/design/spec_item_money.md:100`
- `docs/design/spec_item_money.md:164`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs:122`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs:214`

### 5. AdventurerBattleRecord のイベント購読が設計より不足している

重大度: 中

問題:

イベント設計では `AdventurerBattleRecord` が `CombatEncounterStarted` / `CombatEncounterEnded` で 1 戦闘サマリーを作り、`ActorDefeated` と `ActorExitedDungeon` も扱う想定になっている。実装の `AdventurerBattleRecordService` は `CombatEncounterStarted` と `CombatAttackOccurred` だけを購読しており、終了・死亡・退出による確定処理がない。

原因:

戦闘ログの最小集計が先に実装され、設計上の「1戦闘ごとのサマリー」「退出時の保管ストア移行」が未接続になっている。

解決案:

設計通りにするなら `CombatEncounterEnded` / `ActorDefeated` / `ActorExitedDungeon` を購読してサマリー確定を実装する。現状の累積統計だけを正とするなら、docs の `AdventurerBattleRecord` 仕様を現在の責務へ縮小する。

根拠:

- `docs/design/game-event-design.md:112`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AdventurerBattleRecordService.cs:21`

## パフォーマンスレビュー

### 1. 毎フレーム処理に dirty / schedule tick で十分な処理が混在している

重大度: 高

問題:

`WorldGameLoopEntryPoint.Update()` から毎フレーム `TickAsync().Forget()` し、装備更新、売却、回復アイテム使用、帰還判定まで走る。Actor / Inventory が増えると、毎フレーム O(Actor 数 x 所持品 / 装備) の処理が積み上がる。

原因:

表示更新、リアルタイム戦闘、schedule tick、イベント / dirty 駆動で十分な処理が同じフレームループに混在している。

解決案:

Application 側に frame orchestrator を置き、毎フレーム対象を移動、戦闘、projectile、area effect などに限定する。装備更新、売却、回復アイテム使用、帰還判定は schedule tick、Actor 状態変化、Inventory 変化、dirty flag 起点に分離する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLoopEntryPoint.cs:102`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/UpdateEquipmentUseCase.cs:32`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SellItemsUseCase.cs:47`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/UseRecoveryItemOrchestrator.cs:33`

### 2. 売却 / 取引経路で毎フレーム GC Alloc が起きやすい

重大度: 中

問題:

売却経路で `new List<ItemStack>()`、単一 stack 用の `new[] { stack }`、LINQ `GroupBy` / `Select` / `ToArray`、`slots.ToList()` などが発生する。現状は `SellItemsUseCase.Execute()` が毎フレーム呼ばれるため、GC spike の原因になりうる。

原因:

取引 API が `IEnumerable` と防御的コピーを前提にしており、単一アイテム売買にも配列生成と LINQ 正規化を通している。

解決案:

まず売却処理を毎フレームから外す。加えて単一 `ItemStack` 用 overload、再利用バッファ、LINQ なしの正規化、割当なし `CanAdd` シミュレーションを用意する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/SellItemsUseCase.cs:67`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Commerce/ExchangeExecutor.cs:27`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Item/Inventory.cs:144`

### 3. AreaEffect の target 解決が Actor 全走査になっている

重大度: 中

問題:

AreaEffect ごとに全 Actor を走査し、Fan 判定では `Sqrt` と `Atan2` も使う。AreaEffect 数 x Actor 数で負荷が増える。

原因:

戦闘遭遇用の spatial index は導入されているが、AreaEffect target 解決には共通利用されていない。

解決案:

Layer / cell ベースの actor spatial index を共通化し、効果範囲周辺セルだけを候補にする。Fan 判定は dot / cross と距離二乗で行い、三角関数を避ける。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AttackAreaTargetResolver.cs:25`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceAreaEffectUseCase.cs:36`

### 4. Map chunk mesh 生成が同期スパイクになりうる

重大度: 中

問題:

`WorldMapView.UpdateVisuals()` が毎フレーム Dungeon floors をポーリングし、未構築 layer を見つけると全 chunk を同期生成する。chunk ごとに `List`、`Dictionary`、`Mesh`、material 配列を作り、`RecalculateNormals()` / `RecalculateBounds()` も走る。

原因:

floor 追加イベントや chunk build queue がなく、メッシュ生成バッファも使い捨てになっている。

解決案:

floor 追加時に生成 queue へ積み、1 frame の chunk 生成数を制限する。頂点 / UV / triangle バッファを再利用し、平面 mesh なら法線・bounds を明示して再計算を避ける。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs:30`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs:97`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs:35`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/MapMeshBuildService.cs:70`

### 5. WorldGameLogPresenter が常時大量購読・大量ログを行う

重大度: 中

問題:

`GameEventBus.OnEvent<T>()` は共有 `Subject` に対して `Where + Select` を作る。`WorldGameLogPresenter` は多数のイベント型を常時購読し、イベントごとに型判定が多重に走る。combat 系では文字列補間と `Debug.Log` も高頻度に発生する。

原因:

debug / log 表示が通常 World scene の entry point として常時有効で、イベント種別ごとの fanout が重複している。

解決案:

log presenter を debug flag または development build 限定にする。イベント配送は型別 channel / MessagePipe / 辞書 fanout に寄せる。ログは必要ならバッチ化・レート制限する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/GameEventBus.cs:27`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldGameLogPresenter.cs:35`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs:38`

## 重複した機能を持つクラス / データクラスレビュー

### 1. `InnEconomyStatus` と `InnDailyReport` のフィールドが重複している

重大度: 中

問題:

`InnEconomyStatus` と `InnDailyReport` が近いフィールド列を別型で持ち、`InnEconomyStatusCalculator` が report から status へ詰め替えている。現在状態と日次レポートの区別は必要だが、共通部分の正典が分かれやすい。

原因:

UI 用 DTO と履歴用 report を型として分けた一方、データ構造はほぼ同じまま複製されている。

解決案:

`InnEconomyStatus` が `InnDailyReport CurrentReport` を内包する、または共通の `InnEconomySummary` 値型を切り出す。UI 専用に追加が必要な値だけ status 側へ置く。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatus.cs:3`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Guild/InnDailyReport.cs:3`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/InnEconomyStatusCalculator.cs:14`

### 2. `ActiveStatusEffect` が master/spec 由来の不変値を runtime state にコピーしている

重大度: 中

問題:

`ActiveStatusEffect` が `StatusEffectSpec` の `Type`、`AggregationPolicy`、`TickIntervalSeconds`、`Amount`、`DurationSeconds` をコピーしている。`ActorEffectInstance` も master duration を runtime object にコピーする。Runtime state と master/spec 由来の不変値が同じクラスに混在している。

原因:

状態異常の「現在の進行状態」と「仕様定義」を早く使いやすくするために同一 object へ畳んでいる。

解決案:

`ActiveStatusEffectState` は elapsed、applied、累積値など可変値だけを持つ。不変値は `StatusEffectSpec` 参照、または spec id / index から resolver で解決する。再付与時に変わる値だけ runtime 側へ明示的に持たせる。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/ActiveStatusEffect.cs:8`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/StatusEffectSpec.cs:8`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/ActorEffectInstance.cs:13`

### 3. `ChargeInnFeeService` / `DespawnAdventurerService` などが UseCase 的な実行単位と Service 命名の中間になっている

重大度: 低

問題:

`ChargeInnFeeService`、`DespawnAdventurerService`、`GrantExperienceService`、`DropItemService` は長期状態を持つ Service ではなく、Domain を変更して Event を発行する実行単位に近い。一方で UseCase から呼ばれており、UseCase 間呼び出し禁止を避けるために Service 命名へ逃げているようにも見える。

原因:

UseCase / Service / Orchestrator の境界整理の途中で、命名と配置が責務に追従していない。

解決案:

「状態を持つ Service」と「一回の処理単位」を明確に分ける。これらを Service として残すなら、Domain service としての責務を明文化し `Application/Service` へ移す。UseCase とするなら Orchestrator から呼ぶ構成へ寄せ、UseCase 同士の直接依存を避ける。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/ChargeInnFeeService.cs:12`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/RecoverAdventurerAtInnUseCase.cs:19`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/RecoverAdventurerAtInnUseCase.cs:20`

## その他総合レビュー

### 1. Lighthouse 禁止 API が基盤経路に残っている

重大度: 重大

問題:

`Resources.LoadAsync` と `SceneManager.LoadSceneAsync` / `UnloadSceneAsync` の直接利用が残っている。今回の Milestone 5 作業で追加されたものではないが、プロジェクトのハードゲートとしては例外扱いが必要。

原因:

ScreenStack 生成と reboot / launcher 経路が Lighthouse 移行前の暫定実装として残っている。

解決案:

ScreenStack prefab 読み込みは `IAssetManager` / `IAssetScope` 経由へ移す。Launcher の reboot / unload は Lighthouse の scene transition / product scene manager 側に寄せ、直接 `SceneManager` を呼ばない。すぐ直さない場合は「既存違反・今回追加なし」としてタスク化する。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/AssetLoader/ProductAssetLoader.cs:32`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs:29`
- `Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs:61`
- `docs/guidelines/lighthouse-patterns.md`

### 2. Actor 削除後に actor-keyed state が残る可能性がある

重大度: 高

問題:

Actor 削除後も navigation path state、回復累積値、探索目的地などが残る可能性がある。特に `ActorDeparted` では recovery / exploration 側の cleanup 購読が見当たらない。

原因:

Actor に紐づく状態が複数 Service に分散しており、`GameWorldState.RemoveActor()` と連動する統一 cleanup 契約がない。死亡イベントだけを購読している Service と、despawn / depart でも掃除が必要な Service が混在している。

解決案:

`ActorRemoved` / `ActorDespawned` のような削除イベントを一本化し、actor-keyed state service は必ずそれを購読して削除する。`ActorNavigationService` にも `RemovePathState(actorId)` を追加し、Actor 削除 Orchestrator から確実に呼ぶ。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/ActorNavigationService.cs:10`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GameWorldState.cs:68`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DespawnAdventurerService.cs:42`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerRecoveryStateService.cs:12`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdventurerExplorationStateService.cs:13`

### 3. 戦闘イベントがトランザクション完了前に発行される経路がある

重大度: 中

問題:

`CombatAttackOccurred` や `ProjectileHit` が、死亡処理や linked damage の完了前に publish される経路がある。購読者が状態変更しない前提でも、「事後通知」としての一貫性が弱い。

原因:

Effect 実行中に Resolver / Executor がその場で event を publish している。

解決案:

戦闘 UseCase 内で発生イベントを一旦収集し、ダメージ適用、死亡解決、報酬、ドロップ、Actor 削除が完了した後にまとめて publish する。少なくとも `ProjectileHit` / `AreaEffectHit` は実際の効果適用後の通知に寄せる。

根拠:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatDamageResolver.cs:52`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatDamageResolver.cs:59`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceCombatUseCase.cs:87`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEffectExecutor.cs:90`

### 4. Milestone 5 の検証証跡がレビュー 2 時点では不足している

重大度: 中

問題:

今回のレビューではコードと docs の確認は行ったが、`uloop.cmd compile --project-path Client`、EditMode tests、PlayMode smoke test は実行していない。Milestone 5 完了条件には compile / PlayMode 確認が含まれるため、レビュー文書だけでは完了判定の証跡として不足する。

原因:

依頼範囲が「プロジェクト全体レビューと md 記載」であり、動作確認までは今回の作業に含めていない。

解決案:

別途、Claude Code または Codex の完了確認として `uloop.cmd compile --project-path Client`、EditMode tests、PlayMode smoke test を実行し、結果を本レビューまたは Milestone 5 completion review に追記する。

根拠:

- `docs/roadmap/milestone5-roadmap.md`
- `AGENTS.md` の Codex 完了前チェック

## 推奨対応順

### Milestone 5 完了前に確認推奨

1. `uloop.cmd compile --project-path Client` と PlayMode smoke test の証跡を残す。
2. AI Orchestrator 未接続を仕様として許容するか、ゲームループへ接続するかを決める。
3. 宿泊費不足時の `WaitingForInn` / `Preparing` のどちらを正とするかを決める。
4. 既存禁止 API を「既存違反・別タスク」として明示的に追跡する。

### Milestone 6 の最初に対応推奨

1. `WorldGameLoopEntryPoint` のゲーム進行順序を Application Orchestrator へ移す。
2. Actor 削除イベントと actor-keyed state cleanup を一本化する。
3. AreaEffect target resolver と map chunk 生成のパフォーマンス対策を入れる。
4. `IGameWorldStateReader` から View 専用 Query / DTO を分離する。
5. `InnEconomyStatus` / `InnDailyReport` と status effect runtime/spec の重複整理を行う。

