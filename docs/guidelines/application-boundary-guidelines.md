# Application 層 設計ガイドライン

このドキュメントは、UseCase / Service / Orchestrator / Event / Aggregate 境界を設計・レビューするときの判断基準をまとめる。
特定プロジェクト専用の実装手順ではなく、別プロジェクトでも同じ設計判断を再現するための指針とする。

---

## UseCase / Service / Orchestrator の定義と命名

以下の3種類を明確に区別する。

| 種別 | 責務 | NG パターン |
|---|---|---|
| **UseCase** | 単一ユースケースを実行する。ステートレス。他 UseCase を呼ばない | `IDisposable` を実装する / 状態を持つ |
| **Service** | 長期状態を保持する。`IDisposable` を実装。イベントを購読して状態を更新する | UseCase を注入して呼ぶ |
| **Orchestrator** | UseCase を定義された順序で呼ぶ。順序制御上の分岐は持ってよい | Domain 判断・業務ルール計算を自身に抱え込む |

DungeonInn では、新規実装の命名を以下に統一する。

- `XxxUseCase`: 入力を受け取り、1回のトランザクションとしてDomainを変更し、必要なEventを発行するステートレスな処理。
- `XxxStateService`: `Dictionary` / `HashSet` などの長期状態を保持し、必要に応じてEventを購読して状態を更新・破棄するService。
- `XxxService`: Repository / Registry相当の状態管理、計算補助、または複数UseCaseから共有される補助処理。単発のDomain変更コマンドには使わない。
- `XxxOrchestrator`: 複数UseCase / Serviceを、明示された順序で呼び出す調整役。Domainルールそのものは持たない。

既存の単発コマンドは `ChargeInnFeeUseCase` / `GrantExperienceUseCase` / `DropItemUseCase` / `DespawnAdventurerUseCase` として `XxxUseCase` に統一する。長期状態を持つ場合のみ `XxxStateService` / `XxxService` に残す。

Orchestrator が持ってよい分岐は「成功・失敗に応じた次 UseCase の選択」程度に留める。
戦闘勝敗の判定・料金計算・ビジネスルールの評価を Orchestrator 内で直接計算するのは NG。

```csharp
// Before: UseCase が状態を持ち、IDisposable を実装している
public sealed class FooUseCase : IDisposable
{
    readonly Dictionary<Guid, float> state = new(); // NG: UseCase が状態を持つ
}

// After: 種別に応じてクラスを正しく分類する
public sealed class FooStateService : IDisposable  // Service が状態を持つ
{
    readonly Dictionary<Guid, float> state = new();
}

public sealed class FooUseCase                     // UseCase はステートレス
{
    readonly FooStateService fooState;
    public async UniTask ExecuteAsync(...) { ... }
}

public sealed class FooOrchestrator                // Orchestrator は順序制御のみ
{
    public async UniTask ExecuteAsync(...)
    {
        barUseCase.Execute(...);
        await fooUseCase.ExecuteAsync(...);
    }
}
```

### DungeonInn Example

- `AdventurerRecoveryStateService` / `AdventurerExplorationStateService` → Service
- `AdvanceCombatUseCase` / `SellItemsUseCase` → UseCase
- `WorldGameLoopEntryPoint` / `AdvanceActorAiOrchestrator` → Orchestrator

---

## 1. UseCase は他の UseCase を呼ばない

UseCase の内部から別の UseCase を直接呼び出してはならない。

UseCase 内部で UseCase を呼ぶと、呼び出し元は「何が実行されるか」を知らずに複数の副作用を受け取ることになる。
これはトランザクション境界の混乱と、デバッグ・テストの困難さを生む。

### Before

```csharp
public sealed class CombatDefeatResolver
{
    readonly GrantExperienceUseCase grantExperienceUseCase;
    readonly DropItemUseCase dropItemUseCase;

    public void Resolve(IGameWorldState worldState, Actor attacker, Actor target)
    {
        grantExperienceUseCase.Execute(attacker, target);  // NG: UseCase から UseCase を呼ぶ
        dropItemUseCase.Execute(target, worldState);       // NG: 暗黙の連鎖
        worldState.RemoveActor(target.Id);
        eventBus.Publish(new ActorDefeated(...));
    }
}
```

### After

```csharp
// Resolver は戦闘状態の変更と事実の発行だけを行う
public sealed class CombatDefeatResolver
{
    public void Resolve(IGameWorldState worldState, Actor attacker, Actor target)
    {
        worldState.RemoveActor(target.Id);
        actorCombatService.ClearTargetsReferencing(target.Id);
        actorCombatService.RemoveState(target.Id);
        eventBus.Publish(new ActorDefeated(target.Id, attacker?.Id, DeathCause.Combat));
    }
}

// Orchestrator が「何を・どの順で行うか」を明示的に並べる
public sealed class HandleActorDefeatOrchestrator
{
    public async UniTask ExecuteAsync(IGameWorldState worldState, Actor attacker, Actor target)
    {
        combatDefeatResolver.Resolve(worldState, attacker, target);
        if (attacker != null)
            grantExperienceUseCase.Execute(attacker, target);
        dropItemUseCase.Execute(target, worldState);
    }
}
```

### 適用基準

- クラス名が UseCase でなくても、他の UseCase を注入して呼ぶ Resolver / Service / Handler はこのルールの対象
- UseCase 間の処理順序を表現したい場合は、上位 Orchestrator を1つ作り、そこに順序を書く

---

## 2. UseCase はステートレスに保つ

UseCase はリクエストを受け取り、Domain を操作し、イベントを発行して終わる単方向の処理単位である。
イベントを購読して内部状態（Dictionary、HashSet 等）を管理するクラスは UseCase ではなく Service であり、
LifetimeScope 上の寿命管理と責務の区別を明確にしなければならない。

### Before

```csharp
public sealed class RecoverAdventurerAtInnUseCase : IDisposable
{
    readonly Dictionary<Guid, float> accumulatedHp = new();  // NG: UseCase が状態を持つ
    readonly IDisposable deathSubscription;

    public RecoverAdventurerAtInnUseCase(IGameEventBus eventBus)
    {
        deathSubscription = eventBus.OnEvent<ActorDefeated>()
            .Subscribe(evt => accumulatedHp.Remove(evt.ActorId));  // NG: UseCase がイベントを購読する
    }
}
```

### After

```csharp
// 長期状態は専用の Service に分離し、LifetimeScope で寿命を管理する
public sealed class AdventurerRecoveryStateService : IDisposable
{
    readonly Dictionary<Guid, float> accumulatedHp = new();
    readonly IDisposable subscription;

    public AdventurerRecoveryStateService(IEventSubscriber eventSubscriber)
    {
        subscription = eventSubscriber.OnEvent<ActorDefeated>()
            .Subscribe(evt => accumulatedHp.Remove(evt.ActorId));
    }

    public float GetAccumulatedHp(Guid actorId) =>
        accumulatedHp.TryGetValue(actorId, out var v) ? v : 0f;

    public void Dispose() => subscription.Dispose();
}

// UseCase はステートレスに戻す
public sealed class RecoverAdventurerAtInnUseCase
{
    readonly AdventurerRecoveryStateService recoveryState;

    public async UniTask ExecuteAsync(
        IGameWorldState worldState, Actor adventurer, float deltaGameSeconds)
    {
        var accumulated = recoveryState.GetAccumulatedHp(adventurer.Id);
        // ... 回復計算 ...
        recoveryState.SetAccumulatedHp(adventurer.Id, newAccumulated);
    }
}
```

### 適用基準

- UseCase が `IDisposable` を実装している場合、そのクラスを Service に昇格させることを検討する
- UseCase のコンストラクタでイベント購読を行ってはならない。購読は Service の責務

---

## 3. UseCase は Domain を組み合わせるトランザクション境界である

UseCase は Domain Entity を操作してよいが、Domain 不変条件を壊さない経路を使う。
UseCase が担うのは、入力の組み合わせ、処理順序、取引記録、AI判断の適用、境界との受け渡しである。

1つの UseCase 実行は1つのトランザクションである。
「読み取る → ルールを適用する → 状態を変える → 通知する」の一連を完結させる責務を持つ。

### Before

```csharp
candidate.Role = Role.Staff;
candidate.ScoutCost = null;
```

### After

```csharp
var cost = await calculateCostUseCase.ExecuteAsync(candidate);
organization.Inventory.RemoveRange(cost);
candidate.ChangeBehavior(new StaffBehavior(salary));
organization.RecordTransaction(...);
```

一時的な費用は現在値から逐次計算する。採用結果は Behavior の変更として表す。取引履歴は UseCase が記録する。

### DungeonInn Example

スカウト費用は `ScoutCost` として Actor に保持しない。
`CalculateScoutCostUseCase` で逐次計算し、`RecruitStaffUseCase` が支払い・Behavior 変更・取引記録を行う。

---

## 4. イベントは「起きた事実」を、トランザクション完了後に通知する

ドメインイベントはドメインで何が起きたかの記録である。
表示名・フォーマット済みテキスト・UI 向け集計値など、View 側の関心事を埋め込まない。

また、UseCase の途中でイベントを発行すると、購読者から見た状態がまだ確定していない可能性がある。
状態変化をイベントで連鎖させない。

### Before（イベントに View 用値を含める）

```csharp
public sealed class ItemDropped : IGameEvent
{
    public Guid ActorId { get; }
    public Guid ItemInstanceId { get; }
    public string ItemName { get; }   // NG: 表示用文字列をイベントに持たせている
}
```

### After

```csharp
public sealed class ItemDropped : IGameEvent
{
    public Guid ActorId { get; }
    public ItemInstance ItemInstance { get; }  // ドロップされた事実をそのまま渡す
}
```

アイテム名が必要な View 側が `IMasterRepository` を通じて解決すればよい。

### Before（イベント連鎖）

```csharp
AttackUseCase → Publish(AttackEvent)
  ↓ subscribe
DamageUseCase → Publish(DamageEvent)
  ↓ subscribe
HpReduceUseCase → HP減算
```

購読順がゲームロジックの正しさを決める。整合性の保証がない。

### After（UseCase 内で完結・複数イベントは収集してから発行）

```csharp
// UseCase 内でまとめて状態変更し、完了後に発行する
var events = new List<IGameEvent>();
target.ReceiveDamage(damage);
events.Add(new ProjectileHit(projectile.Id, target.Id, damage));

if (target.Hp <= 0)
    defeatResolver.Resolve(target, events);

eventPublisher.PublishAll(events);  // 状態変更後にまとめて発行
```

### イベントに含めてよい値 / 含めてはいけない値

| 含めてよい | 含めてはいけない |
|---|---|
| ドメインオブジェクト本体（Entity、値型） | マスタデータから引いた表示名・説明文 |
| 変化の前後の状態（`PreviousLevel`, `NewLevel`） | View のレイアウトやフォーマットに依存する値 |
| 「誰が何をしたか」を表す ID と量 | 集計・変換済みの表示用スコア |

### DungeonInn Example

`AdvanceCombatUseCase` がダメージ計算・HP減算・死亡判定をすべて完結させ、
完了後に `CombatAttackOccurred` / `ActorDefeated` を `IGameEventBus` に Publish する。
`CombatLogPresenter`（UI表示）はこれを購読するが、ゲームの状態（Actor の HP 等）を変更しない。

---

## 5. イベント購読者はゲームの状態を変更しない

購読者が状態を変更すると、どの順序でイベントが処理されるかによって最終的な状態が変わる可能性がある。

### Before

```csharp
public sealed class DecideAdventurerReturnUseCase : IDisposable
{
    public DecideAdventurerReturnUseCase(IGameEventBus eventBus)
    {
        // NG: UseCase がイベント購読で Actor の状態を変更している
        eventBus.OnEvent<ActorDefeated>()
            .Subscribe(evt =>
            {
                defeatedMonsterCounts.IncrementKill(evt.KillerActorId);
                dirtyActorIds.Add(evt.KillerActorId);  // 状態変更
            });
    }
}
```

### After

```csharp
// 戦闘履歴の蓄積は Service に分離
public sealed class ActorCombatHistoryService : IDisposable
{
    readonly Dictionary<Guid, CombatHistory> histories = new();
    readonly IDisposable subscription;

    public ActorCombatHistoryService(IEventSubscriber eventSubscriber)
    {
        subscription = eventSubscriber.OnEvent<ActorDefeated>()
            .Subscribe(evt => RecordKill(evt.KillerActorId, evt.ActorId));
            // 購読内ではログとして記録するだけ。Actor への書き込みはしない
    }
}

// UseCase はヒストリを参照して、1回のトランザクションとして判断と変更を完結させる
public sealed class EvaluateAdventurerReturnUseCase
{
    public async UniTask ExecuteAsync(IGameWorldState worldState, Actor adventurer)
    {
        var history = historyService.GetHistory(adventurer.Id);
        var shouldReturn = EvaluateReturnCondition(adventurer, history);

        if (shouldReturn)
        {
            adventurer.RequireBehavior<AdventurerBehavior>()
                .ChangeLifecycleState(AdventurerLifecycleState.Returning);
            eventBus.Publish(new ActorStartedReturning(adventurer.Id));
        }
    }
}
```

### 適用基準

- イベント購読の `Subscribe` コールバック内では、ドメインオブジェクトのプロパティを変更しない
- 購読内で許可されるのは「集計」「カウント」「ログ記録」のみ。これらも Service クラスの内部に留める
- レビュー時は、購読コールバック内に `.ChangeXxx()` / `.SetXxx()` / `worldState.RegisterXxx()` のような呼び出しがないか確認する

---

## 6. Entity の全削除経路に state cleanup を紐付ける

Entity がシステムから除去される経路は複数ある（死亡、自然退場、強制消去など）。
`Dictionary<EntityId, State>` を持つ Service の cleanup が特定経路のみに実装され、別の経路では漏れるパターンが起きやすい。

### 方針

Entity-keyed state を持つ Service は、その Entity が除去されうる**全経路**に対して cleanup を実装する。

**アプローチ A: 削除を一元化する UseCase / Handler を設ける**

```csharp
public sealed class RemoveEntityUseCase
{
    public void Execute(IWorldState worldState, Guid entityId, RemovalCause cause)
    {
        worldState.Remove(entityId);
        fooStateService.Remove(entityId);
        barHistoryService.Remove(entityId);
        eventPublisher.Publish(new EntityRemoved(entityId, cause));
    }
}
```

**アプローチ B: Service 側が全除去イベントを購読する**

```csharp
public sealed class FooStateService : IDisposable
{
    // 購読するイベント: EntityDefeated, EntityDeparted（全除去経路を網羅すること）
    public FooStateService(IEventSubscriber eventSubscriber)
    {
        eventSubscriber.OnEvent<EntityDefeated>()
            .Subscribe(evt => states.Remove(evt.EntityId));
        eventSubscriber.OnEvent<EntityDeparted>()
            .Subscribe(evt => states.Remove(evt.EntityId));
    }
}
```

### レビュー観点

- Entity 除去 UseCase / Handler を追加したとき、関連する全 Service の cleanup を確認したか
- `XxxStateService` を追加したとき、どのイベントで cleanup するかを列挙したか
- cleanup が特定の除去経路だけに偏っていないか

### DungeonInn Example

`AdventurerExplorationStateService` / `AdventurerRecoveryStateService` は
`ActorDefeated`（死亡）では cleanup しているが、`ActorDeparted`（帰還）時の cleanup が未実装。

---

## 7. 集約内部の可変オブジェクトを直接公開しない

集約（Aggregate）は不変条件を守る責任を持つ。
内部に持つ `Equipment`、`Inventory`、`Stats` などの可変オブジェクトを `public get` で公開すると、
外部から集約を迂回して状態を書き換えることができてしまう。

### Before

```csharp
public sealed class Actor
{
    public ActorEquipment Equipment { get; }  // 外部から Equipment.Equip() を呼べる
}

actor.Equipment.Equip(master);  // NG: Actor.RefreshParams() が呼ばれない
```

### After

```csharp
public sealed class Actor
{
    ActorEquipment equipment;

    public IReadOnlyActorEquipment Equipment => equipment;

    public void Equip(EquipmentMaster master)
    {
        equipment.Equip(master);
        RefreshParams();  // Actor が責任を持って同期させる
    }
}
```

### 適用基準

- `public ActorEquipment Equipment { get; }` のように可変オブジェクトを返すプロパティは集約境界の漏れとみなす
- 読み取り専用インターフェース（`IReadOnlyXxx`）を作ることで、参照は渡しつつ書き込みは封じる
- `RefreshParams()` のような整合性維持メソッドが `public` になっている場合、`private` にして書き込み経路内で自動的に呼ぶ

---

## 8. 状態遷移を持つドメインオブジェクトは不正遷移を禁止する

`ActorAction`、`ActorGoal` のような状態遷移を内包するドメインオブジェクトは、不正な遷移をオブジェクト自身が防がなければならない。

### Before

```csharp
public sealed class ActorAction
{
    public ActorActionState State { get; private set; } = ActorActionState.NotStarted;

    public void Start()
    {
        State = ActorActionState.Running;  // NG: NotStarted でない状態から Start() しても通る
    }
}
```

### After

```csharp
public sealed class ActorAction
{
    public ActorActionState State { get; private set; } = ActorActionState.NotStarted;

    public void Start()
    {
        if (State != ActorActionState.NotStarted)
            throw new InvalidOperationException(
                $"Cannot start action in {State} state. Expected: {ActorActionState.NotStarted}");

        State = ActorActionState.Running;
    }
}
```

### 適用基準

- 「状態」を持ち、特定の順序で呼ばれることが前提のメソッドには、事前状態チェックを必ず入れる
- InvalidOperationException のメッセージには「現在の状態」と「期待する状態」を両方含める

---

## 9. Domain Entity が static クラスに依存しない

Domain Entity は DI コンテナから注入された依存のみを使う。
static クラスへのアクセス（Service Locator）は、テスト時に差し替えができず、暗黙のグローバル依存を生む。

### Before

```csharp
public sealed class Actor
{
    public Actor(...)
    {
        // NG: static クラスに直接アクセス。テスト時に差し替え不可
        NaturalWeaponTypeCombatMaster = WeaponTypeCombatMasterCatalog.Get(NaturalWeaponType);
    }
}
```

### After

```csharp
public sealed class Actor
{
    readonly IWeaponMasterRepository weaponMasters;

    public Actor(..., IWeaponMasterRepository weaponMasters)
    {
        this.weaponMasters = weaponMasters;
        NaturalWeaponTypeCombatMaster = weaponMasters.GetCombatMaster(NaturalWeaponType);
    }
}
```

### 適用基準

- Domain 層のクラスで `static` なクラス（Catalog、Registry、Locator 等）を呼んでいる箇所はすべて対象
- `Math` クラス等の純粋関数的 utility は対象外

---

## 10. IEventPublisher と IEventSubscriber を分離する

`IGameEventBus` は Publish と Subscribe の両方を持つが、大多数のクラスはどちらか一方しか必要としない。

### Before

```csharp
public interface IGameEventBus
{
    void Publish(IGameEvent gameEvent);
    Observable<T> OnEvent<T>() where T : class, IGameEvent;
}

// Publish だけすればよいが Subscribe 能力も持ってしまう
public sealed class CombatEffectExecutor
{
    readonly IGameEventBus eventBus;
}
```

### After

```csharp
public interface IEventPublisher  { void Publish(IGameEvent gameEvent); }
public interface IEventSubscriber { Observable<T> OnEvent<T>() where T : class, IGameEvent; }
public interface IGameEventBus : IEventPublisher, IEventSubscriber { }

// Publish しか使わないクラスには IEventPublisher だけを渡す
public sealed class CombatEffectExecutor
{
    readonly IEventPublisher eventPublisher;
}

// Subscribe だけを使うクラスには IEventSubscriber だけを渡す
public sealed class AdventurerRecoveryStateService
{
    readonly IEventSubscriber eventSubscriber;
}
```

### 適用基準

- UseCase はほぼ全て `IEventPublisher` のみで十分
- Service / Aggregator 系は `IEventSubscriber` のみ
- View / Presenter は `IEventSubscriber` のみ
- `IGameEventBus` を直接注入するのは、LifetimeScope の登録や両方が必要な稀なケースのみ

---

## 11. IGameWorldState は読み書きでインターフェースを分ける

`IGameWorldState` に読み取りと書き込みのメソッドが混在すると、
「読むだけのはずの UseCase」が誤って書き込むことを型で防げない。

### Before

```csharp
public interface IGameWorldState
{
    IReadOnlyList<Actor> Actors { get; }
    Actor FindActor(Guid actorId);   // 読み取り
    void RegisterActor(Actor actor); // 書き込み（同一インターフェース）
}
```

### After

```csharp
public interface IGameWorldStateReader
{
    IReadOnlyList<Actor> Actors { get; }
    Actor FindActor(Guid actorId);
}

public interface IGameWorldStateWriter
{
    void RegisterActor(Actor actor);
    bool RemoveActor(Guid actorId);
}

public interface IGameWorldState : IGameWorldStateReader, IGameWorldStateWriter { }

// 読むだけの UseCase には Reader だけを渡す
public sealed class DetectCombatEncounterUseCase
{
    public async UniTask ExecuteAsync(IGameWorldStateReader worldState) { ... }
}
```

### 適用基準

- `worldState.Actors` を参照するだけの UseCase は `IGameWorldStateReader` を受け取る
- 今後追加する UseCase は、まず `IGameWorldStateReader` で書き始め、書き込みが必要になったときのみ `IGameWorldState` に変更する

---

## 12. View 層には表示専用の Reader / DTO を提供する

View が Application 内部用の広い Reader interface を直接参照すると、
表示層から Domain 集約全体へアクセスできる状態になる。
`entity.Behavior is XxxBehavior` のような型判断が View 層に漏れ、Domain の変更が View に伝播する。

### Before

```csharp
// Application 内部用の広い Reader を View に直接渡している
public sealed class WorldMapView
{
    readonly IGameWorldStateReader worldState;  // Domain 集約全体にアクセスできてしまう
}
```

### After

```csharp
// View 専用: 表示に必要な値だけを返す DataProvider
public readonly struct EntityViewData
{
    public Guid Id { get; init; }
    public float HpRatio { get; init; }
    public string StateLabel { get; init; }
}

public interface IEntityViewDataProvider
{
    IReadOnlyList<EntityViewData> GetViewData();
}

// Query が Domain → DTO 変換を担う
public sealed class EntityStatusQuery
{
    public EntityStatusView GetView(Entity entity)
    {
        var foo = entity.Behavior as FooBehavior;
        return new EntityStatusView
        {
            HpRatio = entity.Params.MaxHp > 0 ? (float)entity.CurrentHp / entity.Params.MaxHp : 0f,
            StateLabel = foo?.State.ToString() ?? "Unknown",
        };
    }
}

// Presenter は DTO だけを受け取る
public sealed class EntityStatusPresenter
{
    public void UpdateView(EntityStatusView view)
    {
        hpBar.value = view.HpRatio;
        stateLabel.text = view.StateLabel;
    }
}
```

### レビュー観点

- View / Presenter が Application 内部用の広い Reader を直接受け取っていないか
- `entity.Behavior is XxxBehavior` のような型判断が View 層に入っていないか
- 表示に必要な情報が DTO / Query として明示されているか

### DungeonInn Example

`WorldGameLogPresenter` が `IGameWorldStateReader` 経由で `Guild` / `Actor` / `Inventory` を直接読み、
表示文字列を組み立てている。専用の Query クラスに変換ロジックを移し、Presenter をシンプルに保つ。

---

## 13. Unity EntryPoint にゲーム進行順序を置かない

`MonoBehaviour` が多数の UseCase を直接注入し、`Update()` にゲーム進行の詳細な順序を書くと、
View 層が Application の orchestration を握ることになる。

### Before

```csharp
public sealed class GameLoopEntryPoint : MonoBehaviour
{
    SpawnUseCase spawnUseCase;
    AdvanceSimulationUseCase simulationUseCase;
    SellItemsUseCase sellItemsUseCase;  // UseCase が増えるたびに EntryPoint が肥大化

    async UniTask TickAsync(float deltaTime)
    {
        spawnUseCase.Execute();
        simulationUseCase.Execute();
        sellItemsUseCase.Execute();     // 順序が View 層に書かれている
    }
}
```

### After

```csharp
public sealed class GameLoopEntryPoint : MonoBehaviour
{
    IAdvanceWorldFrameUseCase advanceWorldFrame;

    async UniTask TickAsync(float deltaTime)
    {
        await advanceWorldFrame.ExecuteAsync(new WorldFrameRequest(deltaTime, ct));
        viewUpdater.UpdateVisuals();
    }
}
```

### レビュー観点

- MonoBehaviour が多数の UseCase / Service を直接注入していないか
- `Update()` / `TickAsync()` 内にゲーム進行の詳細な順序が書かれていないか
- Application 層に「1フレーム進める」責務が存在するか

---

## 14. ゲームループ処理を実行トリガーで分類する

ゲームループに処理が増えるにつれて「毎フレーム処理なのか」「イベントトリガーなのか」が不明瞭になる。
毎フレーム呼ばれるクラスで GC Alloc が発生すると影響が大きい。

処理の実行契機を設計時に以下の3種類に分類し、ループエントリポイントでコメントとして明記する。

| 種別 | 例 | 実行契機 |
|---|---|---|
| Frame Loop | 移動補間、物理更新、戦闘進行 | 毎フレーム必須 |
| Schedule Tick | 日次処理、定期生成、時間経過回復 | ゲーム内 tick 単位 |
| Event-Driven | 装備更新、価格再計算、UI 再集計 | 状態変更イベント / dirty flag |

`Frame Loop` に分類される処理は GC Alloc・LINQ 呼び出し・コレクション生成を原則禁止とする。

```csharp
async UniTask TickAsync(float deltaTime)
{
    // [FrameLoop] 毎フレーム必要な処理
    await advanceSimulationUseCase.ExecuteAsync(state, deltaTime);

    // [ScheduleTick] ゲーム内 tick 単位で十分。将来的に条件で絞る候補
    await sellItemsUseCase.Execute(state);

    // [EventDriven] 状態変更イベントで dirty になったときのみ実行候補
    updateEquipmentUseCase.Execute(state);
}
```

### DungeonInn Example

`SellItemsUseCase.Execute()` は現在 `TickAsync` 内で毎フレーム呼ばれているが、
内部でコレクション生成が発生している。`ScheduleTick` または `EventDriven` へ移動することで改善できる。

---

## 15. ゲームループ内の状態変更は「決定フェーズ」と「適用フェーズ」を分ける

複数の UseCase を順番に呼ぶゲームループでは、あるフェーズで発行されたイベントを
別のフェーズの UseCase が購読して状態を変えると、処理順が実行結果を左右する。

### Before

```csharp
// TickAsync 内
await detectCombatEncounterUseCase.ExecuteAsync(worldState);
// ↑ この中で ActorCombatEncounterStarted を発行 →
//   DecideAdventurerReturnUseCase の購読コールバックが即座に Actor 状態を変更
await advanceCombatUseCase.ExecuteAsync(worldState, delta);
// 結果が DetectCombatEncounterUseCase の実行タイミングに依存する
```

### After

```csharp
// フェーズ 1: 状態を読んで「何をするか」を決定する（状態変更なし）
var encounterDecisions = detectCombatEncounterUseCase.CollectDecisions(worldState);
var aiDecisions = advanceActorAiUseCase.CollectDecisions(worldState);

// フェーズ 2: 決定を適用する（すべての決定が揃ってから状態を変更する）
applyCombatEncounterUseCase.Apply(worldState, encounterDecisions);
applyAiDecisionUseCase.Apply(worldState, aiDecisions);

// フェーズ 3: 状態変更後にイベントを発行する
eventBus.Publish(collectedEvents);
```

### 適用基準

- ゲームループ内の UseCase から発行されたイベントの購読コールバックで、状態を変更している箇所は設計負債として記録する
- 新しくゲームループに追加する処理は、入力（状態の読み取り）と出力（状態の変更）を分けて設計する

---

## 16. パフォーマンス敏感経路での LINQ / GC Alloc を禁止する

毎フレーム・Entity 数比例・アイテム数比例の経路で LINQ chain や防御的コピーを使うと、
GC Alloc や CPU コストがデータ数に比例して積み上がる。

`Frame Loop` / `Entity Loop` に分類される経路では、以下を禁止する。

- `ToArray()` / `ToList()` による配列生成
- `Where()` / `Select()` / `GroupBy()` / `OrderBy()` 等の LINQ chain
- 防御的コピー（`new List<T>(existing)` 等）

代わりに loop・再利用バッファ・`Span<T>`・差分更新を使う。

```csharp
// Before: LINQ で毎フレーム GC Alloc が発生
var selected = candidates
    .Where(c => c.Score <= threshold)
    .OrderByDescending(c => c.Priority)
    .FirstOrDefault();

// After: loop で手動探索し、GC Alloc なし
T selected = null;
foreach (var c in candidates)
{
    if (c.Score > threshold) continue;
    if (selected == null || c.Priority > selected.Priority)
        selected = c;
}
```

### DungeonInn Example

ループ内で `floors.Where(...).OrderByDescending(...).FirstOrDefault()` や `slots.ToList()` が
呼ばれており、毎フレーム GC Alloc が発生している。

---

## レビュー用チェックリスト

### UseCase の責務

- [ ] UseCase のコンストラクタに他の UseCase が注入されていないか
- [ ] UseCase が `IDisposable` を実装している場合、Service への昇格を検討したか
- [ ] UseCase のコンストラクタでイベント購読を行っていないか

### イベントと状態変更

- [ ] イベントに View 用の表示文字列・加工済み値が含まれていないか
- [ ] イベントにドメインオブジェクトをそのまま渡せているか
- [ ] イベント生成のためにマスタリポジトリ・View サービスへの依存が増えていないか
- [ ] イベント購読のコールバック内で Domain オブジェクトのメソッドや `worldState` を変更していないか
- [ ] ゲームの状態変更は UseCase の `Execute` / `ExecuteAsync` 内で完結しているか
- [ ] イベントを発行している箇所は、状態変更後（トランザクション完了後）か

### Entity 削除経路

- [ ] Entity 除去 UseCase / Handler を追加したとき、関連する全 Service の cleanup を確認したか
- [ ] `XxxStateService` を追加したとき、どのイベントで cleanup するかを列挙したか

### 集約境界

- [ ] `public ActorEquipment Equipment { get; }` のように可変オブジェクトをそのまま公開していないか
- [ ] 外部コードが集約を迂回して状態を変えていないか
- [ ] 状態遷移メソッド（`Start()`, `Complete()`, `Cancel()` 等）に事前状態チェックが入っているか
- [ ] 不正な遷移時に例外を投げるか、少なくともログに残るようになっているか

### 静的依存

- [ ] Domain Entity のコンストラクタやメソッド内に `Xxx.Instance` / `XxxCatalog.Get()` のような static アクセスがないか

### インターフェース分離

- [ ] `IGameEventBus` を UseCase に注入している場合、`IEventPublisher` だけで十分でないか
- [ ] `IGameWorldState` を UseCase に渡している場合、読み取りだけなら `IGameWorldStateReader` にできないか
- [ ] View / Presenter が Application 内部用の広い Reader を直接受け取っていないか

### ゲームループ

- [ ] MonoBehaviour に UseCase の呼び出し順序が書かれていないか
- [ ] 設計・DI 登録済みの処理が Orchestrator / FrameUseCase 経由で実行パイプラインに実際に接続されているか
- [ ] ゲームループに追加する処理に実行種別コメントを付けたか（`// [FrameLoop]` 等）
- [ ] `Frame Loop` に分類した処理で LINQ / `ToList()` / コレクション生成が発生していないか
- [ ] ゲームループ内の UseCase から発行されたイベントを、同一ループ内の別 UseCase が購読して状態変更していないか
