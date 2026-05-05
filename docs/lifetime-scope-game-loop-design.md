# LifetimeScope / Game Loop DI Design

このドキュメントは、DungeonInn のゲームループ実装に入る前に、VContainer / Lighthouse の LifetimeScope と Application 層サービスの配置方針を整理する。

## 前提

DungeonInn は Lighthouse のシーン遷移を使う。
LifetimeScope の親子関係は、現在の実装では以下になる。

```text
RootLifetimeScope
└── ProductLifetimeScope
    ├── MainScene LifetimeScope
    └── ModuleScene LifetimeScope
```

`RootLifetimeScope` は `RootEntryPoint` で `ProductLifetimeScope` prefab を生成する。
`ProductLifetimeScope` は `DontDestroyOnLoad` でアプリ起動中は生存し、`ProductEntryPoint` が MainScene / ModuleScene の親 LifetimeScope として設定する。

`View/Base` は MainScene / ModuleScene / Dialog の基底クラス群であり、LifetimeScope を置く場所ではない。
LifetimeScope の実体は `Core` または各 `View/Scene/...` に置く。

## 基本方針

Application 層そのものが LifetimeScope を持つわけではない。
Application のサービスを、どの LifetimeScope に登録するかをシーン寿命で決める。

- アプリ全体で共有し、タイトル画面でも存在してよいものは `ProductLifetimeScope` に登録する。
- ゲームプレイ中の1ランだけ存在する状態や進行サービスは、インゲーム MainScene の LifetimeScope に登録する。
- View / Presenter / Scene 固有 UI は各 MainScene / ModuleScene の LifetimeScope に登録する。

UseCase / Factory / Repository は static class にしない。
状態を持たない処理でも、VContainer で依存注入できるインスタンスサービスとして扱う。
理由は、Repository、Policy、Clock、Random、Logger、Config などを後から差し替えやすくするため。

## ProductLifetimeScope に置くもの

`ProductLifetimeScope` はゲーム全体で共有されるアプリ基盤を持つ。
タイトル画面、インゲーム画面、結果画面などをまたいで存在してよいものだけを登録する。

現在登録済み、または登録候補:

- Lighthouse / LighthouseExtends の基盤
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
  - `IMasterRepository`
  - `IAdventurerFactory`
  - `IMonsterFactory`
- 状態を持たない UseCase / Policy / Calculator
  - `SpawnAdventurerUseCase`
  - `SpawnAdventurerFromMasterUseCase`
  - `SpawnMonsterFromMasterUseCase`

`IMasterRepository` は MasterMemory 導入後も読み取り契約として残す。
現在は `HardcodedMasterRepository` を `ProductLifetimeScope` に登録する。

## InGame MainScene LifetimeScope に置くもの

インゲーム画面でのみ必要なものは、インゲーム MainScene の LifetimeScope に登録する。
タイトル画面では生成しない。

登録候補:

- ゲーム進行状態
  - `GameWorldState`
  - `GameClock`
  - `GameRandom`
  - `ActorRegistry`
  - `GameRunSettings`
- ゲームループ
  - `GameLoopUseCase`
  - `AdvanceGameTickUseCase`
  - `GameLoopPresenter` または `GameLoopEntryPoint`
- AI
  - `ActorDecisionScheduler`
  - `AdvanceActorAiUseCase`
  - `ApplyActorAiDecisionUseCase`
  - `AdventurerAiPolicy`
  - `MonsterAiPolicy`
  - `GuildStaffAiPolicy`
  - `PetAiPolicy`
- スポーン
  - `AdventurerSpawnScheduler`
  - `MonsterSpawnScheduler`
- ダンジョン
  - `GenerateDungeonFloorUseCase`
  - フロア到達時の生成サービス
- 戦闘
  - `CombatEncounterDetector`
  - `CombatLineOfSightService`
  - `CombatActionSelector`
  - `CombatEffectExecutor`
- 施設処理
  - 施設利用 Action を UseCase に接続するオーケストレーション

これらはゲームランの開始と終了に合わせて生成・破棄されるべきである。
そのため `ProductLifetimeScope` ではなく、インゲーム MainScene の LifetimeScope に置く。

## 想定するインゲーム構成

将来的に `InGameScene` を作る場合、以下のような構成にする。

```text
View/Scene/MainScene/InGame/
├── InGameScene
├── InGameLifetimeScope
├── InGamePresenter
└── InGameInputLayer
```

`InGameLifetimeScope` は `ProductLifetimeScope` の子として生成される。
`InGameScene` が閉じられると、登録された `GameWorldState` や `GameLoopUseCase` も破棄される。

例:

```csharp
public sealed class InGameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<InGameScene>();
        builder.Register<InGamePresenter>(Lifetime.Scoped).AsImplementedInterfaces();

        builder.Register<GameWorldState>(Lifetime.Scoped).As<IGameWorldState>();
        builder.Register<GameClock>(Lifetime.Scoped).As<IGameClock>();
        builder.Register<ActorDecisionScheduler>(Lifetime.Scoped);
        builder.Register<GameLoopUseCase>(Lifetime.Scoped);
    }
}
```

## GameWorldState の責務

ゲームループに入る前に、インゲーム中の状態をまとめる `GameWorldState` を作る。

想定責務:

- 現在 tick
- ゲーム内日付
- 現在の `AdventurerGuild`
- 現在の `Dungeon`
- 生成済み Actor 一覧
- Actor の登録 / 削除
- Dungeon floor の生成済み状態
- ゲームラン中の一時状態

`GameWorldState` は Domain Entity ではなく、Application / Runtime の集約状態として扱う。
Domain の不変条件は各 Entity が守り、`GameWorldState` はそれらを保持・検索する。

## Game Loop の責務

ゲームループは `GameWorldState` と UseCase / Service をつなぐオーケストレーションである。
Domain そのものにゲームループを置かない。

1 tick の初期処理順序案:

1. 時刻更新
2. 日付変更イベントの発火
3. 冒険者スポーン判定
4. モンスタースポーン判定
5. AI Dirty 評価
6. Actor Action の実行
7. 移動 / 階段移動 / フロア生成
8. 戦闘遭遇判定
9. 戦闘処理
10. 施設利用 / 売買 / 回復処理
11. デスポーン / 死亡処理
12. View 通知用の差分記録

初期実装ではすべてを一度に作らず、以下の順で段階的に接続する。

1. `GameWorldState`
2. `GameClock`
3. `AdvanceGameTickUseCase`
4. 冒険者スポーン
5. AI Dirty 評価
6. 移動
7. ダンジョン階段移動 / フロア生成
8. 戦闘
9. 施設利用 / 売買

## DI 登録の分割方針

`ProductLifetimeScope` にすべて直接書くと肥大化する。
登録が増えた段階で Installer 風の static helper または小さな登録クラスに分ける。

候補:

- `MasterInstaller`
- `ActorInstaller`
- `AiInstaller`
- `DungeonInstaller`
- `CombatInstaller`
- `GameLoopInstaller`

ただし、VContainer の LifetimeScope 自体を増やすという意味ではない。
1つの `Configure` 内で登録処理を分割するための整理である。

## 注意点

- `ProductLifetimeScope` にゲームラン状態を置かない。
- タイトル画面で `GameWorldState` や `GameLoopUseCase` を生成しない。
- UseCase を static にしない。
- Factory は生成対象ごとに分ける。
- Request にはその Factory に必要な情報だけを載せる。
- Domain / Application は UnityEngine に依存しない。
- View は Domain 判定を決めない。
- `SceneManager.LoadScene` を直接使わず、Lighthouse のシーン遷移を使う。
