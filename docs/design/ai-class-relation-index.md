# AI Class Relation Index

このドキュメントは、AI が DungeonInn の全体像を検索なしで把握するための入口である。
目的は、全クラスを網羅することではなく、「どの機能を見るときに、どのクラスから読み始めるべきか」を短時間で判断できるようにすること。

## この考え方の有効性

このドキュメントは意味がある。
特に AI は、最初に検索範囲を誤ると、局所的なクラスだけを見て責務境界や依存方向を誤解しやすい。
そのため、検索前に読む「機能別の地図」があると、調査の入口、主要な状態、責務の分担、関連ドキュメントを素早く揃えられる。

ただし、詳細な依存図や全クラス一覧として運用するとすぐ古くなる。
このドキュメントは以下に限定する。

- 機能ごとの入口クラス
- 状態を持つクラス
- 主要な UseCase / Service / Presenter
- 参照すべき design / guideline
- 検索時の最初のキーワード

詳細なメソッド一覧、全コンストラクタ、全イベント購読関係は書かない。

---

## 読み方

AI が調査・実装を始めるときは、以下の順で読む。

1. このファイルで対象機能の入口を確認する
2. 関連 design / guideline を読む
3. 表にある「最初に読むクラス」からコードを読む
4. 必要になってから `rg` で周辺クラスを検索する

---

## レイヤー構成

| レイヤー | 役割 | 主な場所 |
|---|---|---|
| Domain | Entity、値、状態遷移、計算式、ゲーム固有の不変ルール | `Runtime/Scripts/Domain` |
| Application | UseCase、Orchestrator、Service、GameWorldState、イベント、ゲームループ | `Runtime/Scripts/Application` |
| Master | 仮のマスタデータ、コンテンツ定義、Prefab address、ID 解決 | `Runtime/Scripts/Master` |
| View | Unity scene、Presenter、View、Pool、Factory、Camera、Map mesh | `Runtime/Scripts/View` |
| Input | InputLayer、入力処理 | `Runtime/Scripts/Input` |
| Core / Infrastructure | Product lifecycle、scene management、asset loading 基盤 | `Runtime/Scripts/Core`, `Runtime/Scripts/Infrastructure` |

---

## 機能別インデックス

| 機能 | 最初に読むクラス | 状態・所有者 | 関連クラス | 関連 docs |
|---|---|---|---|---|
| Product / Scene 起動 | `ProductEntryPoint`, `ProductLifetimeScope` | Product lifetime | `RootEntryPoint`, `SceneGroupProvider`, `ProductSceneManager`, `GameSessionLifecycle` | `lifetime-scope-game-loop-design.md` |
| Game session DI | `GameSessionLifetimeScope` | Game session lifetime | `GameWorldState`, `GameClock`, `WorldSimulationOrchestrator`, Application services | `lifetime-scope-game-loop-design.md`, `refactoring-guidelines.md` |
| World scene DI | `WorldLifetimeScope` | World scene lifetime | `WorldScene`, `WorldGameLoopEntryPoint`, scene-owned View adapters | `lifetime-scope-game-loop-design.md`, `refactoring-guidelines.md` |
| Game loop | `WorldSimulationOrchestrator` | `GameClock`, `GameWorldState` | `GameLoopUseCase`, `WorldFrameAdvanceRequest`, `WorldGameLoopEntryPoint` | `lifetime-scope-game-loop-design.md`, `application-boundary-guidelines.md` |
| World state | `GameWorldState` | `GameWorldState` | `IGameWorldState`, `IGameWorldStateReader`, `ActorViewDataStore` | `lifetime-scope-game-loop-design.md` |
| Map / Ground | `InitializeWorldMapUseCase` | `GroundMap` | `MapLayer`, `GroundCell`, `GroundMapGenerationSettings` | `map-dungeon-domain-design.md` |
| Dungeon generation | `GenerateDungeonFloorUseCase` | `Dungeon`, `DungeonFloor` | `InitializeDungeonOrchestrator`, `EnsureDungeonFloorGeneratedOrchestrator`, `UseDungeonStairOrchestrator` | `map-dungeon-domain-design.md` |
| Actor spawn | `SpawnScheduledAdventurerOrchestrator`, `SpawnScheduledMonsterOrchestrator` | `SpawnScheduleState`, `GameWorldState` | `SpawnAdventurerUseCase`, `SpawnMonsterUseCase`, `ActorFactory` | `actor-master-design.md` |
| Actor AI | `AdvanceActorAiOrchestrator` | AI policy / behavior | `AdventurerAiPolicy`, `MonsterAiPolicy`, `ActorDecisionScheduler` | `actor-ai-desing.md` |
| Actor lifecycle | `AdvanceActorLifecycleOrchestrator` | `AdventurerBehavior`, candidate services | `UseRecoveryItemOrchestrator`, `DecideAdventurerReturnUseCase`, `AdvanceInnRecoveryOrchestrator`, `SelectAdventureGoalUseCase`, `AdventureGoalProgressService`, `AdventurerDeathRevivalService` | `actor-ai-desing.md`, `application-boundary-guidelines.md` |
| Actor movement | `ActorMovementService` | `ActorNavigationService` | `MoveActorTowardDestinationUseCase`, `INavigationPathProvider`, `UnityNavMeshPathProvider` | `map-dungeon-domain-design.md` |
| Combat detection | `DetectCombatEncounterUseCase` | `ActorCombatService`, spatial index | `CombatEncounterTargetResolver`, `ActorSpatialIndexService` | `combat-domain-design.md` |
| Combat advance | `AdvanceCombatUseCase` | `ActorCombatService` | `DirectWeaponCombatCalculator`, `CombatEffectExecutor`, `ActorDefeatOrchestrator` | `combat-domain-design.md` |
| Projectile / AreaEffect | `AdvanceProjectileUseCase`, `AdvanceAreaEffectUseCase` | `GameWorldState` projectile / area effect lists | `ProjectileInstance`, `AreaEffectInstance`, `WorldProjectilePresenter`, `WorldAreaEffectPresenter` | `combat-domain-design.md`, `refactoring-guidelines.md` |
| Item pickup / facility trade | `PickUpItemUseCase`, `FacilityInteractionOrchestrator` | `ItemSpatialIndexService`, inventories, facilities | `ItemInstance`, `Inventory`, `IItemStackLimitResolver` | `spec_item_money.md` |
| Guild / Economy | `ChargeInnFeeUseCase`, `PublishInnDailyReportUseCase` | `AdventurerGuild`, `InnEconomyState`, `InnDailyReportStore` | `InnEconomyStatusCalculator`, `InnEconomyStatisticsService` | `guild-domain-design.md`, `spec_item_money.md` |
| Master data | `HardcodedMasterRepository` | Master repository | `WeaponTypeCombatMasterCatalog`, `ActorArchetypeMaster`, `EnvironmentPropVisualMaster` | `actor-master-design.md`, `combat-domain-design.md` |
| Event system | `IGameEventBus`, `GameEventBus` | Event stream / history | `IEventPublisher`, `IEventSubscriber`, `PlayerEventLogStore` | `game-event-design.md` |
| World map view | `WorldMapView` | Map layer view registry / generated meshes | `MapMeshBuildService`, `MapLayerViewRegistry`, `EnvironmentObjectPlacer`, `NavMeshBuildService` | `map-dungeon-domain-design.md` |
| Actor world view | `WorldActorPresenter` | Actor view pool / view data | `ActorView`, `ActorSpriteAnimator`, `ActorSpriteVisualConfig`, `ActorCombatAnimationPresenter` | `actor-visual-size-tier-design.md` |
| Camera | `WorldCameraController` | Camera settings | `WorldActorCameraFollowController`, `WorldCameraSettingsSO` | `lifetime-scope-game-loop-design.md` |
| HUD / Popup | `WorldActorStatusPresenter`, `ActorDetailPopupPresenter` | UI ModuleScene / pools | `WorldHudCanvasProvider`, `ActorHUDViewPool`, `ActorDetailPopup` | `lighthouse-patterns.md`, `refactoring-guidelines.md` |
| Addressable view creation | `WorldAddressableViewFactory` | `IAssetScope` lifetime | `ProjectilePrefabSource`, `AreaEffectPrefabSource`, `VisualConfigLoader` | `lighthouse-patterns.md`, `refactoring-guidelines.md` |
| Input | `WorldSceneInputLayer` | Input layer lifecycle | `WorldActorSelectionInputHandler`, generated `InputActions` | `lighthouse-patterns.md` |
| Settings / Config | `WorldGameSettingsSO` | SO -> runtime settings | `WorldGameSettings`, `WorldMapViewSettings`, `WorldCameraSettingsSO` | `refactoring-guidelines.md` |

---

## 主要な依存関係

### World simulation

```text
WorldGameLoopEntryPoint
  -> WorldSimulationOrchestrator
      -> GameLoopUseCase / GameClock
      -> InitializeGameWorldOrchestrator
      -> SpawnScheduledAdventurerOrchestrator / SpawnScheduledMonsterOrchestrator
      -> AdvanceActorAiOrchestrator
      -> AdvanceActorLifecycleOrchestrator
      -> DetectCombatEncounterUseCase
      -> AdvanceCombatUseCase
      -> AdvanceProjectileUseCase / AdvanceAreaEffectUseCase
      -> PickUpItemUseCase
      -> AdvanceGroundFacilityTaskOrchestrator / FacilityInteractionOrchestrator
      -> AdvanceInnRecoveryOrchestrator
      -> UseRecoveryItemOrchestrator / DecideAdventurerReturnUseCase
```

`WorldSimulationOrchestrator` はゲーム進行の順序制御を持つ。
Domain の判定式や個別ビジネスルールを直接肥大化させず、必要な UseCase / Service へ委譲する。
Ground に戻った Adventurer の準備処理は、装備更新、アイテム売却、宿屋予約・宿代支払い・HP 回復、回復アイテム購入、出発判定の順に固定する。

### Adventure goal

現行実装では、冒険目的の正典を `ActorGoal` に統一している。
探索専用の重複 DTO / enum である `DungeonExplorationGoal` / `DungeonExplorationGoalType` は使わない。

移行後の入口は以下とする。

- `SelectAdventureGoalUseCase`: Actor / Guild / Dungeon / 直近履歴から、重み付きで `ActorGoal` を選ぶ
- `AdventureGoalProgressService`: `ReachFloor`, `LevelUp`, `DefeatMonster`, `EarnMoney`, `CollectItem` の進捗を計算する
- `DecideAdventurerReturnUseCase`: 目的達成、HP 不足、回復アイテム不足などから帰還判断だけを行う
- `AdvanceActorLifecycleOrchestrator`: 選択された `ActorGoal` を Actor に適用し、冒険開始状態へ進める

`EarnMoney` は `LevelUp` と同じ探索方針を使うが、帰還条件は「今回の冒険で得た売却可能アイテムの見込み売却額」とする。
素材集めは独立した `CollectMaterial` ではなく、具体的な素材 item id を対象にした `CollectItem` として扱う。

### World state

```text
GameWorldState
  -> AdventurerGuild
  -> GroundMap
  -> Dungeon
  -> Actors
  -> Items
  -> Projectiles
  -> AreaEffects
  -> InnEconomyState
  -> SpawnScheduleState
```

`GameWorldState` は Domain Entity ではなく、Application runtime の集約状態。
Entity の不変条件は各 Domain class が守る。

### Master and content

```text
HardcodedMasterRepository
  -> Actor / Item / Equipment / Weapon / Dungeon / Environment Prop masters
  -> UseCase / Factory / Calculator
```

Prefab address や visual id は、発生元の Master / Spec が持つ。
World scene や LifetimeScope にコンテンツ Prefab 一覧を集約しない。

### View

```text
WorldScene
  -> WorldMapView
  -> WorldActorPresenter
  -> WorldProjectilePresenter
  -> WorldAreaEffectPresenter
  -> WorldActorStatusPresenter
  -> ActorDetailPopupPresenter
  -> WorldCameraController
```

View は Application の DTO / DataProvider / Query を読む。
Domain Entity や View 実体を広く直接 Inject しない。

---

## 検索の入口

目的別の最初の検索語:

| 目的 | 検索語 |
|---|---|
| ゲーム進行順を見たい | `WorldSimulationOrchestrator` |
| DI 登録を見たい | `ProductLifetimeScope`, `GameSessionLifetimeScope`, `WorldLifetimeScope` |
| Actor の移動を見たい | `ActorMovementService`, `AdvanceActorLifecycleOrchestrator` |
| AI 判断を見たい | `AdvanceActorAiOrchestrator`, `AiDecision` |
| 冒険目的を見たい | `SelectAdventureGoalUseCase`, `ActorGoal`, `AdventureGoalProgressService`, `DecideAdventurerReturnUseCase` |
| 戦闘を見たい | `AdvanceCombatUseCase`, `CombatEffectExecutor` |
| Projectile / AreaEffect 表示を見たい | `WorldProjectilePresenter`, `WorldAreaEffectPresenter` |
| HUD / Popup を見たい | `WorldActorStatusPresenter`, `ActorDetailPopupPresenter` |
| マップ生成を見たい | `InitializeWorldMapUseCase`, `GenerateDungeonFloorUseCase` |
| NavMesh / A* のズレを見たい | `UnityNavMeshPathProvider`, `ActorNavigationService` |
| 設定値の置き場所を見たい | `WorldGameSettingsSO`, `WorldGameSettings` |
| Addressable 経路を見たい | `WorldAddressableViewFactory`, `IAssetScope` |

---

## 更新ルール

このドキュメントは、以下の場合に更新する。

- 新しい大きな機能領域を追加した
- 主要な入口クラスが変わった
- 状態の所有者が変わった
- LifetimeScope / GameLoop / Master / View Presenter の責務が変わった
- フォルダ分割により探索入口が変わった

更新しなくてよいもの:

- private helper method の追加
- 小さな DTO の追加
- 既存機能内の局所的な UseCase 追加
- テスト helper の追加

---

## 注意

このドキュメントは正本ではない。
仕様の正本は `docs/spec*.md`、設計の詳細は各 `docs/design/*.md`、実装ルールは `docs/guidelines/*.md`、現在の挙動はコードで確認する。

このファイルとコードが食い違っている場合は、コードを確認した上でこのファイルを更新する。
