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

### シーンスコープ統計購読者（戦績・統計）

- `AdventurerBattleRecordService`：冒険者ごとの戦闘回数・与ダメージ・被ダメージ・撃破数をシーンスコープで累積する
- `CombatEncounterStarted` を受け取り、戦闘開始回数を加算する
- `CombatAttackOccurred` を受け取り、ダメージ与/受を集計する
- `CombatAttackOccurred.TargetRemainingHp <= 0` の攻撃者を撃破数として加算する
- `CombatEncounterEnded` / `ActorExitedDungeon` は統計の確定条件ではなく、現状は `WorldGameLogPresenter` が累積統計を表示する契機として扱う
- Application 層に置き、`AdventurerBattleRecordService.TryGetRecord` 経由で参照する
- 永続化や1戦闘ごとの保管ストアが必要になった場合は、別途 `CombatEncounterEnded` / `ActorExitedDungeon` を契機に確定する Store を追加する

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

## TODO

### MessagePipe への移行検討

現在の `IGameEventBus` は MessagePipe に置換できる。移行タイミングはパフォーマンス計測でメッセージングがボトルネックになった時、または MessagePipe を別用途（シーン間通信・Request-Response）で導入するタイミング。

置換方針の選択肢：

- **案A（推奨初手）**: `IGameEventBus` インターフェースを維持し、実装だけ MessagePipe に差し替える。全呼び出し元の変更ゼロ。
- **案B（完全移行）**: `IGameEventBus` を廃止し、`IPublisher<T>` / `ISubscriber<T>` を直接注入する。型ごとチャンネル設計の恩恵を得られるが、注入箇所が型の数だけ増える。

今は導入不要。必要になったタイミングで案A → 案B の順で移行する。

### 状態異常ダメージ用イベントの追加

毒・燃焼などの継続ダメージは `CombatAttackOccurred` に追加せず、別イベントとして定義する。

理由：
- 攻撃者が存在しない（または `AttackerActorId` が意味を持たない）
- 武器攻撃とは発生タイミングが異なる（ターン開始時 など）
- `AdventurerBattleRecord` で「武器ダメージ」と「状態異常ダメージ」を別集計したい

想定イベント：

| イベント | 用途 |
|---|---|
| `StatusEffectDamageOccurred` | 毒・燃焼などの継続ダメージ発生 |
| `ActorStunned` | 麻痺などによる行動不能 |
| `StatusEffectApplied` | 状態異常付与 |
| `StatusEffectExpired` | 状態異常解除 |

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
    AdventurerBattleRecordService   ← シーンスコープ統計購読者

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
