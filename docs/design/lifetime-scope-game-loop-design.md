# LifetimeScope / Game Loop DI Design

このドキュメントは、DungeonInn の VContainer / Lighthouse における LifetimeScope 配置、Application 層サービスの登録方針、ゲームループの時間管理方針をまとめる。

## LifetimeScope 階層

現在の親子関係は以下。

```text
RootLifetimeScope
└── ProductLifetimeScope
    ├── GameSessionLifetimeScope
    │   ├── MainScene LifetimeScope
    │   └── ModuleScene LifetimeScope
    ├── MainScene LifetimeScope
    └── ModuleScene LifetimeScope
```

- `RootLifetimeScope` は `RootEntryPoint` で `ProductLifetimeScope` prefab を生成する。
- `ProductLifetimeScope` は `DontDestroyOnLoad` でアプリ起動中は生存する。
- `GameSessionLifetimeScope` は NewGame / Continue / Load で作られる 1 ラン用の子 Scope。ゲームラン状態と Application service はここに置く。
- `ProductEntryPoint` は MainScene / ModuleScene の親 LifetimeScope を設定する。ゲーム中の MainScene / ModuleScene は `GameSessionLifetimeScope` 配下から同じゲームラン状態を参照する。
- `View/Base` は Lighthouse の Scene / Dialog 基底ラッパー置き場であり、LifetimeScope 実体をまとめる場所ではない。
- LifetimeScope 実体は `Core`、`GameSession`、または各 `View/Scene/...` に置く。

## 登録方針

Application 層そのものが LifetimeScope を持つわけではない。Application のサービスをどの LifetimeScope に登録するかは、サービスの寿命で決める。

- アプリ全体で共有し、タイトル画面でも存在してよいものは `ProductLifetimeScope` に登録する。
- ゲームプレイ中の1ランだけ存在する状態や進行サービスは、`GameSessionLifetimeScope` に登録する。
- Scene / Presenter / Scene固有EntryPointは各 MainScene / ModuleScene の LifetimeScope に登録する。
- UseCase / Factory / Repository は static class にしない。状態を持たないものでも DI 可能なインスタンスサービスとして扱う。

## ProductLifetimeScope に置くもの

`ProductLifetimeScope` はゲーム全体で共有されるアプリ基盤を持つ。

現在の登録対象:

- Lighthouse / LighthouseExtends 基盤
  - `SceneManager`
  - `ProductSceneManager`
  - `MainSceneManager`
  - `ModuleSceneManager`
  - `SceneCameraManager`
  - `ExclusiveInputService`
  - `InputLayerController`
  - `LanguageService`
  - `TextTableService`
  - `FontService`
  - `ScreenStackModuleProxy`
- Product 共通サービス
  - `AssetManager`
  - `JsonGameSaveRepository`
  - `ProductTextTableLoader`
  - `HardcodedMasterRepository` as `IMasterRepository`
  - `ActorFactory` as `IActorFactory`
  - `ScoutCostPolicy`
- ゲームセッション開始前に必要なサービス
  - `GameSessionStartRequestStore`
  - `GameSessionStartCoordinator`
  - `GameSessionLifecycle`
  - Save / Load 用 UseCase

`IMasterRepository` は将来 MasterMemory に置き換える想定だが、依存側は `IMasterRepository` のままにする。

## GameSessionLifetimeScope に置くもの

`GameSessionLifetimeScope` には、NewGame / Continue / Load で開始した 1 ランの寿命に閉じるものを登録する。

- ゲームラン状態
  - `GameWorldState` as `IGameWorldState`
  - `GameClock` as `IGameClock`
  - `GameRandom`
  - `GameWorldFrameBuffer`
  - `SpawnScheduleState` は `GameWorldState` が保持する
- ゲームループ / 初期化
  - `GameLoopUseCase` as `IGameLoopUseCase`
  - `WorldSimulationOrchestrator` as `IWorldSimulationOrchestrator`
  - `InitializeGameWorldOrchestrator`
  - `InitializeWorldMapUseCase`
  - `InitializeDungeonOrchestrator`
  - `EnsureDungeonFloorGeneratedOrchestrator`
- Application service / UseCase
  - スポーン、AI、移動、戦闘、アイテム、経済、宿屋、帰還判定、セーブ復元に関わる UseCase / Service
  - `SpawnAdventurerUseCase`
  - `SpawnMonsterUseCase`
  - `SelectAdventureGoalUseCase`
  - `AdventureGoalProgressService`
  - `AdventurerDeathRevivalService`
- View 子 Scope と共有する抽象
  - `IWorldMapViewDataProvider`
  - `IActorSelectionReader`
  - `IActorScreenPositionProvider` proxy
  - `IActiveLayerProvider` proxy
  - `INavigationPathProvider` proxy

これらはタイトル画面では生成しない。ゲームセッション終了、リブート、ロード切り替えで `GameSessionLifetimeScope` ごと破棄する。

## World LifetimeScope に置くもの

現在のインゲーム MainScene は `World`。

`WorldLifetimeScope` には、Worldシーンの寿命に閉じる View / scene-owned adapter を登録する。

- Scene固有
  - `WorldScene`
  - `WorldPresenter`
  - `WorldGameLoopEntryPoint`
  - `WorldViewRoot`
- World View / Adapter
  - `WorldMapView`
  - `WorldActorPresenter`
  - `WorldProjectilePresenter`
  - `WorldAreaEffectPresenter`
  - `WorldActorScreenPositionProvider`
  - `WorldActiveLayerProvider`
  - `UnityNavMeshPathProvider`
  - `WorldCameraController`

`WorldLifetimeScope` はゲームラン状態を所有しない。Worldシーンが閉じられても、同じゲームセッション内で別 ModuleScene / MainScene が必要とする Application state は `GameSessionLifetimeScope` 側に残る。

## GameWorldState

`GameWorldState` は Domain Entity ではなく、Application / Runtime の集約状態として扱う。

現在の責務:

- `AdventurerGuild` の保持
- `GroundMap` の保持
- `Dungeon` の保持
- Actor一覧の登録 / 削除
- ゲームラン初期化済みかどうかの保持

Domain の不変条件は各 Entity が守る。`GameWorldState` はそれらを保持・検索するためのランタイム状態であり、Domainルールを肥大化させない。

## 時間管理

Unity の `Time.timeScale` は使わない。

理由:

- UI Animator
- Lighthouse の遷移演出
- Input / UI 周辺処理
- Editor / View都合の演出

これらまで倍速の影響を受けるため。

ゲーム進行専用の時間は `GameClock` が持つ。

- `ElapsedRealTimeSeconds`: `Time.unscaledDeltaTime` をそのまま積算した現実時間
- `ElapsedGameTimeSeconds`: `ElapsedRealTimeSeconds` にゲーム進行倍率を掛けたゲーム進行時間
- `TimeScale`: ゲーム進行専用の倍率
- `CurrentScheduleTick`: 低頻度スケジュール用の整数秒カウンタ
- `CurrentDay`: ゲーム内日付

`WorldGameLoopEntryPoint` は View 側なので `Time.unscaledDeltaTime` を読んでよい。Application 層には `float unscaledDeltaTimeSeconds` として渡す。

```csharp
await gameLoopUseCase.ExecuteAsync(new GameLoopTickRequest(Time.unscaledDeltaTime));
```

倍速変更は `SetGameTimeScaleUseCase` を通す。

```csharp
await setGameTimeScaleUseCase.ExecuteAsync(2f);
```

## CurrentScheduleTick の用途

`CurrentScheduleTick` は連続動作の表現単位ではない。

用途はリアルタイム性が低い定期処理に限定する。

- 日付変更チェック
- 冒険者スポーン抽選
- ランダムイベント抽選
- 1秒ごとのログ / 集計
- 低頻度の経営シミュレーション処理

以下には使わない。

- Actorの移動補間
- NavMesh移動
- 攻撃間隔
- Projectile移動
- バフ残り時間の精密管理
- AI再判定クールダウン

これらは現実時間ベース、またはゲーム進行時間ベースの `float seconds` で扱う。

## Game Loop

`GameLoopUseCase` は、低頻度スケジュールを進めるApplication UseCase。

現在の実装では以下を行う。

1. `GameClock.Advance(unscaledDeltaTimeSeconds)` を呼ぶ
2. 進んだ `CurrentScheduleTick` 数を返す
3. 日付変更有無を返す

AI評価、スポーン、移動、戦闘、アイテム取得、宿屋処理などの順序制御は `GameLoopUseCase` ではなく `WorldSimulationOrchestrator` が持つ。

## WorldGameLoopEntryPoint

`WorldGameLoopEntryPoint` は Worldシーンに配置される `MonoBehaviour`。

責務:

- `Update()` で `Time.unscaledDeltaTime` を取得する
- `IWorldSimulationOrchestrator.AdvanceFrameAsync()` を呼ぶ
- 多重実行を防ぐ
- Application の進行順序は持たない

## WorldSimulationOrchestrator の接続順

`WorldSimulationOrchestrator` は Application 層のゲーム進行順序を所有する。

1. `GameLoopUseCase` で時間を進める
2. 日付変更があれば宿屋日次レポートを発行する
3. 表示中レイヤーのリアルタイム移動を進める
4. schedule tick が進んだ場合、冒険者 / モンスターのスポーンと低頻度ライフサイクルを進める
5. Ground にいる Actor の準備処理を、装備更新、アイテム売却、宿屋予約、回復アイテム使用、帰還 / 出発判定の順に実行する
6. ActorActionPhase と AI を進める
7. 戦闘遭遇、通常攻撃、Projectile、AreaEffect を進める
8. アイテム拾得、ActorEffect、宿屋回復を進める
9. フェーズイベントをフラッシュする

## 注意

- `ProductLifetimeScope` にゲームラン状態を置かない。
- タイトル画面で `GameWorldState` や `GameLoopUseCase` を生成しない。
- Domain / Application は UnityEngine に依存しない。
- View は Domain 判定を直接決めない。
- `SceneManager.LoadScene` を直接使わず、Lighthouse のシーン遷移を使う。

## 現行登録グループ

現行登録は、`ProductLifetimeScope`、`GameSessionLifetimeScope`、`WorldLifetimeScope` に分かれる。

### ProductLifetimeScope

- Lighthouse / LighthouseExtends 基盤
- Asset / Save / TextTable / Master Repository
- `ActorFactory`
- `ScoutCostPolicy`
- `GameSessionLifecycle` と開始リクエスト系

### GameSessionLifetimeScope

- Application: イベント / アクター状態 / AI
  - `ActorProfileRegistry`
  - `GameEventHistoryService`
  - `GameEventBus`
  - `AdventurerBattleRecordService`
  - `ActorExplorationAchievementRegistry`
  - `AdventurerReturnTrackingService`
  - `AdventurerRecoveryStateService`
  - `ActorProcessingCandidateService`
  - `AdventurerDeathRevivalService`
  - `AdventurerExplorationStateService`
  - `ActorDecisionScheduler`
  - `AdvanceActorAiOrchestrator`
  - `AdventurerAiPolicy`
  - `MonsterAiPolicy`
  - `PetAiPolicy`
  - `GuildStaffAiPolicy`
- Application: ナビゲーション / 空間
  - `GameRandom`
  - `ActorNavigationService`
  - `ActorCombatService`
  - `ActorSpatialIndexService`
  - `ItemSpatialIndexService`
- Application: ワールド状態 / ゲームループ
  - `GameClock`
  - `GameWorldState`
  - `WorldMapViewDataProvider`
  - `ActorViewDataStore`
  - `GameWorldFrameBuffer`
  - `InitializeWorldMapUseCase`
  - `GenerateDungeonFloorUseCase`
  - `EnsureDungeonFloorGeneratedOrchestrator`
  - `InitializeDungeonOrchestrator`
  - `InitializeGameWorldOrchestrator`
  - `GameLoopUseCase`
  - `WorldSimulationOrchestrator`
  - `SetGameTimeScaleUseCase`
  - `PauseGameTimeUseCase`
  - `ResumeGameTimeUseCase`
  - `ToggleGamePauseUseCase`
  - `GetGameTimeStateUseCase`
- Application: 経済 / 宿屋
- Application: アクタースポーン
- Application: アクター移動 / 探索
- Application: 戦闘
- Application: アドベンチャラー帰還 / 宿屋処理
- Application: セーブ / ロード

### WorldLifetimeScope

- View: シーン基盤
  - `WorldScene`
  - `WorldGameLoopEntryPoint`
  - `WorldPresenter`
  - `WorldViewRoot`
- View: マップ描画
  - `WorldMapViewSettingsRepository`
  - `LayerPositionViewSettingsRepository`
  - `WorldCameraSettingsRepository`
  - `MapLayerViewRegistry`
  - `LayerPositionViewMapper`
  - `VisualConfigLoader`
  - `MapMaterialSet`
  - `MapTileVisualConfig`
  - `MapMeshBuildService`
  - `NavMeshBuildService`
  - `EnvironmentObjectPlacer`
  - `WorldMapView`
- View: アクター / Projectile / AreaEffect 描画
  - `ActorVisualDefinitionLoader`
  - `ActorPrefabSource`
  - `WorldActorViewPool`
  - `WorldActorViewRegistry`
  - `WorldActorPresenter`
  - `ProjectilePrefabSource`
  - `WorldProjectileViewPool`
  - `WorldProjectilePresenter`
  - `AreaEffectPrefabSource`
  - `WorldAreaEffectViewPool`
  - `WorldAreaEffectPresenter`
  - `ActorCombatAnimationPresenter`
- View: 入力 / カメラ / Bridge
  - `WorldLayerViewController`
  - `WorldActorSelectionInputHandler`
  - `WorldActorCameraFollowController`
  - `WorldActorScreenPositionProvider`
  - `WorldActiveLayerProvider`
  - `UnityNavMeshPathProvider`
  - `WorldNavigationPathProviderEntryPoint`
- Debug
  - `WorldDebugGameLogPresenter`（DEBUG のみ）
