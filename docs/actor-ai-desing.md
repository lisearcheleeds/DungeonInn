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
  UseCase/
    AdvanceActorAiUseCase
    ApplyActorAiDecisionUseCase
```

Domain は、AI判断の結果として成立する状態を持つ。Application/AI は、評価タイミング、dirty管理、cooldown、判断ロジックを持つ。UseCase は、AI評価の呼び出しと、決定されたDecisionの適用を担当する。

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

### AdvanceActorAiUseCase

AI評価を進めるUseCase。

責務:

- Schedulerから評価対象Actorを受け取る
- ActorAiContextを構築する
- 対応するPolicyを呼び出す
- Decisionを受け取る
- `ApplyActorAiDecisionUseCase` へ渡す

現在の呼び出しは以下の形。

```csharp
await advanceActorAiUseCase.ExecuteAsync(
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
