# UseCase / イベント / 集約境界 設計ガイドライン

このドキュメントは、Milestone 4 までの設計レビューで発見された問題を抽象化し、再発防止のルールとしてまとめる。
`domain-usecase-design-guidelines.md` の補足として、UseCase の責務範囲・イベントの正しい使い方・集約境界の守り方を扱う。

---

## 1. UseCase は他の UseCase を呼ばない

1つの UseCase 実行が1つのトランザクションである原則（`domain-usecase-design-guidelines.md` §10）に照らし、
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

呼び出し元は `Resolve()` を呼んだだけで、経験値付与・アイテムドロップ・Actor 削除が連鎖して実行される。
処理の順序や整合性が UseCase 間の呼び出し順に依存し、変更時の影響範囲が読めない。

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

// 呼び出し元の UseCase が「何を・どの順で行うか」を明示的に並べる
public sealed class HandleActorDefeatUseCase
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
- UseCase 間の処理順序を表現したい場合は、上位 UseCase（Orchestrating UseCase）を1つ作り、そこに順序を書く
- 「これは UseCase ではなく Service だから呼んでよい」とならないよう、UseCase を注入しているかどうかで判断する

---

## 2. UseCase はステートレスに保つ

UseCase はリクエストを受け取り、Domain を操作し、イベントを発行して終わる単方向の処理単位である。
イベントを購読して内部状態（Dictionary、HashSet 等）を管理するクラスは UseCase ではなく Service であり、
LifetimeScope 上の寿命管理と責務の区別を明確にしなければならない。

### Before

```csharp
// UseCase のはずが、IDisposable を実装してイベント購読と状態管理を行っている
public sealed class RecoverAdventurerAtInnUseCase : IDisposable
{
    readonly Dictionary<Guid, float> accumulatedHp = new();  // NG: UseCase が状態を持つ
    readonly IDisposable deathSubscription;

    public RecoverAdventurerAtInnUseCase(IGameEventBus eventBus)
    {
        deathSubscription = eventBus.OnEvent<ActorDefeated>()
            .Subscribe(evt => accumulatedHp.Remove(evt.ActorId));  // NG: UseCase がイベントを購読する
    }

    public void Dispose() => deathSubscription.Dispose();
}
```

UseCase が長期間生存してイベントを購読し続けると、複数インスタンスが生成されたときに
同じイベントへの重複購読が起こる。UseCase の寿命がシーンと結びついているにもかかわらず、
状態がそれより長く（あるいは短く）生きるリスクがある。

### After

```csharp
// 長期状態は専用の Service に分離し、LifetimeScope で寿命を管理する
public sealed class AdventurerRecoveryStateService : IDisposable
{
    readonly Dictionary<Guid, float> accumulatedHp = new();
    readonly IDisposable subscription;

    public AdventurerRecoveryStateService(IGameEventBus eventBus)
    {
        subscription = eventBus.OnEvent<ActorDefeated>()
            .Subscribe(evt => accumulatedHp.Remove(evt.ActorId));
    }

    public float GetAccumulatedHp(Guid actorId) =>
        accumulatedHp.TryGetValue(actorId, out var v) ? v : 0f;

    public void SetAccumulatedHp(Guid actorId, float value) =>
        accumulatedHp[actorId] = value;

    public void Dispose() => subscription.Dispose();
}

// UseCase はステートレスに戻す
public sealed class RecoverAdventurerAtInnUseCase
{
    readonly AdventurerRecoveryStateService recoveryState;

    public async UniTask ExecuteAsync(
        IGameWorldState worldState,
        Actor adventurer,
        float deltaGameSeconds)
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
- VContainer の `Lifetime.Scoped` / `Lifetime.Singleton` はシーンや Service の寿命で決め、UseCase 自体は Transient または都度生成で使えるように保つ

---

## 3. イベント購読者はゲームの状態を変更しない

`game-event-design.md` の方針に従い、イベント購読者はゲームの状態を変更しない。
しかし、UseCase が購読の中で Actor の状態を変更しているケースが発生している。

購読者が状態を変更すると、どの順序でイベントが処理されるかによって最終的な状態が変わる可能性がある。
「なぜこの状態になったか」をトレースするには、UseCase だけでなく全購読者を追う必要が生じる。

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
                defeatedMonsterCountsByActor[evt.KillerActorId].IncrementKill(evt.ActorId);
                dirtyActorIds.Add(evt.KillerActorId);  // 状態変更
            });
    }

    public async UniTask ExecuteAsync(IGameWorldState worldState)
    {
        foreach (var actorId in dirtyActorIds)
        {
            var actor = worldState.FindActor(actorId);
            actor.CurrentGoal.SetProgress(...);  // NG: 購読によって蓄積した状態を使って Actor を変更
        }
    }
}
```

### After

```csharp
// 戦闘履歴の蓄積は Service に分離（ §2 のルールと組み合わせる）
public sealed class ActorCombatHistoryService : IDisposable
{
    readonly Dictionary<Guid, CombatHistory> histories = new();
    readonly IDisposable subscription;

    public ActorCombatHistoryService(IGameEventBus eventBus)
    {
        subscription = eventBus.OnEvent<ActorDefeated>()
            .Subscribe(evt => RecordKill(evt.KillerActorId, evt.ActorId));
            // 購読内ではログとして記録するだけ。Actor への書き込みはしない
    }

    public CombatHistory GetHistory(Guid actorId) =>
        histories.TryGetValue(actorId, out var h) ? h : CombatHistory.Empty;

    public void Dispose() => subscription.Dispose();
}

// UseCase はヒストリを参照して、1回のトランザクションとして判断と変更を完結させる
public sealed class EvaluateAdventurerReturnUseCase
{
    readonly ActorCombatHistoryService historyService;

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

## 4. 集約内部の可変オブジェクトを直接公開しない

集約（Aggregate）は不変条件を守る責任を持つ。
内部に持つ `Equipment`、`Inventory`、`Stats` などの可変オブジェクトを `public get` で公開すると、
外部から集約を迂回して状態を書き換えることができてしまい、キャッシュ無効化や不変条件の維持が崩れる。

### Before

```csharp
public sealed class Actor
{
    public ActorEquipment Equipment { get; }  // 外部から Equipment.Equip() を呼べる
    public Inventory Inventory { get; }       // 外部から Inventory.Remove() を呼べる
}

// 外部コード（UseCase など）で:
actor.Equipment.Equip(master);  // NG: Actor.RefreshParams() が呼ばれない
actor.Inventory.Remove(stack);  // NG: Actor 側のキャッシュが更新されない
```

`ActorParams` のキャッシュが装備変更を検知できず、古い攻撃力でダメージ計算が続く。
どこで装備変更が起きたか追跡するには、`ActorEquipment` の内部も読む必要がある。

### After

```csharp
public sealed class Actor
{
    ActorEquipment equipment;
    Inventory inventory;

    // 読み取りは読み取り専用インターフェースを返す
    public IReadOnlyActorEquipment Equipment => equipment;
    public IReadOnlyInventory Inventory => inventory;

    // 書き込みは Actor を経由させ、キャッシュ更新を一元管理する
    public void Equip(EquipmentMaster master)
    {
        equipment.Equip(master);
        RefreshParams();  // Actor が責任を持って同期させる
    }

    public void GainItem(ItemStack stack)
    {
        inventory.Add(stack);
    }
}
```

### 適用基準

- `public ActorEquipment Equipment { get; }` のように、Setter なしでも可変オブジェクトを返すプロパティは集約境界の漏れとみなす
- 読み取り専用インターフェース（`IReadOnlyXxx`）を作ることで、参照は渡しつつ書き込みは封じる
- `RefreshParams()` のような整合性維持メソッドが `public` になっている場合、外部から呼ぶことを前提としている設計が問われる。理想は `private` にして、書き込み経路内で自動的に呼ぶ

---

## 5. 状態遷移を持つドメインオブジェクトは不正遷移を禁止する

`ActorAction`、`ActorGoal` のような状態遷移を内包するドメインオブジェクトは、
不正な遷移をオブジェクト自身が防がなければならない。
外部の UseCase が遷移の妥当性を毎回確認する設計では、確認漏れが即バグになる。

### Before

```csharp
public sealed class ActorAction
{
    public ActorActionState State { get; private set; } = ActorActionState.NotStarted;

    public void Start()
    {
        State = ActorActionState.Running;  // NG: NotStarted でない状態から Start() しても通る
    }

    public void Complete()
    {
        State = ActorActionState.Completed;  // NG: Running でない状態から Complete() しても通る
    }
}
```

`Running` 状態のアクションに再度 `Start()` を呼んでも例外が出ない。
誤って二重 Start したときの挙動が不定になる。

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

    public void Complete()
    {
        if (State != ActorActionState.Running)
            throw new InvalidOperationException(
                $"Cannot complete action in {State} state. Expected: {ActorActionState.Running}");

        State = ActorActionState.Completed;
    }
}
```

### 適用基準

- 「状態」を持ち、特定の順序で呼ばれることが前提のメソッドには、事前状態チェックを必ず入れる
- チェックは「想定外の状態を防ぐ」ためのものであり、ゲームバランス判断ではない。Domain Validation の対象
- InvalidOperationException のメッセージには「現在の状態」と「期待する状態」を両方含めると原因特定が早い

---

## 6. Domain Entity が static クラスに依存しない

Domain Entity は DI コンテナから注入された依存のみを使う。
static クラスへのアクセス（Service Locator）は、テスト時に差し替えができず、
暗黙のグローバル依存を生む。

### Before

```csharp
public sealed class Actor
{
    public Actor(...)
    {
        // NG: static クラスに直接アクセス。テスト時に差し替え不可
        NaturalWeaponTypeCombatMaster = WeaponTypeCombatMasterCatalog.Get(NaturalWeaponType);
    }

    public void ChangeNaturalWeaponType(WeaponType weaponType)
    {
        var master = WeaponTypeCombatMasterCatalog.Get(weaponType);  // NG: 同様
        // ...
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

    public void ChangeNaturalWeaponType(WeaponType weaponType)
    {
        var master = weaponMasters.GetCombatMaster(weaponType);
        // ...
    }
}
```

ただし、Actor はコンストラクタ引数が多いため、Factory 経由での生成に統一することで引数増加の影響を局所化できる。

### 適用基準

- Domain 層のクラスで `static` なクラス（Catalog、Registry、Locator 等）を呼んでいる箇所はすべて対象
- 「読み取り専用データなので static でよい」という理由は DI の観点では認められない。テスト容易性のために注入する
- static であることが避けられない場合（`Math` クラス等）は対象外

---

## 7. IEventPublisher と IEventSubscriber を分離する

`IGameEventBus` は Publish と Subscribe の両方を持つが、大多数のクラスはどちらか一方しか必要としない。
両方を持つインターフェースを全クラスに渡すと、「Publish しかしないはずのクラスが Subscribe できてしまう」
という意図しない使われ方が発生する。

### Before

```csharp
public interface IGameEventBus
{
    void Publish(IGameEvent gameEvent);
    Observable<T> OnEvent<T>() where T : class, IGameEvent;
}

// CombatEffectExecutor は Publish だけすればよいが Subscribe 能力も持ってしまう
public sealed class CombatEffectExecutor
{
    readonly IGameEventBus eventBus;  // Subscribe も可能な型
}
```

### After

```csharp
public interface IEventPublisher
{
    void Publish(IGameEvent gameEvent);
}

public interface IEventSubscriber
{
    Observable<T> OnEvent<T>() where T : class, IGameEvent;
}

// IGameEventBus は両方を継承した型として残し、登録・解決に使う
public interface IGameEventBus : IEventPublisher, IEventSubscriber { }

// Publish しか使わないクラスには IEventPublisher だけを渡す
public sealed class CombatEffectExecutor
{
    readonly IEventPublisher eventPublisher;  // Subscribe を誤って呼べない

    public CombatEffectExecutor(IEventPublisher eventPublisher)
    {
        this.eventPublisher = eventPublisher;
    }
}

// Subscribe だけを使うクラスには IEventSubscriber だけを渡す
public sealed class AdventurerRecoveryStateService
{
    readonly IEventSubscriber eventSubscriber;

    public AdventurerRecoveryStateService(IEventSubscriber eventSubscriber)
    {
        eventSubscriber.OnEvent<ActorDefeated>().Subscribe(...);
    }
}
```

### 適用基準

- UseCase はほぼ全て `IEventPublisher` のみで十分
- Service / Aggregator 系は `IEventSubscriber` のみ（自分の内部で Publish することは原則ない）
- View / Presenter は `IEventSubscriber` のみ
- `IGameEventBus` を直接注入するのは、LifetimeScope の登録や、Publisher と Subscriber の両方が必要な稀なケースのみ

---

## 8. IGameWorldState は読み書きでインターフェースを分ける

`IGameWorldState` に読み取りと書き込みのメソッドが混在すると、
「読むだけのはずの UseCase」が誤って書き込むことを型で防げない。

### Before

```csharp
public interface IGameWorldState
{
    IReadOnlyList<Actor> Actors { get; }
    Actor FindActor(Guid actorId);    // 読み取り
    void RegisterActor(Actor actor);  // 書き込み（同一インターフェース）
    bool RemoveActor(Guid actorId);   // 書き込み
    void AddItem(ItemInstance item);  // 書き込み
}

// DetectCombatEncounterUseCase は読み取りだけでよいが、書き込みメソッドも見えてしまう
public sealed class DetectCombatEncounterUseCase
{
    public async UniTask ExecuteAsync(IGameWorldState worldState) { ... }
}
```

### After

```csharp
public interface IGameWorldStateReader
{
    IReadOnlyList<Actor> Actors { get; }
    Actor FindActor(Guid actorId);
    IReadOnlyList<ItemInstance> Items { get; }
    // ... 読み取りのみ ...
}

public interface IGameWorldStateWriter
{
    void RegisterActor(Actor actor);
    bool RemoveActor(Guid actorId);
    void AddItem(ItemInstance item);
    // ... 書き込みのみ ...
}

public interface IGameWorldState : IGameWorldStateReader, IGameWorldStateWriter { }

// 読むだけの UseCase には Reader だけを渡す
public sealed class DetectCombatEncounterUseCase
{
    public async UniTask ExecuteAsync(IGameWorldStateReader worldState) { ... }
}

// 書き込みも行う UseCase には IGameWorldState を渡す
public sealed class SpawnAdventurerUseCase
{
    public async UniTask ExecuteAsync(IGameWorldState worldState) { ... }
}
```

### 適用基準

- `worldState.Actors` を参照するだけの UseCase は `IGameWorldStateReader` を受け取る
- 引数型を見るだけで「このクラスは状態を変えるか」が分かる設計が目標
- 今後追加する UseCase は、まず `IGameWorldStateReader` で書き始め、書き込みが必要になったときのみ `IGameWorldState` に変更する

---

## 9. ゲームループ内の状態変更は「決定フェーズ」と「適用フェーズ」を分ける

複数の UseCase を順番に呼ぶゲームループでは、あるフェーズで発行されたイベントを
別のフェーズの UseCase が購読して状態を変えると、処理順が実行結果を左右する。

状態変更の順序が暗黙のイベント購読順に依存している設計は、将来的に購読者が増えるたびに
「どの順序で呼ばれるか」の追跡が必要になる。

### Before

```csharp
// WorldGameLoopEntryPoint の TickAsync
await detectCombatEncounterUseCase.ExecuteAsync(worldState);   // イベントを発行
// ↑ この中で ActorCombatEncounterStarted を発行 →
//   DecideAdventurerReturnUseCase の購読コールバックが即座に Actor 状態を変更
await advanceCombatUseCase.ExecuteAsync(worldState, delta);    // 変更済み状態を読む
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

完全にこの構造に揃えるのは大規模な変更になるため、まずは「ループ内で発行されたイベントを購読して
状態を変えている箇所」を特定し、その購読をゲームループの明示的なフェーズに移すことから始める。

### 適用基準

- ゲームループ内の UseCase から発行されたイベントの購読コールバックで、Actor・WorldState の状態を変更している箇所は設計負債として記録する
- 新しくゲームループに追加する処理は、入力（状態の読み取り）と出力（状態の変更）を分けて設計する
- テスト時に「購読の順序を変えたら結果が変わる」ケースがあれば、このルールの対象として設計を見直す

---

## レビュー用チェックリスト

### UseCase の責務

- [ ] UseCase のコンストラクタに他の UseCase が注入されていないか
- [ ] Resolver / Handler / Service という名前でも、UseCase を注入して連鎖呼び出ししていないか
- [ ] UseCase が `IDisposable` を実装している場合、それは本当に Service であるべきでないか
- [ ] UseCase のコンストラクタでイベント購読を行っていないか

### イベントと状態変更

- [ ] イベント購読のコールバック内で Domain オブジェクトのメソッドや `worldState` を変更していないか
- [ ] ゲームの状態変更は UseCase の `Execute` / `ExecuteAsync` 内で完結しているか
- [ ] イベントを発行している箇所は、状態変更後（トランザクション完了後）か

### 集約境界

- [ ] `public ActorEquipment Equipment { get; }` のように可変オブジェクトをそのまま公開していないか
- [ ] 外部コードが `actor.Equipment.Equip()` のように集約を迂回して状態を変えていないか
- [ ] `RefreshParams()` のようなキャッシュ更新メソッドが `public` になっている場合、内部化できないか

### ドメインオブジェクトの状態遷移

- [ ] 状態遷移メソッド（`Start()`, `Complete()`, `Cancel()` 等）に事前状態チェックが入っているか
- [ ] 不正な遷移時に例外を投げるか、少なくともログに残るようになっているか

### 静的依存

- [ ] Domain Entity のコンストラクタやメソッド内に `Xxx.Instance` / `XxxCatalog.Get()` のような static アクセスがないか

### インターフェース分離

- [ ] `IGameEventBus` を UseCase に注入している場合、`IEventPublisher` だけで十分でないか
- [ ] `IGameWorldState` を UseCase に渡している場合、読み取りだけなら `IGameWorldStateReader` にできないか
- [ ] `IActorCombatService` のような広いインターフェースで、実際に使うメソッドが 3 つ以下の場合、インターフェース分割を検討する

### ゲームループの順序依存

- [ ] ゲームループ内の UseCase から発行されたイベントを、同一ループ内の別 UseCase が購読して状態変更していないか
- [ ] 購読者の順序を変えた場合に結果が変わるテストケースが存在しないか
