# LifetimeScope / Game Loop DI Design

このドキュメントは、DungeonInn の VContainer / Lighthouse における LifetimeScope 配置、Application 層サービスの登録方針、ゲームループの時間管理方針をまとめる。

## LifetimeScope 階層

現在の親子関係は以下。

```text
RootLifetimeScope
└── ProductLifetimeScope
    ├── MainScene LifetimeScope
    └── ModuleScene LifetimeScope
```

- `RootLifetimeScope` は `RootEntryPoint` で `ProductLifetimeScope` prefab を生成する。
- `ProductLifetimeScope` は `DontDestroyOnLoad` でアプリ起動中は生存する。
- `ProductEntryPoint` は MainScene / ModuleScene の親 LifetimeScope として `ProductLifetimeScope` を設定する。
- `View/Base` は Lighthouse の Scene / Dialog 基底ラッパー置き場であり、LifetimeScope 実体をまとめる場所ではない。
- LifetimeScope 実体は `Core` または各 `View/Scene/...` に置く。

## 登録方針

Application 層そのものが LifetimeScope を持つわけではない。Application のサービスをどの LifetimeScope に登録するかは、サービスの寿命で決める。

- アプリ全体で共有し、タイトル画面でも存在してよいものは `ProductLifetimeScope` に登録する。
- ゲームプレイ中の1ランだけ存在する状態や進行サービスは、インゲーム MainScene の LifetimeScope に登録する。
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
  - `ProductAssetLoader`
  - `HardcodedMasterRepository` as `IMasterRepository`
  - `AdventurerFactory` as `IAdventurerFactory`
  - `MonsterFactory` as `IMonsterFactory`
  - `ScoutCostPolicy`
- 共有してよい UseCase
  - Master / Factory / Domain Entity を組み合わせるステートレスUseCase群
  - 例: `SpawnAdventurerFromMasterUseCase`, `SpawnMonsterFromMasterUseCase`, `GenerateDungeonFloorUseCase`

`IMasterRepository` は将来 MasterMemory に置き換える想定だが、依存側は `IMasterRepository` のままにする。

## World LifetimeScope に置くもの

現在のインゲーム MainScene は `World`。

`WorldLifetimeScope` には、Worldシーンの寿命に閉じるものを登録する。

- Scene固有
  - `WorldScene`
  - `WorldPresenter`
  - `WorldGameLoopEntryPoint`
- ゲームラン状態
  - `GameWorldState` as `IGameWorldState`
  - `GameClock` as `IGameClock`
- ゲームループ
  - `GameLoopUseCase` as `IGameLoopUseCase`
  - `SetGameTimeScaleUseCase`
- AI実行基盤
  - `ActorDecisionScheduler`
  - `ApplyActorAiDecisionUseCase`
  - `AdvanceActorAiOrchestrator`
  - `AdventurerAiPolicy`
  - `MonsterAiPolicy`
  - `PetAiPolicy`
  - `GuildStaffAiPolicy`

これらはタイトル画面では生成しない。Worldシーンが閉じられれば破棄される。

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

現在の初期実装では以下のみ行う。

1. `GameClock.Advance(unscaledDeltaTimeSeconds)` を呼ぶ
2. 進んだ `CurrentScheduleTick` 数を返す
3. 日付変更有無を返す

AI評価は `GameLoopUseCase` から呼ばない。AIは状態変化でdirtyを立て、現実時間またはゲーム進行時間ベースで別途評価する。

## WorldGameLoopEntryPoint

`WorldGameLoopEntryPoint` は Worldシーンに配置される `MonoBehaviour`。

責務:

- `Update()` で `Time.unscaledDeltaTime` を取得する
- `IGameLoopUseCase.ExecuteAsync()` を呼ぶ
- 多重実行を防ぐ
- 現在は確認用に、1スケジュール秒ごとにActor数と内訳をログ出力する

ログ例:

```text
[WorldGameLoop] ScheduleTick=1 Day=0 GameTime=5.16s Scale=1 Actors=0 Adventurers=0 Monsters=0 Pets=0 GuildStaff=0 Others=0
```

現在はSpawn未実装なのでActor数は常に0になる。

## 今後の接続順

ログでゲームループが回ることは確認済み。次に接続する候補は以下。

1. `GameWorldState` の初期化
2. `AdventurerGuild` / `GroundMap` / `Dungeon` の生成
3. 冒険者スポーンスケジューラ
4. `GameWorldState.RegisterActor`
5. AI dirtyイベントの発火
6. Actor Action 実行
7. 移動 / 戦闘 / 施設利用

## 注意

- `ProductLifetimeScope` にゲームラン状態を置かない。
- タイトル画面で `GameWorldState` や `GameLoopUseCase` を生成しない。
- Domain / Application は UnityEngine に依存しない。
- View は Domain 判定を直接決めない。
- `SceneManager.LoadScene` を直接使わず、Lighthouse のシーン遷移を使う。
