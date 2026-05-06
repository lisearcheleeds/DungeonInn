# Game Event Design

このドキュメントは、ゲーム内で発生するイベントの通知方式と、購読者の設計方針をまとめる。

## 目的

戦闘・移動・スポーン・ダンジョン入退場など、ゲーム内で発生した出来事を
関心のある複数のシステムへ通知する仕組みを提供する。

発行者（Publisher）と購読者（Subscriber）を分離し、それぞれが独立して寿命・責務を持てるようにする。

## 設計方針

### イベントは「通知」であり「命令」ではない

イベントバスに流れるイベントは、**すでに確定した状態変化の通知**である。

```
UseCase（処理）                          Bus（通知）

AdvanceCombatUseCase
  ├─ 射程内か判定
  ├─ ダメージ計算
  ├─ HP減算              ──→  Publish(new CombatAttackOccurred(...))
  └─ HP0判定             ──→  Publish(new ActorDefeated(...))
```

購読者はゲームの状態を変更しない。
「このイベントを受け取ったら別のUseCase処理を起動する」という設計は取らない。
ゲームロジックはUseCase内で完結させ、バスは通知専用とする。

### UseCase はトランザクション境界

1つのUseCase実行が1つのトランザクションである。
「攻撃する」「ダメージを受ける」「死亡判定する」は別々のイベントで連鎖させず、
1つのUseCaseが順序と整合性を保って完結させ、完了後にまとめてイベントを発行する。

これにより：
- ゲーム状態の整合性がUseCase内に閉じる
- 購読順序がゲームロジックに影響しない
- デバッグ時に「なぜ状態が変わったか」はUseCase内を見ればわかる

### 発行者は Use Case 層のみ

Domain Entity はイベントを発行しない。
バスへの Publish は Application / UseCase 層が行う。

## バス設計

単一の `Subject<IGameEvent>` をイベントバスとして使う（案A）。
型別フィルタは購読側が行う。

```csharp
public interface IGameEventBus
{
    void Publish(IGameEvent gameEvent);
    IObservable<T> OnEvent<T>() where T : IGameEvent;
}

public sealed class GameEventBus : IGameEventBus
{
    readonly Subject<IGameEvent> subject = new();

    public void Publish(IGameEvent gameEvent)
    {
        subject.OnNext(gameEvent);
    }

    public IObservable<T> OnEvent<T>() where T : IGameEvent
    {
        return subject.OfType<T>();
    }
}
```

## IGameEvent

全イベントが実装するマーカーインターフェース。

```csharp
public interface IGameEvent { }
```

イベントクラスは不変の値オブジェクトとして実装する。
必要な情報（ActorId、Name、数値など）をコンストラクタで受け取り、公開プロパティで読み取れるようにする。

### 命名規約

`{何が}{どうなった}` の形式で過去形を使う。

例：

| クラス名 | 意味 |
|---|---|
| `CombatAttackOccurred` | 攻撃が発生した |
| `ActorDefeated` | ActorのHPが0になった |
| `CombatEncounterStarted` | 戦闘遭遇が始まった |
| `CombatEncounterEnded` | 戦闘遭遇が終わった |
| `ActorEnteredDungeon` | Actorがダンジョンに入った |
| `ActorExitedDungeon` | Actorがダンジョンから出た |
| `ActorSpawned` | Actorがスポーンした |

## 購読者の種類と寿命

### 揮発性購読者（View / エフェクト）

- 戦闘ログUI、ダメージ数値表示、ヒットエフェクトなど
- イベントを受け取り表示するが、状態を保持しない
- View / Presenter 層に置く
- VContainer の Lifetime Scoped で購読を管理し、シーン破棄時に破棄される

### 長期保管購読者（戦績・統計）

- `AdventurerBattleRecord`：冒険者ごとの戦闘履歴・ダメージ統計を記録する
- `CombatEncounterStarted` / `CombatEncounterEnded` を受け取り、1戦闘ごとのサマリーを生成する
- `CombatAttackOccurred` を受け取り、ダメージ与/受を集計する
- `ActorDefeated` を受け取り、撃破/死亡を記録する
- `ActorExitedDungeon` のタイミングでサマリーを確定し、保管ストアへ移す
- Application 層に置き、`GameWorldState` 経由で参照可能にする

## 廃棄管理

購読は `CompositeDisposable` で管理し、VContainer の Lifetime Scoped による自動破棄に乗せる。

```csharp
public sealed class CombatLogPresenter : IInitializable, IDisposable
{
    readonly IGameEventBus eventBus;
    readonly CompositeDisposable disposables = new();

    public CombatLogPresenter(IGameEventBus eventBus)
    {
        this.eventBus = eventBus;
    }

    public void Initialize()
    {
        eventBus.OnEvent<CombatAttackOccurred>()
            .Subscribe(OnAttack)
            .AddTo(disposables);
    }

    public void Dispose() => disposables.Dispose();

    void OnAttack(CombatAttackOccurred e) { ... }
}
```

## フォルダ方針

```text
Application/
  Event/
    IGameEvent
    IGameEventBus
    GameEventBus
    Events/
      CombatAttackOccurred
      ActorDefeated
      CombatEncounterStarted
      CombatEncounterEnded
      ActorEnteredDungeon
      ActorExitedDungeon
      ActorSpawned
  Combat/
    AdventurerBattleRecord   ← 長期保管購読者

View/
  Presenter/
    CombatLogPresenter       ← 揮発性購読者（UI表示）
```

## 既存クラスの移行

`AdvanceCombatResult`（フレーム単位の集約結果）はイベントバス導入後に廃止する。

移行前：
- `AdvanceCombatUseCase` が `AdvanceCombatResult` を返す
- `WorldGameLoopEntryPoint` がログ出力

移行後：
- `AdvanceCombatUseCase` が `CombatAttackOccurred` / `ActorDefeated` を Publish する
- `WorldGameLoopEntryPoint` のログ出力は `CombatLogPresenter` などの購読者に移す
- `AdvanceCombatResult` は void または空の結果型に簡略化する

既存の `CombatAttackEvent` / `CombatDeathEvent` は `IGameEvent` を実装する形にリネーム・移行する。
