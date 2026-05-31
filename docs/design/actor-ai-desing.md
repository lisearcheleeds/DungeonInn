# Actor AI Design

このドキュメントは、Actor のAI設計方針をまとめる。

Adventurer、Monster、Pet、GuildStaff は判断内容こそ異なるが、同じAI実行基盤に乗せる。

## 目的

Actor AI は、Actor が現在の状況を見て次の行動を自律的に決定するための仕組みである。

AIは以下の3階層で扱う。

- 長期目標: 何を達成したいか
- 中期計画: 長期目標を達成するために、現在どの方針で動くか
- 短期行動: 直近で実行する具体的なAction

例:

- 長期目標: 薬草を10個集める
- 中期計画: 薬草が出るフロアを探索する
- 短期行動: 移動する、アイテムを拾う、敵と戦う

## 配置方針

AIはUseCaseそのものではなく、Application層の意思決定サービスとして扱う。

```text
Application/
  AI/
    ActorDecisionScheduler
    ActorAiContext
    ActorAiRuntimeState
    ActorAiDirtyFlags
    IActorAiPolicy
    AdventurerAiPolicy
    MonsterAiPolicy
    PetAiPolicy
    GuildStaffAiPolicy
  Orchestration/
    AdvanceActorAiOrchestrator
  UseCase/
    ApplyActorAiDecisionUseCase
```

Domain は、AI判断の結果として成立する状態を持つ。Application/AI は、評価タイミング、dirty管理、cooldown、判断ロジックを持つ。Orchestrator は AI 評価の呼び出しと、決定されたDecisionの適用順序を担当する。

## Domain が持つもの

Actor または Actor に紐づくDomain状態として持ってよいもの:

- 現在の長期目標
- 現在の中期計画
- 現在の短期Action
- Actionの実行状態
- AI判断に使う永続的な傾向値
- AI判断に使う保存対象の記憶

Domain に持たせないもの:

- dirty flag
- cooldown
- last evaluated time
- 同一評価フレーム内で評価済みかどうか
- 一時的なイベント蓄積キュー
- 再評価優先度

これらは Application/AI の実行管理であり、Actor本体に持たせない。

## AI階層

### LongTermGoal

Actor が何を達成したいかを表す。

例:

- レベルを上げる
- 特定アイテムを集める
- 特定モンスターを討伐する
- 特定フロアへ到達する
- 施設勤務を継続する
- 巡回する

### MidTermPlan

長期目標を達成するための現在の方針を表す。

例:

- 目的アイテムが出るフロアへ向かう
- 目的アイテムが集まるまで現在フロアを探索する
- HPが少ないので回復可能な場所へ戻る
- 勤務施設へ移動する
- 周囲を巡回する

### ShortTermAction

直近で実行する具体的な行動を表す。

例:

- 移動する
- アイテムを拾う
- 敵を攻撃する
- 逃げる
- 階段を使う
- 施設を利用する
- 待機する

ShortTermAction は単なる結果値ではなく、実行状態を持つ。

想定する実行状態:

- NotStarted
- Running
- Completed
- Failed
- Cancelled

## 再評価

AIは毎フレーム常時評価しない。状態変化が起きたときにdirtyを立て、別の評価タイミングで最大1回だけ評価する。

イベント発生時に直接AI判定を行わない。イベントはdirty flagを立てるだけにする。

### Dirty Flags

dirty は階層別に持つ。

```csharp
[Flags]
public enum ActorAiDirtyFlags
{
    None = 0,
    ShortTerm = 1,
    MidTerm = 2,
    LongTerm = 4
}
```

評価時は、dirtyになっている最上位階層から再評価する。

```text
LongTerm dirtyあり -> LongTerm評価 -> MidTerm/ShortTerm dirty追加
MidTerm dirtyあり  -> MidTerm評価  -> ShortTerm dirty追加
ShortTerm dirtyあり -> ShortTerm評価
```

## Event To Dirty Mapping

イベントをそのままAI dirtyに変換しすぎない。AI用の意味イベントに集約してからdirtyを立てる。

例:

| 発生した変化 | AI用イベント | dirty |
|---|---|---|
| HP帯が変わった | HealthBandChanged | ShortTerm / MidTerm |
| 敵と接敵した | EnemyEnteredRange | ShortTerm |
| 攻撃を実行した | AttackExecuted | ShortTerm |
| ダンジョンに入った | EnteredDungeon | MidTerm / ShortTerm |
| 目的アイテム数が変わった | ObjectiveItemCountChanged | MidTerm / LongTerm |
| 目的モンスターを倒した | ObjectiveMonsterDefeated | LongTerm |
| ゲーム内日付が変わった | GameDateChanged | MidTerm / LongTerm |
| 現在Actionが失敗した | CurrentActionFailed | ShortTerm / MidTerm |
| 現在Actionが完了した | CurrentActionCompleted | ShortTerm |

どのイベントがどの階層をdirtyにするかは、AI設計上の重要な表として管理する。

## Cooldown と時間

AI評価にはcooldownを設ける。戦闘中など短時間に多くのイベントが発生する状況でも、一定間隔より短い再評価をしない。

重要な方針:

- AI cooldown は `CurrentScheduleTick` で扱わない。
- AI cooldown は `float currentTimeSeconds` で扱う。
- 同一判定フレーム内でActorごとのAI評価は最大1回にする。
- 同一フレームガードは `evaluationFrameId` で扱う。
- 短期AIの最小評価間隔は 0.5秒など小数秒を扱えるようにする。

`CurrentScheduleTick` は日付変更やスポーン抽選など低頻度スケジュール用であり、AIの再評価精度に使わない。

## ActorAiRuntimeState

Application/AI は、Actorごとの実行管理状態を持つ。

現在の主なフィールド:

```csharp
public sealed class ActorAiRuntimeState
{
    public Guid ActorId { get; }
    public ActorAiDirtyFlags DirtyFlags { get; private set; }
    public float LastEvaluatedTimeSeconds { get; private set; }
    public float CooldownUntilTimeSeconds { get; private set; }
    public int EvaluatedFrameId { get; private set; }
}
```

このRuntimeStateは原則としてセーブ対象にしない。セーブ復元後は、Goal / Plan / CurrentAction から必要なdirtyを立て直す。

## ActorDecisionScheduler

ActorDecisionScheduler は、どのActorのAIをいつ評価するかを制御する。

責務:

- イベントからdirtyを立てる
- 評価可能なActorを選ぶ
- 同一 `evaluationFrameId` 内で最大1回の制限を守る
- cooldownを守る
- dirtyの最上位階層を判定する

ActorDecisionScheduler はDomain Entityを直接変更しすぎない。意思決定結果をUseCaseへ渡し、Decision適用はUseCaseで行う。

## AI Policy

Actor種別ごとにPolicyを分ける。共通基盤は共有し、判断内容は分ける。

共通化してよいもの:

- dirty / cooldown / scheduler
- LongTerm / MidTerm / ShortTerm の評価順
- Action実行状態
- Action結果の扱い

共通化しすぎないもの:

- Adventurerの探索目的選択
- Adventurerの施設利用判断
- Monsterの徘徊判断
- Monsterの戦闘継続判断
- Petの追従判断
- GuildStaffの勤務判断

インターフェース:

```csharp
public interface IActorAiPolicy
{
    bool CanHandle(Actor actor);
    ActorAiDecision EvaluateLongTerm(ActorAiContext context);
    ActorAiDecision EvaluateMidTerm(ActorAiContext context);
    ActorAiDecision EvaluateShortTerm(ActorAiContext context);
}
```

`CanHandle` は実体のBehaviorを見て判断する。Behaviorに重複した識別enumを持たせない。

## ActorAiContext

AI Policyには、判断に必要な情報をContextとして渡す。

現在の主な情報:

- Actor
- `CurrentTimeSeconds`
- `ActorAiRuntimeState`
- 現在のGoal / Plan / Action

将来的に追加する候補:

- 現在地点
- 現在フロア
- 周囲の敵
- 周囲のアイテム
- 利用可能な施設
- 宿屋予約状態
- 交換項目

Contextは読み取り用にする。Policyは直接Domainを変更せず、Decisionを返す。

## ActorAiDecision

AI Policy は Domain を直接変更せず、Decision を返す。

```csharp
public sealed class ActorAiDecision
{
    public ActorGoal NextGoal { get; }
    public ActorPlan NextPlan { get; }
    public ActorAction NextAction { get; }
    public ActorAiDirtyFlags AdditionalDirtyFlags { get; }
}
```

UseCase が Decision を Actor / World へ適用する。

## UseCase

### AdvanceActorAiOrchestrator

AI評価を進めるOrchestrator。

責務:

- Schedulerから評価対象Actorを受け取る
- ActorAiContextを構築する
- 対応するPolicyを呼び出す
- Decisionを受け取る
- `ApplyActorAiDecisionUseCase` へ渡す

現在の呼び出しは以下の形。

```csharp
await advanceActorAiOrchestrator.ExecuteAsync(
    actors,
    currentTimeSeconds,
    evaluationFrameId,
    cooldownSeconds);
```

### ApplyActorAiDecisionUseCase

Decisionの結果をDomainへ適用するUseCase。

責務:

- Goal / Plan / Action の更新
- 必要に応じたAction状態の反映
- Actor位置やInventoryなどDomain状態の更新
- 既存UseCaseとの接続

## 永続化方針

保存する:

- LongTermGoal
- MidTermPlan
- 必要なCurrentActionの種類と対象ID
- AI判断に必要な永続的Memory / Preference

保存しない:

- dirty flag
- cooldown
- last evaluated time
- evaluated frame id
- 一時的な探索候補
- イベントキュー

復元時は、保存されたGoal / Plan / Actionから再評価に必要なdirtyを立て直す。

## 注意

- ActorにはAIの現在意思を持たせる。
- dirty、cooldown、評価時刻はApplication/AIへ置く。
- `CurrentScheduleTick` をAI評価間隔に使わない。
- PolicyはDecisionを返し、Domain更新はUseCaseで行う。
- イベントを細かくdirtyに直結させすぎない。

## Ground return cycle

Adventurer が Dungeon から Ground に戻った後の準備サイクルは、宿代不足による不要な再出発を避けるため、以下の順序に固定する。

1. 装備更新
2. 冒険者ギルドでのアイテム売却
3. 宿屋予約、宿代支払い、HP 回復
4. 回復アイテムなどの購入
5. 次の冒険への出発判定

装備更新は売却より前に行う。拾得した装備が現在の装備より強い場合、先に装備へ反映してから売却することで、使用すべき装備が売却対象になることを防ぐ。

宿代が払えない Actor でも宿泊は成立する。満額を払える場合は満額を徴収し、足りない場合は所持 Gold 全額だけを徴収する。所持 Gold が 0 の場合も 0G の宿泊として扱い、宿代不足を理由に回復を諦めて次の冒険へ出発してはならない。

現時点では、回復アイテムの購入処理は未実装の可能性がある。既存の `UseRecoveryItemOrchestrator` は購入ではなく、探索中に回復アイテムを使用する処理として扱う。

## Adventure goal design

Adventurer の冒険目的は、Actor が現在の冒険で「何を達成したら帰還してよいか」を表す。行動経路そのものではなく、探索中の終了条件を定義する。

冒険目的の正典は `ActorGoal` に統一する。`DungeonExplorationGoal` / `DungeonExplorationGoalType` のような探索専用の重複 DTO / enum は廃止し、目的選択 UseCase は直接 `ActorGoal` を返す。これにより、目的選択、Actor への適用、帰還判定で別々の型変換を持たない。

冒険目的は以下を標準とする。

| Goal | 行動方針 | 達成条件 |
|---|---|---|
| `ReachFloor` | 戦闘力に応じた目標フロアへ向かう | 指定フロアに到達する |
| `LevelUp` | 適切なフロアで探索・戦闘する | レベル上昇、またはレベル上昇に必要な経験値進捗を満たす |
| `DefeatMonster` | 指定モンスターが出るフロアを優先して探索・戦闘する | 指定 species のモンスターを指定数倒す |
| `EarnMoney` | 適切なフロアでモンスターを狩り、売却価値のあるドロップを集める | 今回の冒険で得た売却可能アイテムの見込み売却額が目標額に達する |
| `CollectItem` | 指定アイテムが落ちるフロア・敵を優先して探索する | 指定 item id を指定数集める |

`CollectMaterial` は独立 Goal として追加しない。素材集めは、具体的な素材 item id を指定する `CollectItem` として扱う。将来「素材タグのどれでもよい」目的が必要になった場合は、`CollectItem` を拡張するのではなく、タグ条件を表現できる Goal target を設計してから追加する。

### EarnMoney

`EarnMoney` は「Gold を直接拾う」ことではなく、「売却用または売却可能なドロップを集めて、帰還後に冒険者ギルドへ売る」ことを目的とする。

`EarnMoney` の行動方針は `LevelUp` と同じでよい。Actor は自分の戦闘力に合うフロアを選び、モンスターを狩り、ドロップを拾う。差分は帰還条件だけである。

`EarnMoney` の達成判定は以下で行う。

1. 今回の冒険開始時点の所持品を基準として記録する
2. 探索中に増えた売却可能アイテムを抽出する
3. 装備中のアイテム、回復アイテム、保持すべき非売却アイテムは除外する
4. `PricePolicy` と item master を使って、帰還後に施設へ売れる見込み金額を計算する
5. 見込み売却額が `ActorGoal.TargetCount` 以上なら達成とする

`EarnMoney` は `SpecialItemIds.Money` の所持数だけで判定してはならない。Dungeon 内で得た Gold がある場合は加算してよいが、主対象は売却可能ドロップの換金価値である。

`EarnMoney` の `ActorGoal` は以下の意味を持つ。

| Field | Meaning |
|---|---|
| `Type` | `ActorGoalType.EarnMoney` |
| `TargetId` | 0。特定 item / monster を指定しない |
| `TargetCount` | 目標見込み売却額 |
| `ProgressCount` | 現在の見込み売却額 |

探索中の基準所持品や討伐数など、冒険単位で変化する進捗は `ActorGoal` に直接詰め込まない。`ActorGoal` は目標と表示可能な進捗だけを持ち、冒険開始時点の inventory snapshot や討伐記録は Application service が管理する。

### Goal selection

冒険目的の選択は `SelectAdventureGoalUseCase` が担当する。探索専用の旧 `SelectDungeonExplorationGoalUseCase` は残さない。

`SelectAdventureGoalUseCase` は以下の入力から候補を作る。

- Actor のレベル、戦闘力、装備、所持品
- Guild の施設状態、依頼、交換需要、現在の Gold 不足
- Dungeon の深度帯、spawn table、drop table
- 直近の冒険履歴

候補選択は単純な均等ランダムではなく、重み付き選択にする。重みは後から調整可能な設定値として扱い、最低限以下の状況を反映する。

- 宿屋・店・施設アップグレードなどで Guild 側の Gold が不足している場合、`EarnMoney` の重みを上げる
- 未到達フロアがある場合、`ReachFloor` の重みを上げる
- Actor が次レベルに近い場合、`LevelUp` の重みを上げる
- 有効な依頼や交換需要がある場合、`CollectItem` の重みを上げる
- 特定 monster の討伐需要がある場合、`DefeatMonster` の重みを上げる

宿代不足時の `EarnMoney` 強制は行わない。宿代が足りない Actor も宿泊できるため、次回冒険目的は通常の `SelectAdventureGoalUseCase` による重み付き選択で決める。

### Adventurer death revival

Adventurer death is an exception to the normal Ground return cycle. When an Adventurer is defeated in combat and an Inn facility exists, the actor does not despawn. Instead, the death transaction performs the following order.

1. Publish `ActorDefeated` and clear combat state.
2. Drop every inventory item at the death position, including `SpecialItemIds.Money`.
3. Unequip every equipped item and drop each equipment item at the death position.
4. Move the Adventurer to Ground and set lifecycle to `Recovering`.
5. Create a free Inn revival reservation and publish `ActorReservedInn`.

The revival reservation does not charge the normal inn fee because all carried money has already been dropped at the death position. This flow is owned by Application lifecycle service state, not by permanent Domain actor state. If there is no Inn facility in the world, defeat keeps the legacy behavior and removes the actor from the world.

If a Monster defeats an Adventurer, the Monster still receives the normal defeat experience reward. Revival is an Adventurer-side recovery flow and does not cancel the `ActorDefeated` fact.

### Goal completion

冒険目的の達成判定は `DecideAdventurerReturnUseCase` に閉じ込めすぎない。理想設計では、Goal ごとの進捗計算を `AdventureGoalProgressService` に分離し、`DecideAdventurerReturnUseCase` は以下だけを行う。

1. `AdventureGoalProgressService` から現在の進捗を受け取る
2. Goal 達成、HP 不足、回復アイテム不足などをスコア化する
3. 帰還するかどうかを決める

`AdventureGoalProgressService` は `ActorGoalType` ごとの進捗計算を持つ。

- `ReachFloor`: 現在 LayerId
- `LevelUp`: 冒険開始時点からの Level / Experience 差分
- `DefeatMonster`: 冒険単位の defeated species count
- `EarnMoney`: 冒険単位の sellable loot value
- `CollectItem`: 冒険単位の item count 差分、または現在所持数

### Floor and target selection

`LevelUp` と `EarnMoney` は、どちらも「戦闘力に合う適切なフロアでモンスターを狩る」目的である。そのため、フロア選択処理は共有する。

`DefeatMonster` / `CollectItem` は、将来的には spawn table / drop table を見て、対象 monster または item の期待値が高いフロアを選ぶ。ただし対象フロアが Actor の戦闘力を大きく超える場合は、より安全なフロアへフォールバックする。

`ReachFloor` は到達可能な最深フロアを優先する。到達目標と戦闘力が矛盾する場合は、到達できる範囲の次フロアを選ぶ。
