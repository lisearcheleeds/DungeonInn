# Code Review Policy Candidate - Codex

作成日: 2026-05-13

このドキュメントは、DungeonInn Milestone 5 完了前レビュー第2回で見つかった問題を、他プロジェクトでも使える恒久的なレビュー観点として抽象化した候補リストである。

正式採用前の候補であり、採用する項目は `review-policy-guideline.md`、`usecase-boundary-guidelines.md`、`domain-usecase-design-guidelines.md`、`coding-rules.md` などへ分割して反映する。

---

## 1. Mutable Child Object を Aggregate 外へ公開しない

### 問題

Aggregate root が内部に持つ可変オブジェクトを public getter でそのまま公開すると、外部の UseCase / Service が Aggregate の不変条件を迂回して状態を変更できる。

例:

```csharp
public sealed class Actor
{
    public Inventory Inventory { get; }
}

actor.Inventory.Remove(stack);
actor.Inventory.TrySpendGold(price);
```

この形では、Inventory 変更に合わせて必要な cache 更新、event 発行、装備や派生値との整合性維持が漏れやすい。

### 方針

Aggregate root の外へ公開する child object は、原則として読み取り専用 interface にする。状態変更は Aggregate root のメソッドとして表現し、変更に伴う整合性維持を root に集約する。

例:

```csharp
public sealed class Actor
{
    readonly Inventory inventory;

    public IReadOnlyInventory Inventory => inventory;

    public bool TrySpendGold(int amount)
    {
        return inventory.TrySpendGold(amount);
    }

    public void GainItem(ItemStack stack)
    {
        inventory.Add(stack);
        RefreshItemDependentState();
    }
}
```

### レビュー観点

- `public Inventory Inventory { get; }` のような可変 child object の直接公開がないか。
- 外部コードが `actor.Inventory.Add()` / `actor.Inventory.Remove()` のように Aggregate 内部を直接変更していないか。
- child object の変更時に必要な cache 更新や event 発行が root 経由に集約されているか。

---

## 2. View 用 Reader と Application 内部 Reader を分ける

### 問題

View が Application 内部用の広い Reader を直接参照すると、表示層から Domain 集約全体へアクセスできる。これは View が表示に不要なビジネス情報へ依存する入口になる。

例:

```csharp
public interface IGameWorldStateReader
{
    AdventurerGuild Guild { get; }
    Dungeon Dungeon { get; }
    IReadOnlyList<Actor> Actors { get; }
}

public sealed class WorldMapView
{
    readonly IGameWorldStateReader worldState;
}
```

### 方針

View には表示目的ごとの Query / DTO / DataProvider を渡す。Application 内部の読み取り interface と View 用の読み取り interface は分ける。

例:

```csharp
public readonly struct ActorViewData
{
    public Guid ActorId { get; }
    public LayerPosition Position { get; }
    public ActorVisualRole VisualRole { get; }
    public ActorVisualState VisualState { get; }
}

public interface IActorViewDataProvider
{
    IReadOnlyList<ActorViewData> GetActorViewData();
}
```

### レビュー観点

- View / Presenter が Domain 集約全体を読める interface を直接受け取っていないか。
- View が UI 表示や描画に不要な Domain 情報を参照していないか。
- 表示に必要な情報が DTO / Query として明示されているか。

---

## 3. Unity EntryPoint に Game Simulation の順序を置かない

### 問題

`MonoBehaviour.Update()` や scene entry point が多数の UseCase を直接注入し、ゲーム進行順序を固定すると、View 層が Application の orchestration を握ることになる。

例:

```csharp
public sealed class WorldGameLoopEntryPoint : MonoBehaviour
{
    SpawnUseCase spawnUseCase;
    AdvanceCombatUseCase combatUseCase;
    SellItemsUseCase sellItemsUseCase;

    void Update()
    {
        spawnUseCase.Execute();
        combatUseCase.Execute();
        sellItemsUseCase.Execute();
    }
}
```

### 方針

Unity entry point は lifecycle、delta time、cancel token、View 更新の入口に限定する。ゲーム進行順序は Application 層の Orchestrator / UseCase に置く。

例:

```csharp
public sealed class WorldGameLoopEntryPoint : MonoBehaviour
{
    IAdvanceWorldFrameUseCase advanceWorldFrameUseCase;

    void Update()
    {
        advanceWorldFrameUseCase.Execute(new WorldFrameRequest(Time.deltaTime));
        worldViewUpdater.UpdateVisuals();
    }
}
```

### レビュー観点

- MonoBehaviour が多数の UseCase / Service を直接注入していないか。
- `Update()` 内にゲーム進行の詳細な順序が書かれていないか。
- Application 層に「1フレーム進める」責務が存在するか。

---

## 4. Frame Loop / Schedule Tick / Event-Driven 処理を混ぜない

### 問題

毎フレーム必要な処理、ゲーム内時刻 tick で十分な処理、状態変化時だけ必要な処理を同じ loop で実行すると、Actor 数や Item 数が増えた時に不要な処理が積み上がる。

例:

```csharp
void Update()
{
    AdvanceCombat(deltaTime);       // frame loop
    UpdateEquipment();              // inventory dirty で十分
    SellItems();                    // schedule tick / event で十分
    DecideReturnToInn();            // actor dirty で十分
}
```

### 方針

処理の実行契機を設計時に分類する。

| 種別 | 例 | 実行契機 |
|---|---|---|
| Frame Loop | 移動補間、戦闘進行、projectile | 毎フレーム |
| Schedule Tick | 日次処理、定期 spawn、宿回復 | ゲーム内 tick |
| Event-Driven / Dirty | 装備更新、売却、帰還判定、UI 再集計 | 状態変更 event / dirty flag |

### レビュー観点

- 毎フレーム処理に schedule tick で十分な処理が混ざっていないか。
- Actor / Item / Inventory 数に比例する処理が毎フレーム無条件に走っていないか。
- dirty flag や event によって再計算対象を限定できないか。

---

## 5. Actor-keyed State は削除イベントで必ず cleanup する

### 問題

`Dictionary<ActorId, State>` を持つ Service が複数ある場合、Actor 削除時の cleanup が漏れると、存在しない Actor の状態が残り続ける。

例:

```csharp
public sealed class ActorRecoveryStateService
{
    readonly Dictionary<Guid, float> accumulatedHp = new();
}
```

死亡時だけ cleanup していても、帰還、despawn、scene unload など別の削除経路で漏れる可能性がある。

### 方針

Actor が world から除去される全経路を表す統一イベント、または cleanup contract を用意する。Actor-keyed state を持つ Service は必ずその contract に従う。

例:

```csharp
public sealed class ActorRecoveryStateService : IDisposable
{
    readonly Dictionary<Guid, float> accumulatedHp = new();

    public ActorRecoveryStateService(IEventSubscriber eventSubscriber)
    {
        eventSubscriber.OnEvent<ActorRemoved>()
            .Subscribe(gameEvent => accumulatedHp.Remove(gameEvent.ActorId));
    }
}
```

### レビュー観点

- `Dictionary<Guid, ...>` や `HashSet<Guid>` を持つ長期 Service が Actor 削除時に cleanup されるか。
- cleanup が特定イベントだけに偏っていないか。
- Actor の削除経路が一覧化されているか。

---

## 6. Runtime State に Master / Spec の不変値をコピーしない

### 問題

Runtime state に master / spec 由来の不変値をコピーすると、どちらが正典か分かりにくくなる。master の変更や再付与ポリシーの扱いも曖昧になる。

例:

```csharp
public sealed class ActiveStatusEffect
{
    public StatusEffectType Type { get; }
    public int Amount { get; private set; }
    public float DurationSeconds { get; private set; }
    public float TickIntervalSeconds { get; }
    public float ElapsedSeconds { get; private set; }
}
```

`Type`、`Amount`、`DurationSeconds`、`TickIntervalSeconds` は spec 由来であり、`ElapsedSeconds` は runtime state である。

### 方針

Runtime state は可変状態だけを持つ。不変値は master id、spec id、spec 参照、または resolver 経由で取得する。再付与で変化する値だけ runtime 側へ明示的に持たせる。

例:

```csharp
public sealed class ActiveStatusEffectState
{
    public int StatusEffectSpecId { get; }
    public float ElapsedSeconds { get; private set; }
    public int AppliedAmount { get; private set; }
}
```

### レビュー観点

- runtime object に master / spec から O(1) で解決できる値をコピーしていないか。
- コピーしている値が「履歴」「個体差」「再付与で変化する値」などの例外に該当するか。
- 例外として持つ場合、その理由が名前やコメントで明確か。

---

## 7. DTO 重複は「現在値」「履歴」「集計途中」の責務で分ける

### 問題

似たフィールドを持つ DTO が複数存在すると、どれが正典か分かりにくくなる。詰め替えコードが増え、片方だけ更新されるリスクもある。

例:

```csharp
public readonly struct EconomyStatus
{
    public int GuestsToday { get; }
    public int SalesToday { get; }
    public int Reputation { get; }
}

public readonly struct DailyEconomyReport
{
    public int Guests { get; }
    public int Sales { get; }
    public int Reputation { get; }
}
```

### 方針

DTO は責務で分ける。

| 種別 | 役割 |
|---|---|
| Current Status | 現在表示する状態 |
| Report / History | 過去の記録として保存する不変データ |
| Statistics / Accumulator | 集計途中の内部状態 |
| Summary | Status と Report の共通値 |

共通フィールドが多い場合は、共通の `Summary` 値型を切り出すか、status が report を内包する。

### レビュー観点

- 同じ意味のフィールドが複数 DTO に重複していないか。
- DTO 間の詰め替えが増えていないか。
- 現在値、履歴、集計途中の責務が名前と構造で分かるか。

---

## 8. Static Catalog / Registry を Domain から直接呼ばない

### 問題

Domain entity / domain service が static catalog や registry を直接呼ぶと、暗黙のグローバル依存が生まれる。テスト時の差し替えも難しくなる。

例:

```csharp
public sealed class WeaponCombatCalculatorFactory
{
    public static IWeaponCombatCalculator Create(WeaponType weaponType)
    {
        return Create(WeaponTypeCombatMasterCatalog.Get(weaponType));
    }
}
```

### 方針

Domain には必要な master / spec / calculator を外から渡す。master 解決は Factory / Repository / Application service の境界で行う。

例:

```csharp
public sealed class WeaponCombatCalculatorFactory
{
    public IWeaponCombatCalculator Create(WeaponTypeCombatMaster master)
    {
        return new DirectWeaponCombatCalculator(master);
    }
}
```

### レビュー観点

- Domain 配下の class が `XxxCatalog.Get()`、`XxxRegistry.Instance`、static locator を呼んでいないか。
- master 解決が DI / Repository 経由で差し替え可能か。
- static を使う場合、それが純粋関数的 utility であり、データ取得や状態を持たないか。

---

## 9. Event は Transaction 完了後に publish する

### 問題

UseCase の途中で event を publish すると、購読者から見た状態がまだ確定していない可能性がある。特に damage、defeat、drop、reward のように複数状態変更が連鎖する処理では、event の順序が仕様になる。

例:

```csharp
eventPublisher.Publish(new ProjectileHit(projectile.Id, target.Id, damage));
target.ReceiveDamage(damage);
if (target.Hp <= 0)
{
    defeatResolver.Resolve(target);
}
```

### 方針

Event は「起きた事実」を、関連する状態変更が完了した後に publish する。複数 event が発生する場合は一旦収集し、transaction の最後で順序を明示して publish する。

例:

```csharp
var events = new List<IGameEvent>();
target.ReceiveDamage(damage);
events.Add(new ProjectileHit(projectile.Id, target.Id, damage));

if (target.Hp <= 0)
{
    defeatResolver.Resolve(target, events);
}

eventPublisher.PublishAll(events);
```

### レビュー観点

- Event publish の時点で関連する状態変更が完了しているか。
- 購読者が event 直後に world state を読んだ場合、一貫した状態を取得できるか。
- event 発行順が暗黙ではなく、UseCase / Orchestrator で明示されているか。

---

## 10. 暫定 TODO が実挙動に影響する場合は Task 化する

### 問題

TODO が「将来の改善」ではなく現在のゲーム挙動に影響している場合、仕様不一致やバランス不整合として扱う必要がある。

例:

```csharp
// TODO: SpawnTableMaster から重み付き抽選に変更する
var entry = spawnTable.Entries[0];
```

この場合、TODO は単なるメモではなく、現在の spawn 結果を仕様と違うものにしている。

### 方針

TODO を分類する。

| 種別 | 扱い |
|---|---|
| 実挙動に影響する TODO | 不具合 / 仕様不一致として task 化 |
| 性能に影響する TODO | milestone の前提条件として task 化 |
| 将来拡張の TODO | backlog へ記録 |
| コメントだけの TODO | 削除または設計 docs へ移す |

### レビュー観点

- TODO が現在の挙動を変えていないか。
- TODO が仕様 docs と矛盾する状態を作っていないか。
- TODO に milestone / task / owner があるか。

---

## 11. Factory / Request の並列構造は共通核と差分を先に確認する

### 問題

Entity 種別ごとに Factory / Request / UseCase を並列に作ると、後から共通化しづらい重複構造になる。

例:

```text
AdventurerFactory / AdventurerCreateRequest / SpawnAdventurerUseCase
MonsterFactory    / MonsterCreateRequest    / SpawnMonsterUseCase
```

差分が少ない場合、上層 interface だけが重複している可能性がある。

### 方針

型別 Factory を作る前に、共通 request と差分 field で表現できないか確認する。種別ごとの違いは behavior、profile、spawn policy などに閉じ込める。

例:

```csharp
public sealed class ActorSpawnRequest
{
    public int ArchetypeId { get; }
    public Guid ActorId { get; }
    public LayerPosition Position { get; }
    public ActorFaction Faction { get; }
    public string DisplayNameOverride { get; }
}
```

### レビュー観点

- `XxxFactory` / `YyyFactory` の差分が本当に責務差か、単なる request field 差か。
- 共通の core factory があるのに上層 interface が増えていないか。
- 将来の actor 種別追加時に同じ構造をコピーする必要がないか。

---

## 12. Performance-sensitive Path では LINQ / Defensive Copy を避ける基準を置く

### 問題

毎フレーム、Actor 数比例、Item 数比例、combat hit 数比例の経路で LINQ chain や防御的コピーを使うと、GC alloc や CPU cost が積み上がる。

例:

```csharp
var selected = floors
    .Where(score => score.Difficulty <= actorCombatPower)
    .OrderByDescending(score => score.Master.FloorIndex)
    .FirstOrDefault();

var items = slots.ToList();
```

### 方針

性能敏感な経路では loop、再利用バッファ、差分更新を優先する。LINQ や defensive copy は、実行頻度が低い初期化・編集・一回限りの query に限定する。

### レビュー観点

- `Update()` / frame loop / combat loop / actor loop 内に `ToArray()`、`ToList()`、`GroupBy()`、`OrderBy()` がないか。
- 防御的コピーが必要な場面か、読み取り専用 view / span / reusable buffer で代替できないか。
- 実行頻度とデータ件数に対して allocation が許容できるか。

---

## 採用候補の優先順位

今回のレビューから見て、正式ガイドラインへ優先的に反映したい順序は以下。

1. Mutable Child Object を Aggregate 外へ公開しない
2. Unity EntryPoint に Game Simulation の順序を置かない
3. Frame Loop / Schedule Tick / Event-Driven 処理を混ぜない
4. Actor-keyed State は削除イベントで必ず cleanup する
5. Runtime State に Master / Spec の不変値をコピーしない
6. Static Catalog / Registry を Domain から直接呼ばない
7. Event は Transaction 完了後に publish する
8. View 用 Reader と Application 内部 Reader を分ける
9. 暫定 TODO が実挙動に影響する場合は Task 化する
10. Performance-sensitive Path では LINQ / Defensive Copy を避ける基準を置く
11. DTO 重複は「現在値」「履歴」「集計途中」の責務で分ける
12. Factory / Request の並列構造は共通核と差分を先に確認する

