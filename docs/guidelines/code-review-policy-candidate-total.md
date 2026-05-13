# コードレビュー追加ガイドライン候補（統合版）

Milestone 5 完了レビューの振り返りから Claude Code と Codex がそれぞれ提案した内容を整理した統合版。
採用する項目を決定後、対応する既存ガイドラインへ移植する。

このドキュメントは他プロジェクトでも利用できる汎用的な設計ポリシーとして記述する。
プロジェクト固有の具体例は各項目末尾の「DungeonInn Example」にのみ記載する。

## 整理の結果

| 区分 | 説明 |
|---|---|
| **新規追加候補** | 既存ガイドラインに対応する記述がなく、新たに追加する価値がある |
| **既存対応済み** | 同等のルールが既存ガイドラインに存在する。独立した追加は不要 |

---

## 新規追加候補

### 1. Entity の全削除経路に state cleanup を紐付ける

**出典**: Claude:A + Codex:5（統合）  
**対応先候補**: `usecase-boundary-guidelines.md`

### 問題

Entity がシステムから除去される経路は複数ある（戦闘での死亡、自然退場、強制消去など）。
`Dictionary<EntityId, State>` を持つ Service の cleanup が特定経路のみに実装され、
別の経路では漏れるパターンが起きやすい。

死亡時だけ cleanup しても、退場・despawn・scene unload など別の削除経路で
エントリが残り続け、メモリリークや不正な状態参照が起きる。

### 方針

Entity-keyed state を持つ Service は、その Entity が除去されうる**全経路**に対して cleanup を実装する。
2つのアプローチを選べる。

**アプローチ A: 削除を一元化する UseCase / Handler を設ける**

```csharp
public sealed class RemoveEntityUseCase
{
    public void Execute(IWorldState worldState, Guid entityId, RemovalCause cause)
    {
        worldState.Remove(entityId);

        // 全 Service への cleanup を網羅的に記述する
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
帰還後も内部 Dictionary にエントリが残り続ける。

---

### 2. View 層には表示専用の Reader / DTO を提供する

**出典**: Claude:B + Codex:2（統合）  
**対応先候補**: `domain-usecase-design-guidelines.md`

### 問題

View が Application 内部用の広い Reader interface を直接参照すると、
表示層から Domain 集約全体へアクセスできる状態になる。
これは View が表示に不要なビジネス情報へ依存する入口になる。

また、View が Entity から直接値を取得して計算・型判断を行い始めると、
Domain の変更が即座に View に伝播する。

### 方針

View 用の読み取り経路と Application 内部用の読み取り経路を分ける。

**インターフェース分離**: View には表示目的ごとの DataProvider / Query interface を渡す

```csharp
// Application 内部用: 広い Reader（UseCase が使う）
public interface IWorldStateReader
{
    IReadOnlyList<Entity> Entities { get; }
    Entity Find(Guid id);
    // ... ビジネスロジックに必要な全読み取り
}

// View 専用: 表示に必要な値だけを返す DataProvider
public readonly struct EntityViewData
{
    public Guid Id { get; init; }
    public Vector3 Position { get; init; }
    public float HpRatio { get; init; }
    public string StateLabel { get; init; }
}

public interface IEntityViewDataProvider
{
    IReadOnlyList<EntityViewData> GetViewData();
}
```

**DTO 変換**: Domain → DTO の変換を Query クラスに集中させる

```csharp
// Before: Presenter が Domain 内部を直接参照
public sealed class EntityStatusPresenter
{
    public void UpdateView(Entity entity)
    {
        hpBar.value = (float)entity.CurrentHp / entity.Params.MaxHp;  // 計算式が View に漏れる
        stateLabel.text = entity.Behavior is FooBehavior foo
            ? foo.State.ToString()
            : "Unknown";                                                // 型判断が View に漏れる
    }
}

// After: Query が変換を担い、Presenter は DTO だけを受け取る
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

### 3. ゲームループ処理を実行トリガーで分類する

**出典**: Claude:C + Codex:4（統合）  
**対応先候補**: `usecase-boundary-guidelines.md`

### 問題

ゲームループに処理が増えるにつれて「毎フレーム処理なのか」「イベントトリガーなのか」が不明瞭になる。
毎フレーム処理で GC Alloc や重い処理が発生すると影響が大きいが、
実行頻度が分からないとパフォーマンスレビューの基準が曖昧になる。

また、定期 tick で十分な処理や状態変化時だけ必要な処理が毎フレームループに混在すると、
Entity 数・データ数が増えたときに不要な処理が積み上がる。

### 方針

処理の実行契機を設計時に以下の3種類に分類し、ループエントリポイントでコメントとして明記する。

| 種別 | 例 | 実行契機 |
|---|---|---|
| Frame Loop | 移動補間、物理更新、戦闘進行 | 毎フレーム必須 |
| Schedule Tick | 日次処理、定期生成、時間経過回復 | ゲーム内 tick 単位 |
| Event-Driven | 装備更新、価格再計算、UI 再集計 | 状態変更イベント / dirty flag |

`Frame Loop` に分類される処理は GC Alloc・LINQ 呼び出し・コレクション生成を原則禁止とする。

```csharp
// Before: 分類が不明瞭
async UniTask TickAsync(float deltaTime)
{
    updateEquipmentUseCase.Execute(state);   // これは毎フレーム必要か？
    await sellItemsUseCase.Execute(state);   // これは？
    await advanceSimulationUseCase.ExecuteAsync(state, deltaTime);
}

// After: 種別をコメントで明示する
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

### レビュー観点

- ゲームループに追加する処理に種別コメントを付けたか
- `Frame Loop` に分類した処理で LINQ / `ToList()` / コレクション生成が発生していないか
- 毎フレーム処理に `Schedule Tick` / `Event-Driven` で十分な処理が混ざっていないか

### DungeonInn Example

`SellItemsUseCase.Execute()` は現在 `TickAsync` 内で毎フレーム呼ばれているが、
内部でコレクション生成が発生している。`ScheduleTick` または `EventDriven` へ移動することで削減できる。

---

### 4. UseCase / Service / Orchestrator の命名と配置を定義する

**出典**: Claude:D  
**対応先候補**: `usecase-boundary-guidelines.md`

### 問題

「UseCase なのに状態を持つ」「Service なのに UseCase を呼ぶ」という問題がレビューで繰り返し指摘される。
クラス名と責務が一致しないと、新しいクラスをどこに追加すべきか判断が曖昧になる。

### 方針

以下の3種類を明確に区別する。

| 種別 | 責務 | NG パターン |
|---|---|---|
| **UseCase** | 単一ユースケースを実行する。ステートレス。他 UseCase を呼ばない | `IDisposable` を実装する / 状態を持つ |
| **Service** | 長期状態を保持する。`IDisposable` を実装。イベントを購読して状態を更新する | UseCase を注入して呼ぶ |
| **Orchestrator** | UseCase を定義された順序で呼ぶ。順序制御上の分岐は持ってよい | Domain 判断・業務ルール計算を自身に抱え込む |

Orchestrator が持ってよい分岐は「成功・失敗に応じた次 UseCase の選択」程度に留める。
戦闘勝敗の判定・料金計算・ビジネスルールの評価を Orchestrator 内で直接計算するのは NG。

```csharp
// Before: UseCase が状態を持ち、Service が UseCase を呼ぶ
public sealed class FooUseCase : IDisposable       // NG: UseCase が IDisposable
{
    readonly Dictionary<Guid, float> state = new(); // NG: UseCase が状態を持つ
}

// After: 種別に応じてクラスを正しく分類する
public sealed class FooStateService : IDisposable  // OK: Service が状態を持つ
{
    readonly Dictionary<Guid, float> state = new();
}

public sealed class FooUseCase                     // OK: UseCase はステートレス
{
    readonly FooStateService fooState;
    public async UniTask ExecuteAsync(...) { ... }
}

public sealed class FooOrchestrator                // OK: Orchestrator は順序制御のみ
{
    public async UniTask ExecuteAsync(...)
    {
        barUseCase.Execute(...);
        await fooUseCase.ExecuteAsync(...);
    }
}
```

### レビュー観点

- 新しいクラスを追加するとき、UseCase / Service / Orchestrator のどれかを最初に決めたか
- `IDisposable` を実装している UseCase は Service への昇格を検討したか
- Orchestrator が `if` / `switch` による分岐判断を持ち始めていないか

### DungeonInn Example

`WorldGameLoopEntryPoint` は Orchestrator、`AdventurerRecoveryStateService` は Service、
`AdvanceCombatUseCase` は UseCase に分類される。

---

### 5. Unity EntryPoint にゲーム進行順序を置かない

**出典**: Codex:3  
**対応先候補**: `usecase-boundary-guidelines.md`

### 問題

`MonoBehaviour` が多数の UseCase を直接注入し、`Update()` にゲーム進行の詳細な順序を書くと、
View 層が Application の orchestration を握ることになる。
UseCase の追加・削除のたびに MonoBehaviour を修正する必要が生じ、責務が混在する。

### 方針

Unity EntryPoint は lifecycle・delta time・cancel token・View 更新の入口に限定する。
ゲーム進行順序は Application 層の Orchestrator に置く。

```csharp
// Before: MonoBehaviour がゲーム進行の詳細を管理している
public sealed class GameLoopEntryPoint : MonoBehaviour
{
    SpawnUseCase spawnUseCase;
    AdvanceSimulationUseCase simulationUseCase;
    SellItemsUseCase sellItemsUseCase;  // UseCase が増えるたびに EntryPoint が肥大化する

    async UniTask TickAsync(float deltaTime)
    {
        spawnUseCase.Execute();
        simulationUseCase.Execute();
        sellItemsUseCase.Execute();
    }
}

// After: EntryPoint は Orchestrator に委譲するだけ
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

### DungeonInn Example

`WorldGameLoopEntryPoint` が UseCase を直接注入してループを管理している。
将来的には `AdvanceWorldFrameUseCase` 相当の Orchestrator に順序を移し、
EntryPoint は delta time と cancel token の受け渡しに限定することが望ましい。

---

### 6. 並列 Factory / Request 構造は共通基盤を先に確認する

**出典**: Claude:E + Codex:11（統合）  
**対応先候補**: `domain-usecase-design-guidelines.md`

### 問題

Entity 種別ごとに Factory / Request / UseCase を並列に作ると、重複構造になる。
差分が少ない場合、共通 interface だけで対処できるのに型を増やし続けてしまう。

### 方針

型別 Factory / Request を作る前に、共通基盤と差分フィールドで表現できないか確認する。

| 状況 | 対応 |
|---|---|
| 2つのクラスが全フィールド同一 | 1つに統合する |
| 上層 interface が別で実装が共通 | interface を統一し、差分は型判断で処理する |
| 差異が1フィールドのみ | 共通 interface を作り、差分フィールドを派生型に置く |
| 仕様上の意味が明確に異なる | 分離を維持し、コメントで理由を残す |

```csharp
// Before: 差分が DisplayName のみなのに型が2本立て
public sealed class FooCreateRequest { public int ArchetypeId { get; init; } public string DisplayName { get; init; } }
public sealed class BarCreateRequest { public int ArchetypeId { get; init; } }

public sealed class EntityFactory
{
    public Entity Create(FooCreateRequest request) { ... }
    public Entity Create(BarCreateRequest request) { ... }
}

// After: 共通 request に optional field を持たせ、Factory 内の型判断を不要にする
public sealed class EntityCreateRequest
{
    public int ArchetypeId { get; init; }
    public string DisplayNameOverride { get; init; }  // null なら archetype のデフォルト名を使う
    public EntityFaction Faction { get; init; }
}

public sealed class EntityFactory
{
    public Entity Create(EntityCreateRequest request)
    {
        return core.Create(request.ArchetypeId, request.DisplayNameOverride, request.Faction);
    }
}
```

### レビュー観点

- `XxxFactory` / `YyyFactory` の差分が責務差か、単なる request フィールド差か
- 共通の core factory があるのに上層 interface が増えていないか
- 新しい Entity 種別追加時に同じ構造をコピーする必要が生まれていないか

### DungeonInn Example

`AdventurerCreateRequest` / `MonsterCreateRequest` は `ActorFactoryCore` で共通実装されているが、
上層 interface が統一されていないため Factory のメソッドが2本立てになっている。

---

### 7. TODO を責務で分類しマイルストーンに紐付ける

**出典**: Claude:F + Codex:10（統合）  
**対応先候補**: `AGENTS.md` または新規 `workflow-guidelines.md`

### 問題

TODO が「将来の改善」ではなく現在のゲーム挙動に影響しているケースで、
仕様不一致として扱われずにマイルストーンが完了とみなされてしまう。

### 方針

TODO を以下の4種類に分類し、それぞれ対応を決める。

| 種別 | 扱い |
|---|---|
| 実挙動に影響する（仕様と差異がある） | 不具合・仕様不一致として task 化し、milestone に紐付ける |
| 性能に影響する | milestone の前提条件として task 化する |
| 将来拡張 | backlog へ記録。`// TODO(backlog):` で明示 |
| コメントのみ（設計 docs に移すべき内容） | 削除または設計 docs へ移す |

実挙動・性能に影響する TODO は `// TODO(milestone:X): 理由` 形式で記録し、task ファイルを作成する。

```csharp
// Before: 理由も対応予定も不明な TODO
// TODO: 重み付き抽選に変更する
return candidates[Random.Range(0, candidates.Count)];

// After: milestone 番号と理由を明示し、task ファイルを作成する
// TODO(milestone:6): 重み付き抽選に変更する。現在は等確率。
// 理由: milestone:5 では重みデータが未準備のため省略
return candidates[Random.Range(0, candidates.Count)];
```

マイルストーン完了レビューのチェックリストに「そのマイルストーン番号の TODO が残っていないか」を追加する。

### レビュー観点

- TODO が現在の挙動を仕様と違うものにしていないか
- milestone 番号のない TODO が実挙動に影響していないか
- マイルストーン完了時に、そのマイルストーン番号の TODO が残っていないか

### DungeonInn Example

`SpawnTableUseCase` が重みを無視して等確率選択している状態で milestone:5 が完了とみなされ、
次のレビューで「仕様差分」として発覚した。

---

### 8. DTO は「現在値・履歴・集計途中」の責務で分ける

**出典**: Codex:7（固有）  
**対応先候補**: `domain-usecase-design-guidelines.md`

### 問題

似たフィールドを持つ DTO が複数存在すると、どれが正典か分かりにくくなる。
詰め替えコードが増え、片方だけ更新されるリスクもある。

### 方針

DTO は責務で分類し、名前でその役割を明示する。

| 種別 | 役割 | 命名目安 |
|---|---|---|
| Current Status | 現在表示する状態 | `XxxStatus` |
| Report / History | 過去の記録として保存する不変データ | `XxxReport` / `XxxRecord` |
| Statistics / Accumulator | 集計途中の内部状態 | `XxxStatistics` / `XxxAccumulator` |
| Summary | 複数 DTO の共通フィールドをまとめた値型 | `XxxSummary` |

```csharp
// Before: 同じ意味のフィールドが2つの DTO に重複している
public readonly struct EconomyStatus  { public int GuestsToday; public int SalesToday; public int Reputation; }
public readonly struct DailyReport    { public int Guests;       public int Sales;      public int Reputation; }

// After: 共通フィールドを Summary に切り出し、Status と Report がそれを参照する
public readonly struct EconomySummary { public int Guests; public int Sales; public int Reputation; }
public readonly struct EconomyStatus  { public EconomySummary Today; public int TrendDelta; }
public readonly struct DailyReport    { public EconomySummary Result; public DateOnly Date; }
```

### レビュー観点

- 同じ意味のフィールドが複数 DTO に重複していないか
- DTO 間の詰め替えコードが増えていないか
- 現在値・履歴・集計途中の責務が名前と構造から分かるか

### DungeonInn Example

`InnEconomyStatus`（現在表示）・`InnDailyReport`（履歴記録）・`InnEconomyStatistics`（集計途中）が
類似フィールドを持っており、`InnEconomySummary` のような共通値型を切り出すことで重複を減らせる。

---

### 9. パフォーマンス敏感経路での LINQ / GC Alloc を禁止する

**出典**: Codex:12（固有）  
**対応先候補**: `coding-rules.md` または `usecase-boundary-guidelines.md`

### 問題

毎フレーム・Entity 数比例・アイテム数比例の経路で LINQ chain や防御的コピーを使うと、
GC Alloc や CPU コストが積み上がる。データ数が増えるにつれて線形に悪化する。

### 方針

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

### レビュー観点

- `Frame Loop` に分類される処理内に `ToArray()` / `ToList()` / LINQ chain がないか
- Entity 数・アイテム数に比例する処理が毎フレーム無条件に走っていないか
- 防御的コピーが必要な場面か、読み取り専用 view / span / 再利用バッファで代替できないか

### DungeonInn Example

ループ内で `floors.Where(...).OrderByDescending(...).FirstOrDefault()` や `slots.ToList()` が
呼ばれており、毎フレーム GC Alloc が発生している。

---

## 既存ガイドラインで対応済み（新規追加は不要）

以下は Codex が提案したが、既存のガイドラインに同等のルールが存在する。
内容の補足・拡充が必要であれば既存項目に加筆する形で対応する。

| Codex 提案 | 既存対応先 |
|---|---|
| Codex:1 — Mutable Child Object を Aggregate 外へ公開しない | `usecase-boundary-guidelines.md` §4「集約内部の可変オブジェクトを直接公開しない」|
| Codex:6 — Runtime State に Master / Spec の不変値をコピーしない | `domain-usecase-design-guidelines.md`「マスタ参照値を Runtime Instance に複製しない」|
| Codex:8 — Static Catalog / Registry を Domain から直接呼ばない | `usecase-boundary-guidelines.md` §6「Domain Entity が static クラスに依存しない」|
| Codex:9 — Event は Transaction 完了後に publish する | `domain-usecase-design-guidelines.md` §10「UseCase はトランザクション境界であり、イベントは事後通知である」|

---

## 採用検討マトリクス

| # | タイトル | 重要度 | 対応先候補 | 備考 |
|---|---|---|---|---|
| 1 | Entity 全削除経路への state cleanup | 高 | `usecase-boundary-guidelines.md` | 実際のバグとして発生済み |
| 2 | View 層への表示専用 Reader / DTO 分離 | 中 | `domain-usecase-design-guidelines.md` | 導入基準を明確にすると使いやすい |
| 3 | ゲームループの実行トリガー分類 | 中 | `usecase-boundary-guidelines.md` | §9 と合わせて整理 |
| 4 | UseCase / Service / Orchestrator の命名 | 高 | `usecase-boundary-guidelines.md` | §2 と合わせて整理 |
| 5 | Unity EntryPoint にゲーム進行順序を置かない | 中 | `usecase-boundary-guidelines.md` | #4 と一緒に扱ってもよい |
| 6 | 並列 Factory / Request 構造の共通化 | 低 | `domain-usecase-design-guidelines.md` | 発生頻度は低め |
| 7 | TODO の責務分類とマイルストーン管理 | 高 | `AGENTS.md` | 仕様差分バグ防止に直結 |
| 8 | DTO の責務分類（現在値・履歴・集計途中） | 中 | `domain-usecase-design-guidelines.md` | 命名規約として位置付けやすい |
| 9 | パフォーマンス敏感経路での GC Alloc 禁止 | 中 | `coding-rules.md` | 3（トリガー分類）と連動させる |
