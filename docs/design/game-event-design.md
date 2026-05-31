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
- `CombatEncounterEnded` / `ActorExitedDungeon` は統計の確定条件ではなく、現状は `WorldDebugGameLogPresenter` が累積統計を表示する契機として扱う
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

## バッファ付きパブリッシュ契約（現状）

戦闘トランザクションヘルパーはグローバルイベントバスの参照を保持せず、バッファなし公開オーバーロードを公開しない。
`CombatDamageResolver`・`CombatEffectExecutor`・`CombatDefeatResolver` は明示的な `IEventPublisher` 引数を必要とする。
ランタイムの呼び出し元は `BufferedEventPublisher` を渡し、状態変更を先に完了してから外側の UseCase で一度フラッシュする。

現在の戦闘イベント順:

- 通常攻撃: `CombatAttackOccurred`
- 飛翔体命中: `ProjectileHit` → `CombatAttackOccurred`
- 範囲効果命中: `AreaEffectHit` → ターゲットごとに `CombatAttackOccurred`
- モンスター撃破トランザクション: 直前の命中イベント → `CombatAttackOccurred` → `CombatEncounterEnded` → `ActorDefeated` → 種族ドロップの `ItemDropped` → `ExperienceGranted` → （任意）`ActorLeveledUp`
- 冒険者死亡復活トランザクション: 直前の命中イベント → `CombatAttackOccurred` → `CombatEncounterEnded` → `ActorDefeated` → 所持品 / 装備 / 所持金ロストの `ItemDropped` → `ActorReservedInn` → `ExperienceGranted` → （任意）`ActorLeveledUp`

上記イベントは、所有 UseCase / Orchestrator がトランザクションを完了した後にのみグローバルバスに発行される。

## `AdvanceFrameAsync` 内の戦闘イベント順序

`WorldSimulationOrchestrator.AdvanceFrameAsync()` は現在、以下のフレーム順序で戦闘関連イベントを発行する。
これはゲーム状態を変更しないサブスクライバー向けの通知順序契約である。
サブスクライバーが同一フレーム内の後続イベントに依存する挙動を必要とする場合は、この順序契約を参照し、イベント購読ではなく UseCase / Orchestrator の実行内で状態変更を行うこと。

1. 遭遇検出フェーズ: `DetectCombatEncounterUseCase` は同フレームの攻撃解決より先に実行される。グローバルイベントバスを通じて `CombatEncounterStarted` と `CombatEncounterEnded` を即座に発行する可能性があるため、遭遇開始 / 終了通知は同フレームの攻撃・ダメージ結果通知より先に届く。
2. 通常攻撃フェーズ: `AdvanceCombatUseCase` が近接 / 直接武器攻撃を解決する。`CombatDamageResolver` が `CombatAttackOccurred` を `BufferedEventPublisher` に記録し、連鎖する戦闘効果で `ProjectileFired` や `AreaEffectCreated` も記録される場合がある。バッファは `AdvanceCombatUseCase` 終了時に一度フラッシュされる。
3. 撃破解決フェーズ: 通常攻撃・飛翔体命中・範囲効果命中がターゲットの死亡を確認した場合、`ActorDefeatOrchestrator` が所有フェーズ内で実行される。モンスター撃破では、撃破されたアクターを攻撃していた攻撃者の `CombatEncounterEnded`・`ActorDefeated`・種族ドロップの `ItemDropped`・`ExperienceGranted`・（任意）`ActorLeveledUp` の順。冒険者死亡復活では、`CombatEncounterEnded`・`ActorDefeated`・所持品 / 装備 / 所持金ロストの `ItemDropped`・`ActorReservedInn`・`ExperienceGranted`・（任意）`ActorLeveledUp` の順。モンスターが冒険者を倒した場合も、攻撃者が存在するなら経験値報酬を得る。これらのイベントはバッファ済みであり、所有フェーズのフラッシュ時にのみ届く。
4. 飛翔体フェーズ: `AdvanceProjectileUseCase` が通常攻撃の後に飛翔体を進行させる。命中時、`CombatEffectExecutor.ExecuteProjectileHit()` が `ProjectileHit` を記録し、リンクされた直接ダメージが `CombatAttackOccurred` を記録する。リンクされた効果で `ProjectileFired` や `AreaEffectCreated` などの追加イベントも記録される場合がある。撃破イベントは 3 の順序に従い、飛翔体フェーズ終了時に一度フラッシュされる。
5. 範囲効果フェーズ: `AdvanceAreaEffectUseCase` が飛翔体の後に範囲効果を進行させる。各ターゲット命中につき、`CombatEffectExecutor.ExecuteAreaHit()` が `AreaEffectHit` を記録し、リンクされた直接ダメージが `CombatAttackOccurred` を記録する。撃破イベントは 3 の順序に従い、範囲効果フェーズ終了時に一度フラッシュされる。
