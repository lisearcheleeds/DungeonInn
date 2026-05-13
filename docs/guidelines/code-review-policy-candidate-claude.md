# コードレビュー追加ガイドライン候補（Claude Code 提案）

このドキュメントは、Milestone 5 完了レビューの振り返りから抽出した設計ルール候補をまとめる。
採用が決まった項目は、対応する既存ガイドラインに移植する。

対応先の目安を各項目に記載しているが、横断的な内容については新規ドキュメントへの分離も検討する。

---

## 提案A: Entity の全削除経路に state cleanup を紐付ける

**対応先候補**: `usecase-boundary-guidelines.md`

### 背景

Entity（Actor 等）がゲームワールドから除去される経路は複数ある。
戦闘での死亡（`ActorDefeated`）・帰還（`ActorDeparted`）・タイムアウト・強制消去など、経路が増えるにつれて
各経路固有の cleanup が漏れやすくなる。

「死亡の cleanup は実装済みだが、帰還時の cleanup が漏れていた」という問題が実際に発生した（DungeonInn: `ActorDeparted` 後の `AdventurerExplorationStateService` エントリ残留）。

### ルール

Entity を削除・除去する UseCase / Resolver を追加するたびに、その Entity が保有しているすべての
Service 側エントリ（`XxxStateService`、`XxxHistoryService` 等）を明示的に cleanup する経路を確認する。
cleanup を行わない場合は、その理由をコメントまたはタスクログに記録する。

### Before

```csharp
// ActorDepartedHandler — 帰還処理
public void Handle(ActorDeparted evt)
{
    worldState.RemoveActor(evt.ActorId);
    eventPublisher.Publish(evt);
    // NG: explorationStateService / recoveryStateService のエントリを消していない
}
```

```csharp
// 死亡経路（別クラス）は cleanup 済みだが、帰還経路では漏れが起きている
public sealed class AdventurerRecoveryStateService : IDisposable
{
    public AdventurerRecoveryStateService(IEventSubscriber eventSubscriber)
    {
        // ActorDefeated は購読しているが ActorDeparted は購読していない
        eventSubscriber.OnEvent<ActorDefeated>()
            .Subscribe(evt => accumulatedHp.Remove(evt.ActorId));
    }
}
```

### After

```csharp
// 削除経路のルーティングを一元化する
public sealed class RemoveActorUseCase
{
    public void Execute(IGameWorldState worldState, Guid actorId, ActorRemovalCause cause)
    {
        worldState.RemoveActor(actorId);

        // 全 Service への cleanup を網羅的に記述する
        recoveryStateService.Remove(actorId);
        explorationStateService.Remove(actorId);
        combatHistoryService.Remove(actorId);

        eventPublisher.Publish(new ActorRemoved(actorId, cause));
    }
}
```

あるいは、Service 側が全削除イベントを購読する場合は、購読するイベントの一覧を Service のコンストラクタコメントに列挙する。

```csharp
public sealed class AdventurerRecoveryStateService : IDisposable
{
    // 購読するイベント: ActorDefeated, ActorDeparted（全除去経路を網羅すること）
    public AdventurerRecoveryStateService(IEventSubscriber eventSubscriber)
    {
        eventSubscriber.OnEvent<ActorDefeated>()
            .Subscribe(evt => accumulatedHp.Remove(evt.ActorId));
        eventSubscriber.OnEvent<ActorDeparted>()
            .Subscribe(evt => accumulatedHp.Remove(evt.ActorId));
    }
}
```

### DungeonInn Example

`AdventurerExplorationStateService` と `AdventurerRecoveryStateService` は `ActorDefeated` を購読して cleanup しているが、
`ActorDeparted`（帰還）時の cleanup が未実装。帰還後も内部 Dictionary にエントリが残り続ける。

### 適用基準

- 新しい Entity 除去経路（UseCase / Handler / Resolver）を追加するときは必ず確認する
- 新しい `XxxStateService` を作るときは、cleanup 対象の全除去イベントをコンストラクタで購読するか、呼び出し元 UseCase から明示的に削除するか、どちらかを選ぶ
- レビュー時は「この Entity はどの経路で消えるか」を列挙し、各経路で Service cleanup が漏れていないか確認する

---

## 提案B: View 層には表示専用の Query / DTO を提供する

**対応先候補**: `domain-usecase-design-guidelines.md`

### 背景

View / Presenter が `IGameWorldStateReader` 経由で `Actor` 等の Domain Entity を直接参照すると、
表示のために Entity の内部構造（Behavior 型、Stats、Equipment 等）を知る必要が生じ、View と Domain が密結合する。

View が表示に必要な値を Entity から自分で計算し始めると、Domain の変更が即座に View に伝播する。

### ルール

View / Presenter が表示に必要な値を Domain Entity から直接取得する場合、
「表示用に整形された値だけを返す Query クラス / DTO」を間に置く。
Domain Entity の型・継承・フィールド名が View から見えない設計を目指す。

### Before

```csharp
// Presenter が Actor の内部を直接参照している
public sealed class ActorStatusPresenter
{
    public void UpdateView(Actor actor)
    {
        nameLabel.text = actor.DisplayName;
        hpBar.value = (float)actor.CurrentHp / actor.Params.MaxHp;  // Domain 計算式が View に漏れる
        weaponLabel.text = actor.Params.WeaponAttack.ToString();      // 同上
        stateLabel.text = actor.Behavior is AdventurerBehavior adv
            ? adv.LifecycleState.ToString()                           // Behavior 型に依存
            : "Unknown";
    }
}
```

### After

```csharp
// 表示専用 DTO
public sealed class ActorStatusView
{
    public string DisplayName { get; init; }
    public float HpRatio { get; init; }
    public int WeaponAttack { get; init; }
    public string LifecycleStateLabel { get; init; }
}

// Query クラスが Domain → DTO 変換を担う
public sealed class ActorStatusQuery
{
    public ActorStatusView GetView(Actor actor)
    {
        var adv = actor.Behavior as AdventurerBehavior;
        return new ActorStatusView
        {
            DisplayName = actor.DisplayName,
            HpRatio = actor.Params.MaxHp > 0
                ? (float)actor.CurrentHp / actor.Params.MaxHp
                : 0f,
            WeaponAttack = actor.Params.WeaponAttack,
            LifecycleStateLabel = adv?.LifecycleState.ToString() ?? "Unknown",
        };
    }
}

// Presenter は DTO だけを受け取る
public sealed class ActorStatusPresenter
{
    public void UpdateView(ActorStatusView view)
    {
        nameLabel.text = view.DisplayName;
        hpBar.value = view.HpRatio;
        weaponLabel.text = view.WeaponAttack.ToString();
        stateLabel.text = view.LifecycleStateLabel;
    }
}
```

### DungeonInn Example

`WorldGameLogPresenter` が `InnEconomyStatus` や `Actor` のフィールドを直接読んで文字列を組み立てている箇所は、
専用の `InnStatusQuery` / `ActorStatusQuery` に変換ロジックを移し、Presenter をシンプルに保つ。

### 適用基準

- Presenter / View が `actor.Behavior is XxxBehavior` のような型判断を行っている場合は Query クラスへ移す
- Domain Entity のフィールド名が View 層のコードに直接現れていたら、DTO 経由に置き換えを検討する
- Domain の表示用フィールドが増える理由が「View が必要としているから」の場合、そのフィールドは DTO に置くべきか問い直す

---

## 提案C: ゲームループ処理を実行トリガーで分類する

**対応先候補**: `usecase-boundary-guidelines.md`

### 背景

ゲームループ（`WorldGameLoopEntryPoint.TickAsync` 等）に UseCase が増えるにつれて、
「このクラスは毎フレーム呼ばれているのか」「このクラスはイベントトリガーか」が不明瞭になる。

毎フレーム呼ばれるクラスで GC Alloc が発生すると影響が大きいが、イベントトリガーなら許容できる場合がある。
実行頻度の違いが明確でないと、パフォーマンスレビューの基準が曖昧になる。

### ルール

ゲームループ内の処理を以下の3種類に分類し、クラスや TaskFile に明記する。

| 種別 | 説明 | 命名目安 |
|---|---|---|
| `EveryFrame` | `TickAsync` から毎フレーム呼ばれる | UseCase / Processor |
| `EventDriven` | ゲームイベントを購読して実行される | Service / Handler |
| `OnDemand` | ユーザー操作や他 UseCase から明示的に呼ばれる | UseCase |

`EveryFrame` に分類されるクラスは GC Alloc・LINQ 呼び出し・foreach による配列生成を原則禁止とする。

### Before

```csharp
// WorldGameLoopEntryPoint — どれが毎フレームかコード全体を読まないと分からない
await updateEquipmentUseCase.Execute(gameWorldState);    // 毎フレーム
await sellItemsUseCase.Execute(gameWorldState);           // 毎フレーム
await advanceCombatUseCase.ExecuteAsync(worldState, dt); // 毎フレーム
// recoveryStateService は EventDriven だが、呼び出しと購読が混在していて判断しにくい
```

### After

```csharp
// コメントで実行種別を宣言する
// [EveryFrame] 毎フレーム呼ばれる UseCase
await updateEquipmentUseCase.Execute(gameWorldState);
await sellItemsUseCase.Execute(gameWorldState);
await advanceCombatUseCase.ExecuteAsync(worldState, dt);

// [EventDriven] — recoveryStateService はコンストラクタで ActorDefeated を購読済み
// ここでは直接呼ばない
```

あるいは、ループ内の処理を `EveryFrameGroup` / `EventDrivenGroup` としてグループ化するクラスを作り、
クラス名で明示する手法も有効。

### DungeonInn Example

`SellItemsUseCase.Execute()` は毎フレーム経路（`TickAsync` 内）に置かれているが、
クラス内部で `List.Add()` / `foreach` / LINQ による新規コレクション生成が発生しており、
「`EveryFrame` 制約」を明示することでパフォーマンスレビューのトリガーになる。

### 適用基準

- ゲームループエントリポイントに UseCase を追加するときは、種別コメント（`// [EveryFrame]` 等）を付ける
- `EveryFrame` UseCase のレビュー時は GC Alloc チェックを必須とする
- イベント購読コールバック内で重い処理をしている場合は `EventDriven` として記録し、発生頻度の見積もりをコメントに残す

---

## 提案D: UseCase / Service / Orchestrator の命名と配置を定義する

**対応先候補**: `usecase-boundary-guidelines.md`

### 背景

「UseCase なのに状態を持つ」「Service なのに UseCase を呼ぶ」「Orchestrator と UseCase の境界が曖昧」
という問題がレビューで繰り返し指摘されている。

クラス名と実際の責務が一致していないと、新しいクラスをどこに追加すべきか判断が難しくなる。

### ルール

以下の3種類を明確に区別する。

#### UseCase

- 単一のユースケースを実行する。ステートレス
- コンストラクタでイベントを購読しない
- 他の UseCase を注入して呼び出さない
- 命名: `ExecuteXxxUseCase` / `AdvanceXxxUseCase` / `DetectXxxUseCase` / `SpawnXxxUseCase` 等

#### Service

- 長期状態を保持する（`Dictionary`、`HashSet`、フラグ等）
- `IDisposable` を実装し、イベントを購読して状態を更新する
- UseCase からは読み取り API として利用される
- 命名: `XxxStateService` / `XxxHistoryService` / `XxxAggregatorService` 等

#### Orchestrator

- 複数の UseCase を定義された順序で呼ぶ
- 自身は状態を持たず、判断も行わない（判断は個々の UseCase に委譲する）
- ゲームループエントリポイント（`WorldGameLoopEntryPoint`）もこの分類に近い
- 命名: `XxxOrchestrator` / `XxxLoopEntryPoint` / `XxxPipeline` 等

### Before

```csharp
// "UseCase" なのに IDisposable を実装して状態を持っている
public sealed class RecoverAdventurerAtInnUseCase : IDisposable
{
    readonly Dictionary<Guid, float> accumulatedHp = new(); // NG: UseCase が状態を持つ
}

// "Service" なのに UseCase を注入して呼び出している
public sealed class CombatDefeatService
{
    readonly GrantExperienceUseCase grantExperience; // NG: Service が UseCase を持つ
}
```

### After

```csharp
// 状態を持つなら Service に昇格させる
public sealed class AdventurerRecoveryStateService : IDisposable
{
    readonly Dictionary<Guid, float> accumulatedHp = new(); // OK: Service が状態を持つ
}

// UseCase の呼び出し順序を定義するなら Orchestrator にする
public sealed class HandleActorDefeatOrchestrator
{
    // UseCase を順番に呼ぶだけ。自身は状態を持たない
    public async UniTask ExecuteAsync(IGameWorldState worldState, Actor attacker, Actor target)
    {
        combatDefeatResolver.Resolve(worldState, attacker, target);
        await grantExperienceUseCase.Execute(attacker, target);
        await dropItemUseCase.Execute(target, worldState);
    }
}
```

### DungeonInn Example

- `AdventurerRecoveryStateService` / `AdventurerExplorationStateService` → Service（状態保持・イベント購読）
- `AdvanceCombatUseCase` / `SellItemsUseCase` → UseCase（ステートレス・単一責務）
- `WorldGameLoopEntryPoint` → Orchestrator（UseCase 呼び出し順序の定義）
- `AdvanceActorAiOrchestrator` → Orchestrator（AI 評価の順序制御）

### 適用基準

- クラスを追加するときは「UseCase / Service / Orchestrator のどれか」を最初に決める
- `IDisposable` を実装している UseCase はコードレビューで Service への昇格を検討する
- Orchestrator が `if` / `switch` による分岐判断を持ち始めたら、その判断を UseCase に移す

---

## 提案E: 類似する並列構造は共通基盤に集約し上層 interface を統一する

**対応先候補**: `domain-usecase-design-guidelines.md`

### 背景

`AdventurerCreateRequest` と `MonsterCreateRequest` のように、ほぼ同じ構造を持つクラスが
並列して存在するケースがある。実装の共通化は `ActorFactoryCore` で行われているが、
上層の interface が別々のままだと、それを受け取る UseCase も別々に定義されてしまう。

並列重複が「異なる仕様を表すために必要」なのか「単なる構造上の重複」なのかをレビュー時に判断する基準が必要。

### ルール

「同じ共通基盤を使い、かつ互いに置き換え可能な型」が並列して存在する場合は、共通の interface に統一する。
一方、「仕様上の差異があり、将来的に独立して拡張される」場合は分離を維持してよい。

判断基準:

| 状況 | 対応 |
|---|---|
| 2つのクラスが全フィールド同一 | 1つに統合する |
| 上層 interface が別で実装が共通 | interface を統一し、型で差異を表す |
| 差異が1フィールドのみで他は同一 | 基底クラスまたは interface を作り、差異フィールドを分離する |
| 仕様上の意味が明確に異なる | 分離を維持し、コメントで理由を残す |

### Before

```csharp
// AdventurerCreateRequest と MonsterCreateRequest がほぼ同一構造
public sealed class AdventurerCreateRequest
{
    public ActorArchetypeMasterId ArchetypeId { get; init; }
    public string DisplayName { get; init; }  // MonsterCreateRequest にはない
}

public sealed class MonsterCreateRequest
{
    public ActorArchetypeMasterId ArchetypeId { get; init; }
}

// Factory が2つの別メソッドを持つ
public sealed class ActorFactory
{
    public Actor Create(AdventurerCreateRequest request) { ... }
    public Actor Create(MonsterCreateRequest request) { ... }
}
```

差異が `DisplayName` の有無だけなのに、Factory・UseCase・テストが全て2本立てになる。

### After

```csharp
// 共通 interface を作り、差異フィールドを分離する
public interface IActorCreateRequest
{
    ActorArchetypeMasterId ArchetypeId { get; }
}

public sealed class AdventurerCreateRequest : IActorCreateRequest
{
    public ActorArchetypeMasterId ArchetypeId { get; init; }
    public string DisplayName { get; init; }
}

public sealed class MonsterCreateRequest : IActorCreateRequest
{
    public ActorArchetypeMasterId ArchetypeId { get; init; }
}

// Factory は共通 interface で受け取り、必要な差異は型判断で処理する
public sealed class ActorFactory
{
    public Actor Create(IActorCreateRequest request)
    {
        var displayName = request is AdventurerCreateRequest adv ? adv.DisplayName : null;
        return core.Create(request.ArchetypeId, displayName);
    }
}
```

### DungeonInn Example

`AdventurerCreateRequest` / `MonsterCreateRequest` は `ActorFactoryCore` で共通実装されているが、
上層 interface が統一されていないため UseCase と Factory のメソッドが2本立てになっている。
`IActorCreateRequest` を導入し、共通受け口を作ることで UseCase 側を1本に統一できる。

### 適用基準

- 「ほぼ同じ構造のクラスが2つある」をレビューで発見したら、差異の理由を確認する
- 差異が仕様上の違いを表すのか、単なる実装上の重複なのかをタスクログに記録する
- 共通基盤（Factory Core、Calculator Base 等）が存在するのに上層 interface が分かれている場合は、interface 統一を優先的に検討する

---

## 提案F: 仕様と実装に差異がある TODO はマイルストーン完了条件に含める

**対応先候補**: `AGENTS.md` または新規 `workflow-guidelines.md`

### 背景

`SpawnTableUseCase` が重みを無視して等確率選択していたように、「仕様では定義されているが未実装」の箇所が
マイルストーン完了とみなされてしまうケースがある。

TODO コメントがコードに残っているが、それがいつ対応される予定なのかが不明瞭になっている。

### ルール

実装中に「仕様に記載があるが今回は省略」と判断した箇所は、以下のどちらかを必ず行う。

1. **次のマイルストーンのタスクとして登録する**: 省略した理由と対応予定マイルストーンをタスクに記載する
2. **コードに `// TODO(milestone:X): ...` 形式で記録する**: マイルストーン番号を含めることで、いつ対応予定かを明示する

マイルストーン完了レビュー時は、そのマイルストーンに紐づく TODO が残っていないことを確認する。

### Before

```csharp
// 理由も対応予定も不明な TODO
public sealed class SpawnTableUseCase
{
    private Actor SelectFromSpawnTable(IReadOnlyList<SpawnEntry> table)
    {
        // TODO: 重み付き抽選に変更する
        return table[Random.Range(0, table.Count)].Actor;
    }
}
```

マイルストーン完了後もこの TODO が残り、次のレビューで「未実装」として発覚する。

### After

```csharp
// milestone 番号と理由を明示する
public sealed class SpawnTableUseCase
{
    private Actor SelectFromSpawnTable(IReadOnlyList<SpawnEntry> table)
    {
        // TODO(milestone:6): 重み付き抽選に変更する。現在は等確率。
        // 理由: milestone:5 では重みデータの準備が完了していなかったため省略
        return table[Random.Range(0, table.Count)].Actor;
    }
}
```

対応するタスクファイルを作成する。

```markdown
## task_0042: SpawnTable 重み付き抽選の実装

- 状態: 実装待ち（milestone:6 対象）
- 背景: milestone:5 では等確率での暫定実装。spec_system.md §3 の重み付き仕様を実装する
```

### DungeonInn Example

`SpawnTableUseCase` の重みを無視した等確率抽選は、`spec_system.md` の仕様と差異がある。
milestone:5 完了時点でこの差異を明示し、milestone:6 タスクとして `task_XXX.md` に登録する必要があった。

### 適用基準

- 仕様ドキュメントに記載があるのに「時間がないので省略」した箇所は、必ず上記どちらかの方法で追跡可能にする
- 「設計上の意図的な決定として省略」は省略してよいが、その旨をタスクログまたはコメントに残す
- マイルストーン完了レビューのチェックリストに「そのマイルストーン番号の TODO が残っていないか」を追加する

---

## レビュー用チェックリスト（候補）

採用された項目を既存チェックリストに追加する想定。

### Entity 削除経路（提案A）

- [ ] Entity を除去する UseCase / Handler を追加した場合、関連する全 Service の cleanup を確認したか
- [ ] `XxxStateService` を追加した場合、どのイベントで cleanup するかを列挙したか

### View / DTO（提案B）

- [ ] Presenter / View が Domain Entity の型に直接依存していないか
- [ ] `actor.Behavior is XxxBehavior` のような型判断が View 層に入っていないか

### ゲームループ分類（提案C）

- [ ] ゲームループに追加する処理に `// [EveryFrame]` / `// [EventDriven]` の分類コメントを付けたか
- [ ] `EveryFrame` クラスで LINQ / `List.Add()` / コレクション生成が発生していないか

### 命名・配置（提案D）

- [ ] 新しいクラスが UseCase / Service / Orchestrator のどれかを明確に判断したか
- [ ] UseCase が `IDisposable` を実装している場合、Service への昇格を検討したか

### 並列構造（提案E）

- [ ] ほぼ同じ構造のクラスが2つ以上ある場合、共通 interface を作れないか確認したか
- [ ] 「差異がない」と判断した場合、その理由をタスクログに残したか

### TODO 管理（提案F）

- [ ] 省略した仕様には `TODO(milestone:X):` 形式のコメントを残したか
- [ ] マイルストーン完了レビュー時に、そのマイルストーン番号の TODO が残っていないか確認したか
